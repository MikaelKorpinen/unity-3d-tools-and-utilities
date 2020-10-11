using System;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsGameObjects
{
    public class GeometryObjectTargeting : IGeoTargeting
    {
        public NativeArray<GeometryDataModels.Target> GetTargetsAsNativeArray(Vector3 rayLocation, Vector3 rayDirection, GeometryVision geometryVision, TargetingInstruction targetingInstruction)
        {
            throw new NotImplementedException();
        }

        List<GeometryDataModels.Target> IGeoTargeting.GetTargets(Vector3 rayLocation, Vector3 rayDirectionWS,
            GeometryVision geoVision, TargetingInstruction targetingInstruction)
        {
            GeometryDataModels.Target targetInfo = new GeometryDataModels.Target();
            List<GeometryDataModels.Target> gameObjectTargets= new List<GeometryDataModels.Target>();
            
            if (targetingInstruction.TargetTag.Length > 0)
            {
                foreach (var geoInfoAsTarget in geoVision.GetEye<GeometryVisionEye>().SeenGeoInfos)
                {
                    if (geoInfoAsTarget.gameObject.CompareTag(targetingInstruction.TargetTag))
                    {
                        gameObjectTargets.Add(GetDataForTarget(geoInfoAsTarget));
                    } 
                }
            }
            else 
            {
                foreach (var geoInfoAsTarget in geoVision.GetEye<GeometryVisionEye>().SeenGeoInfos)
                {
                    gameObjectTargets.Add(GetDataForTarget(geoInfoAsTarget));
                } 
            }
            

            return gameObjectTargets;
            
            //Local functions 
            
            GeometryDataModels.Target GetDataForTarget(GeometryDataModels.GeoInfo geoInfoAsTarget)
            {
                float3 point = geoInfoAsTarget.transform.position;
                float3 rayDirectionEndPoint = rayDirectionWS;
                point = pointToRaySpace(rayLocation, point);
                rayDirectionEndPoint = pointToRaySpace(rayLocation, rayDirectionWS);
                targetInfo.projectedTargetPosition = Vector3.Project(point, rayDirectionEndPoint) + rayLocation;
                targetInfo.position = pointFromRaySpaceToObjectSpace(point, rayLocation);
                targetInfo.distanceToRay =
                    Vector3.Distance(targetInfo.position, targetInfo.projectedTargetPosition);
                targetInfo.distanceToCastOrigin =
                    Vector3.Distance(rayLocation, targetInfo.projectedTargetPosition);
                targetInfo.GeoInfoHashCode = geoInfoAsTarget.GetHashCode();
                
                return targetInfo;
            }

        }

        public NativeArray<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirectionWS, GeometryVision geometryVision,
            List<GeometryDataModels.GeoInfo> targets)
        {
            List<GeometryDataModels.Target> targetInfos = new List<GeometryDataModels.Target>();
            targetInfos = GetProjectionDataForTargets(rayLocation, rayDirectionWS, targets, targetInfos);

            return new NativeList<GeometryDataModels.Target>(); //targetInfos;
        }

        private List<GeometryDataModels.Target> GetProjectionDataForTargets(Vector3 rayLocation, Vector3 rayDirectionWS,
            List<GeometryDataModels.GeoInfo> GeoInfos, List<GeometryDataModels.Target> targetInfos)
        {
            GeometryDataModels.Target targetInfo = new GeometryDataModels.Target();
            foreach (var geoInfoAsTarget in GeoInfos)
            {
                targetInfos = GetDataForTarget();

                List<GeometryDataModels.Target> GetDataForTarget()
                {
                    Vector3 point = geoInfoAsTarget.transform.position;
                    Vector3 rayDirectionEndPoint = rayDirectionWS;
                    point = pointToRaySpace(rayLocation, point);
                    rayDirectionEndPoint = pointToRaySpace(rayLocation, rayDirectionWS);
                    targetInfo.projectedTargetPosition = Vector3.Project(point, rayDirectionEndPoint) + rayLocation;
                    targetInfo.position = pointFromRaySpaceToObjectSpace(point, rayLocation);
                    targetInfo.distanceToRay =
                        Vector3.Distance(targetInfo.position, targetInfo.projectedTargetPosition);
                    targetInfo.distanceToCastOrigin = Vector3.Distance(rayLocation, targetInfo.projectedTargetPosition);
                    targetInfo.GeoInfoHashCode = geoInfoAsTarget.GetHashCode();
                    targetInfos.Add(targetInfo);
                    return targetInfos;
                }
            }

            return targetInfos;
        }

        private Vector3 pointToRaySpace(Vector3 rayLocation, Vector3 target)
        {
            return target - rayLocation;
        }

        private Vector3 pointFromRaySpaceToObjectSpace(Vector3 rayLocation, Vector3 target)
        {
            return target + rayLocation;
        }

        public GeometryType TargetedType
        {
            get { return GeometryType.Objects; }
        }

        public bool IsForEntities()
        {
            return false;
        }
    }
}