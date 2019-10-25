using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraWeight : MonoBehaviour
{
    public float Weight;
    public bool Important;

    private void Start()
    {
        WeightedCamera.AddWeight(this);  
    }

}
