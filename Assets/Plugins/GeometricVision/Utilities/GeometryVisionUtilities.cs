using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using UnityEngine;

public static class GeometryVisionUtilities
{
    // Start is called before the first frame update
    public static IGeoBrain getControllerFromGeometryManager(GeometryVisionHead head, GeometryVisionEye eye)
    {

        if (head == null)
        {
            var factory = new GeometryVisionFactory();
            var headObject = factory.CreateGeometryVision(new Vector3(0f, 0f, 0f), Quaternion.identity, 25, eye,
                GeometryType.Edges, 0);
            return headObject.GetComponent<GeometryVisionBrain>();
        }

        return head.GetComponent<GeometryVisionBrain>();
    }
}
