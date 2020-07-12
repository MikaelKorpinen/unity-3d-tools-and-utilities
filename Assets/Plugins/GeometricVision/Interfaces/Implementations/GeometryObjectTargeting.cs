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
                targetInfos = GetDataForTarget();
                
                List<GeometryDataModels.Target> GetDataForTarget()
                {
                    Vector3 point = target.transform.position;
                    Vector3 rayDirectionEndPoint = rayDirectionWS;
                    point = pointToRaySpace(rayLocation, point);
                    rayDirectionEndPoint = pointToRaySpace(rayLocation, rayDirectionWS);
                    targetInfo.projectedTargetPosition = Vector3.Project(point, rayDirectionEndPoint) + rayLocation;
                    targetInfo.position = pointFromRaySpaceToObjectSpace(point, rayLocation);
                    targetInfo.distanceToRay = Vector3.Distance(targetInfo.position, targetInfo.projectedTargetPosition);
                    targetInfo.distanceToCastOrigin = Vector3.Distance(rayLocation, targetInfo.projectedTargetPosition);
                    targetInfos.Add(targetInfo);
                    return targetInfos;
                }
            }

            return targetInfos;
        }
        
        
        private Vector3 pointToRaySpace(Vector3 rayLocation,  Vector3 target)
        {
            return target - rayLocation;
        }
        
        private Vector3 pointFromRaySpaceToObjectSpace(Vector3 rayLocation,  Vector3 target)
        {
            return target + rayLocation;
        }
        
        public GeometryType TargetedType
        {
            get
            {
                return GeometryType.Objects;
            }
        }


    }
}
