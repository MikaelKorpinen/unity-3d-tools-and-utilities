using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.Interfaces;
using UnityEngine;

namespace Plugins.GeometricVision.Utilities
{
    public static class GeometryVisionUtilities
    {
        // Start is called before the first frame update
        public static IGeoBrain getControllerFromGeometryManager(GeometryVisionHead head, GeometryVisionEye eye)
        {

            if (head == null)
            {
                var factory = new GeometryVisionFactory();
                var geoTypesToTarget = new List<GeometryType>();
                geoTypesToTarget.Add(GeometryType.Objects);
                var headObject = factory.CreateGeometryVision(new Vector3(0f, 0f, 0f), Quaternion.identity, 25, eye,
                    geoTypesToTarget, 0);
                return headObject.GetComponent<GeometryVisionBrain>();
            }

            return head.GetComponent<GeometryVisionBrain>();
        }
    }
}
