using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DisallowMultipleComponent]
public class GeometryVisionHead : MonoBehaviour
{
    private GeometryVisionEye _eye;
    private GeometryVisionBrain _brain;

    public GeometryVisionBrain Brain
    {
        get { return _brain; }
        set { _brain = value; }
    }

    public GeometryVisionEye Eye
    {
        get { return _eye; }
        set { _eye = value; }
    }
}
