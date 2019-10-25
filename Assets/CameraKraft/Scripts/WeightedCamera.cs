using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeightedCamera : MonoBehaviour
{
    [Serializable]
    private struct ScereenPercentage{
       [Header("Size")]
        [Range(0, 1)] public float Height; 
        [Range(0, 1)] public float Width;
        [Header("Offset")]
        [Range(0, 1)] public float HeightOffset; 
        [Range(0, 1)] public float WidthOffset;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="horizontalFov">Camera horizontal fov in radians</param>
        /// <param name="verticalFov">Camera vertical fov in radians</param>
        /// <param name="transform">Center point of rect</param>
        /// <returns></returns>
        public Rect GetRect(float horizontalFov, float verticalFov, Vector3 cameraPos)
        {
            float relativeHeight = (float)(Math.Tan(verticalFov * 0.5f) * Math.Abs(cameraPos.z));
            float relativeWidth = (float)(Math.Tan(horizontalFov * 0.5f) * Math.Abs(cameraPos.z));
            Vector2 offset = new Vector2(relativeWidth * (0.5f - WidthOffset), relativeHeight * (0.5f - HeightOffset));
            Vector2 size = new Vector2(relativeWidth * Width, relativeHeight * Height) * 2;
            offset += (Vector2)cameraPos;
            return new Rect(offset, size);
        }
        public float aspect { get { return  (Screen.width * Width) / (Screen.height * Height); } }
        public Vector2 GetOffset(float horizontalFov, float verticalFov, Vector3 cameraPos)
        {
            return GetRect(horizontalFov, verticalFov, cameraPos).position - (Vector2)cameraPos;
        }

    }

    public class CameraWeightData
    {
        CameraWeight _cameraWeight;
        public Vector2 position;
        public float weight { get; private set; }
        public bool IsImportant { get { return _cameraWeight != null && _cameraWeight.isActiveAndEnabled && _cameraWeight.Important; } }

        public CameraWeightData(CameraWeight cameraWeight)
        {
            _cameraWeight = cameraWeight;
            position = cameraWeight.transform.position;
            weight = 0;
        }

        public void Update(float TimeToReach)
        {
            bool weightIsAlive = _cameraWeight != null && _cameraWeight.isActiveAndEnabled;

            position = weightIsAlive ? (Vector2)_cameraWeight.transform.position : position;
            float monoWeight = weightIsAlive ? _cameraWeight.Weight : 0f;
            weight += (monoWeight - weight) * (Time.deltaTime / TimeToReach);

            if (Mathf.Approximately(weight, 0))
                RemoveWeight(this);

        }
    }

    [Header("Movement")]
    [SerializeField] private float _speed = 0.01f;
    [Header("Zoom")]
    [SerializeField] private float _maxZoomDistance;
    [SerializeField] private float _minZoomDistance;
    // [SerializeField] private ScereenPercentage _screenBuffer;
    [SerializeField] private Vector2 _edgeBuffer;


    private List<CameraWeightData> _weights = new List<CameraWeightData>();
    private Camera _camera;
    private static WeightedCamera _instance;

    private Rect _levelBounds;

    private float _verticalFOVInRads { get { return _camera.fieldOfView * Mathf.Deg2Rad; } }

    private float _help_HFOVInRads = 0f;
    private float _help_cameraAspect = 0f;
    private float _horizontalFOVInRads
    {
        get
        {
            if(_camera.aspect != _help_cameraAspect)
            {
                _help_cameraAspect = _camera.aspect;
                _help_HFOVInRads = 2 * Mathf.Atan(Mathf.Tan(_verticalFOVInRads / 2) * _camera.aspect);
            }
            return _help_HFOVInRads;
        }
    }
    private float _horizontalFOV => _horizontalFOVInRads * Mathf.Rad2Deg;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        _camera = GetComponent<Camera>();
        LevelCameraData data = FindObjectOfType<LevelCameraData>();
        Debug.Log(data.gameObject.name);
        _levelBounds = data.Bounds;
        float maxXHieght = (_levelBounds.width * 0.5f) / Mathf.Tan(_horizontalFOVInRads * 0.5f);
        float maxYHieght = (_levelBounds.height * 0.5f) / Mathf.Tan(_verticalFOVInRads * 0.5f);

        _maxZoomDistance = maxYHieght > maxXHieght ? maxXHieght : maxYHieght;
    }

    void Update()
    {
        if (_levelBounds == null) return;
        _weights.ForEach(weight => weight.Update(1));
        Vector3 pos = GetCenterPos();
        pos.z = Zoom(pos);

        transform.position = Clamp(pos);
    }
    //Gather all the weights and calculate the weighted center
    private Vector2 GetCenterPos()
    {
        Vector2 centerPos = Vector3.zero;
        float totalWeight = 0;
        foreach (CameraWeightData weight in _weights)
        {
            centerPos += weight.position * weight.weight;
            totalWeight += weight.weight;
        }

        return totalWeight <= 0 ? (Vector2)transform.position : (centerPos / totalWeight); 
    }

    private float Zoom(Vector3 currentPos)
    {
        //Find the furthest relative point
        float currentDistance = 0;
        Vector2 furthestPoint = currentPos;

        List<CameraWeightData> centralWeights = new List<CameraWeightData>( _weights.Where(weight => weight.IsImportant));


        foreach (CameraWeightData weight in centralWeights)
        {

            float tempX = Mathf.Abs((weight.position.x  - currentPos.x) / _camera.aspect);
            float tempY = Mathf.Abs(weight.position.y - currentPos.y);

            if (tempY  > currentDistance || tempX > currentDistance)
            {
                currentDistance = tempY > tempX ? tempY : tempX;
                furthestPoint = weight.position;
            } 
        }

        float xWeightDist = Mathf.Abs(currentPos.x - furthestPoint.x) + _edgeBuffer.x;
        float yWeightDist = Mathf.Abs(currentPos.y - furthestPoint.y) + _edgeBuffer.y;

        //Calculate the cameras distance from 0 in z axis
        float zDistance;
        if (yWeightDist > (xWeightDist  / _camera.aspect))
            zDistance = yWeightDist / Mathf.Tan(_verticalFOVInRads * 0.5f);
        else
            zDistance = xWeightDist / Mathf.Tan(_horizontalFOVInRads * 0.5f);

        zDistance = Mathf.Clamp(zDistance, _minZoomDistance, _maxZoomDistance);
        return -zDistance;
    }

    private Vector3 Clamp(Vector3 pos)
    {
        Vector3 xEdgeDirection = Quaternion.Euler(0, _horizontalFOV * 0.5f, 0) * -transform.forward;
        Vector3 yEdgeDirection = Quaternion.Euler(-_camera.fieldOfView * 0.5f, 0 , 0) * -transform.forward;

        Vector2 maxPos = _levelBounds.position + new Vector2(_levelBounds.width * 0.5f, _levelBounds.height * 0.5f);

        float cameraDist = Mathf.Abs(transform.position.z) - _camera.nearClipPlane;

        float xLength = cameraDist / Mathf.Cos(_horizontalFOVInRads * 0.5f);
        float yLength = cameraDist / Mathf.Cos(_verticalFOVInRads * 0.5f);

        float maxX = ((Vector3)(_levelBounds.position + new Vector2(_levelBounds.width * 0.5f, 0)) + xLength * xEdgeDirection).x - _camera.rect.width * 0.5f;
        float maxY = ((Vector3)(_levelBounds.position + new Vector2(0, _levelBounds.height * 0.5f)) + yLength * yEdgeDirection).y - _camera.rect.height * 0.5f;
        
        maxX = Mathf.Clamp(maxX, 0, _levelBounds.width * 0.5f);
        Debug.Log(_levelBounds);
        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);;

        maxY = Mathf.Clamp(maxY, 0, _levelBounds.height * 0.5f);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);

        Debug.Log(pos);
        
        return pos;
    }

    public static void AddWeight(CameraWeight weight) {
       _instance._weights.Add(new CameraWeightData(weight));
    }
    public static void RemoveWeight(CameraWeightData weight)
    {
        _instance._weights.Remove(weight);
    }

    private void OnDrawGizmos()
    {/*
        Gizmos.color = new Color(0, 0, 1, 0.5F);
        Rect rect = _screenBuffer.GetRect(2 * Mathf.Atan(Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad / 2) * Camera.main.aspect), Camera.main.fieldOfView * Mathf.Deg2Rad, transform.position);
        Gizmos.DrawCube(rect.position, rect.size); */
    }
}
