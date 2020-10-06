using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsGameObjects
{
    public class GeometryVertexTargeting : IGeoTargeting
    {
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
            get
            {
                return GeometryType.Vertices;
            }
        }

        public bool IsForEntities()
        {
            return false;
        }
    }
}
