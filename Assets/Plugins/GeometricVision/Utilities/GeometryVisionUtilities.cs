using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using UnityEngine;

namespace Plugins.GeometricVision.Utilities
{
    public static class GeometryVisionUtilities
    {
        // Start is called before the first frame update
        public static void CreateGeometryVision(GeometryVisionHead head, GeometryVision geoVision)
        {
            if (head == null)
            {
                var factory = new GeometryVisionFactory();
                var geoTypesToTarget = new List<GeometryType>();
                geoTypesToTarget.Add(GeometryType.Objects);
                factory.CreateGeometryVision(new Vector3(0f, 0f, 0f), Quaternion.identity, 25, geoVision,
                    geoTypesToTarget, 0);
            }
        }
    }
}