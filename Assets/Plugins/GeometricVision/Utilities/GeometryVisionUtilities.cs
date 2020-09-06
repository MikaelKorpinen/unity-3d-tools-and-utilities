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
        internal static void SetupGeometryVision(GeometryVisionRunner runner, GeometryVision geoVision, List<TargetingInstruction> targetTypes, GeometryDataModels.FactorySettings settings)
        {
            var factory = new GeometryVisionFactory(settings);
            var geoTypesToTarget = new List<GeometryType>();
            
            foreach (var targetType in targetTypes)
            {
                geoTypesToTarget.Add(targetType.GeometryType);
            }

            factory.CreateGeometryVision(new Vector3(0f, 0f, 0f), Quaternion.identity, 25, geoVision,
                geoTypesToTarget, 0);
        }
    }
}