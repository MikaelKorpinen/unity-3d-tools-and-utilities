﻿using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using UnityEngine;

namespace Plugins.GeometricVision.Utilities
{
    public static class GeometryVisionUtilities
    {
        // Start is called before the first frame update
        public static void SetupGeometryVision(GeometryVisionHead head, GeometryVision geoVision, List<VisionTarget> targetTypes)
        {
            if (head == null)
            {

                var factory = new GeometryVisionFactory();
                var geoTypesToTarget = new List<GeometryType>();
                foreach (var targetType in targetTypes)
                {
                    geoTypesToTarget.Add(targetType.GeometryType);
                }

                factory.CreateGeometryVision(new Vector3(0f, 0f, 0f), Quaternion.identity, 25, geoVision,
                    geoTypesToTarget, 0);
            }
        }
        
        public static void SetupGeometryVisionEye(GeometryVisionHead head, GeometryVision geoVision, float fov)
        {
            var factory = new GeometryVisionFactory();
                factory.CreateEye(head.gameObject, fov, geoVision);
        }
    }
}