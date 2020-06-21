using System.Collections.Generic;
using GeometricVision;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    public class GeometryObjectTargeting : IGeoTargeting
    {

        public Vector3 ClosestPointOnRay(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets)
        {
            foreach (var target in targets)
            {
                Vector3 point =  target.transform.position;
                Vector3 rayDirectionEndPoint = rayDirection;
                point = VectorToRaySpace(rayLocation, point);
                rayDirectionEndPoint = VectorToRaySpace(rayLocation, rayDirection);
                Vector3 projection = Vector3.Project(point, rayDirectionEndPoint);
            }
            return Vector3.back;
        }

        public GeometryType TargetedType
        {
            get
            {
                return GeometryType.Objects;
            }
        }

        private Vector3 VectorToRaySpace(Vector3 rayLocation,  Vector3 target)
        {
            return target - rayLocation;
        }
    }
}
