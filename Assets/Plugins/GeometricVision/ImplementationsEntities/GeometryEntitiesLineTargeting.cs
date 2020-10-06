using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsEntities
{
    [DisableAutoCreation]
    public class GeometryEntitiesLineTargeting : SystemBase, IGeoTargeting
    {
        protected override void OnUpdate()
        {
        
        }

        public NativeArray<GeometryDataModels.Target> GetTargetsAsNativeArray(Vector3 rayLocation, Vector3 rayDirection,GeometryVision geometryVision, TargetingInstruction targetingInstruction)
        {
            throw new System.NotImplementedException();
        }

        public List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirection, GeometryVision geometryVision,
            TargetingInstruction targetingInstruction)
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
