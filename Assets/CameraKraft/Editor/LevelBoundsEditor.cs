using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LevelBoundsEditor : EditorWindow
{
    private static LevelCameraData _cameraData;
    private static Tool lastTool;
    private Vector3[] positions = new[] {Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};
    private bool _active = false;
    private static Color _fillColor;
    private static Color _outlineColor;
    private static Color _handleColor;
    [MenuItem("Tools/CameraKraft")]
    public static void Initialize()
    {
        GetWindow<LevelBoundsEditor>();
   
    }
    
    private void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
        EditorApplication.update += UpdateFast;
        
        _fillColor = new Color(0.9f, 0.3f, 0, 0.3f);
        _outlineColor = new Color(0, 0.3f, 0.5f, 1f);
        _handleColor = new Color(0.6f, 0.8f, 0.6f, 1f);
    }

    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        EditorApplication.update -= UpdateFast;
    }


    private void UpdateFast()
    {
        
    }


    
    

    void OnSceneGUI(SceneView scene)
    {     
        if(Selection.gameObjects.Length <= 0) return;
        
        GameObject gameObject = Selection.gameObjects.FirstOrDefault(gObj => gObj.GetComponent<LevelCameraData>());

        if (gameObject == null)
        {
            if (_cameraData != null)
            {
                _cameraData = null;
                OnDeselection();
            }
              
            return;
        }

        if (_cameraData == null)
        {
            _cameraData = gameObject.GetComponent<LevelCameraData>();
            OnSelection();
        }
        
//        Debug.Log($"Width: {_cameraData.Bounds.width}" );
        Handles.DrawSolidRectangleWithOutline(
            new Rect(_cameraData.Bounds.position - _cameraData.Bounds.size * 0.5f, _cameraData.Bounds.size), _fillColor, 
            _outlineColor);

        foreach (Vector3 pos in positions)
        {
            float scale = HandleUtility.GetHandleSize(pos) * 0.25f;
            Handles.color = _handleColor;
            Handles.CubeHandleCap(0, pos, Quaternion.identity, scale, EventType.Repaint);
            Handles.color = Color.white;
        }
        
        
        if(_active)
            Selection.activeGameObject = gameObject;




    }
    

    private void OnSelection()
    {
        Debug.Log("I am getting selected");
        lastTool = Tools.current;
        Tools.current = Tool.None;

        Rect dataBound = _cameraData.Bounds;

        positions[0] = dataBound.position + new Vector2(-dataBound.size.x * 0.5f, -dataBound.size.y * 0.5f);
        positions[1] = dataBound.position + new Vector2(dataBound.size.x * 0.5f, -dataBound.size.y * 0.5f);
        positions[2] = dataBound.position + new Vector2(-dataBound.size.x * 0.5f, dataBound.size.y * 0.5f);
        positions[3] = dataBound.position + new Vector2(dataBound.size.x * 0.5f, dataBound.size.y * 0.5f);

    }

    private void OnDeselection()
    {
        Debug.Log("O no don't go!");
        Tools.current = lastTool;
    }
    
    
}
