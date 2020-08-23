using System.Collections.Generic;
using GeometricVision;
using Unity.Collections;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    public class GeometryLineTargeting : IGeoTargeting
    {
        public NativeArray<GeometryDataModels.Target> GetTargetsAsNativeArray(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets)
        {
            throw new System.NotImplementedException();
        }

        List<GeometryDataModels.Target> IGeoTargeting.GetTargets(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets)
        {
            throw new System.NotImplementedException();
        }

        public NativeList<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets)
        {
            
            return new NativeList<GeometryDataModels.Target>();
            //throw new System.NotImplementedException();
        }

        public GeometryType TargetedType
        {
            get
            {
                return GeometryType.Lines;
            }
        }

        public bool IsForEntities()
        {
            return false;
        }
    }
}
