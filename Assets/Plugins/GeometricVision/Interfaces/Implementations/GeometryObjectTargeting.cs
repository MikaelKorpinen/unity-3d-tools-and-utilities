using System.Collections.Generic;
using GeometricVision;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    public class GeometryObjectTargeting : IGeoTargeting
    {

        public Vector3 ClosestPointOnRay(Vector3 rayLocation, Vector3 rayDirectionWS, List<GeometryDataModels.GeoInfo> targets)
        {
            Vector3 projection = Vector3.zero;
            foreach (var target in targets)
            {
                Vector3 point =  target.transform.position;
                Vector3 rayDirectionEndPoint = rayDirectionWS;
                point = pointToRaySpace(rayLocation, point);
                rayDirectionEndPoint = pointToRaySpace(rayLocation, rayDirectionWS);
                projection = Vector3.Project(point, rayDirectionEndPoint);
                
            }
            return projection;
        }

        public GeometryType TargetedType
        {
            get
            {
                return GeometryType.Objects;
            }
        }

        private Vector3 pointToRaySpace(Vector3 rayLocation,  Vector3 target)
        {
            return target - rayLocation;
        }
    }
}
