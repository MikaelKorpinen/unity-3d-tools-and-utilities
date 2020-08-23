using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.ImplementationsEntities
{
    [DisableAutoCreation]
    public class GeometryEntitiesLineTargeting : SystemBase, IGeoTargeting
    {
    
    
        protected override void OnUpdate()
        {
        
        }

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
            throw new System.NotImplementedException();
        }

        public GeometryType TargetedType
        {
            get { return GeometryType.Lines; }
        }

        public bool IsForEntities( ){
            return true;
        }
    }
}
