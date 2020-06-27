using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using UnityEngine;
[DisallowMultipleComponent]
public class GeometryVisionHead : MonoBehaviour
{
    private GeometryVisionEye _eye;
    private IGeoBrain _brain;

    public IGeoBrain Brain
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
