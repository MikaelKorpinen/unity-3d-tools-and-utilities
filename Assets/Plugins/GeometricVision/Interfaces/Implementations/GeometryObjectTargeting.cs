using System.Collections.Generic;
using System.Linq;
using GeometricVision;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    public class GeometryObjectTargeting : IGeoTargeting
    {

        public List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirectionWS, List<GeometryDataModels.GeoInfo> targets)
        {
            List<GeometryDataModels.Target> targetInfos = new List<GeometryDataModels.Target>();
            targetInfos =GetProjectionDataForTargets(rayLocation, rayDirectionWS, targets, targetInfos);
            targetInfos = targetInfos.OrderBy(target => target.distanceToRay).ToList();
            
            return targetInfos;
            
        }

        private List<GeometryDataModels.Target> GetProjectionDataForTargets(Vector3 rayLocation, Vector3 rayDirectionWS, List<GeometryDataModels.GeoInfo> targets, List<GeometryDataModels.Target> targetInfos)
        {
            GeometryDataModels.Target targetInfo = new GeometryDataModels.Target();
            foreach (var target in targets)
            {
                targetInfos = getDataForTarget(rayLocation, rayDirectionWS, targetInfos, target, targetInfo);
            }

            return targetInfos;
        }

        private List<GeometryDataModels.Target> getDataForTarget(Vector3 rayLocation, Vector3 rayDirectionWS,
            List<GeometryDataModels.Target> targetInfos, GeometryDataModels.GeoInfo target,
            GeometryDataModels.Target targetInfo)
        {
            Vector3 point = target.transform.position;
            Vector3 rayDirectionEndPoint = rayDirectionWS;
            point = pointToRaySpace(rayLocation, point);
            rayDirectionEndPoint = pointToRaySpace(rayLocation, rayDirectionWS);
            targetInfo.projectionOnDirection = Vector3.Project(point, rayDirectionEndPoint) + rayLocation;
            targetInfo.position = pointFromRaySpaceToObjectSpace(point, rayLocation);
            targetInfo.distanceToRay = Vector3.Distance(targetInfo.position, targetInfo.projectionOnDirection);
            targetInfo.distanceToCastOrigin = Vector3.Distance(rayLocation, targetInfo.projectionOnDirection);
            targetInfos.Add(targetInfo);
            return targetInfos;
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
        
        private Vector3 pointFromRaySpaceToObjectSpace(Vector3 rayLocation,  Vector3 target)
        {
            return target + rayLocation;
        }
    }
}
