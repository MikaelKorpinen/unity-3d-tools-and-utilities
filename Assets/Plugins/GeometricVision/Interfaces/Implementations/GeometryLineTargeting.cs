using System.Collections.Generic;
using GeometricVision;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    public class GeometryLineTargeting : IGeoTargeting
    {

        public List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets)
        {
            
            return new List<GeometryDataModels.Target>();
            //throw new System.NotImplementedException();
        }

        public GeometryType TargetedType
        {
            get
            {
                return GeometryType.Lines;
            }
        }
    }
}
