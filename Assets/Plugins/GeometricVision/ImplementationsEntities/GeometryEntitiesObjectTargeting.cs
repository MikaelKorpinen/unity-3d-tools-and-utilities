using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsEntities
{
    /// <inheritdoc />
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class GeometryEntitiesObjectTargeting : SystemBase, IGeoTargeting
    {
        private BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;
        private EntityQuery entityQuery;
        private GeometryVision geoVision;
        Vector3 rayLocation = Vector3.zero;
        Vector3 rayDirectionWS = Vector3.zero;
        List<GeometryDataModels.Target> sharedTargets = new List<GeometryDataModels.Target>();

        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }


        protected override void OnUpdate()
        {
            Vector3 rayDirWS = rayDirectionWS;
            Vector3 rayLocWS = rayLocation;
            
            
            entityQuery = GetEntityQuery(typeof(Translation), typeof(GeometryDataModels.Target));

            var job2 = new GetTargetsInParallel()
            {
                targets = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.Temp),
                rayDirWS = rayDirectionWS,
                rayLocWS = rayLocation
            };
            this.Dependency = job2.Schedule(job2.targets.Length, 6);

            NativeArray<GeometryDataModels.Target> targets = job2.targets;

            //Wait for job completion
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            sharedTargets = targets.ToList();
            targets.Dispose();
        }

        [BurstCompile]
        public struct GetTargetsInParallel : IJobParallelFor
        {
            [System.ComponentModel.ReadOnly(true)] public NativeArray<GeometryDataModels.Target> targets;

            internal Vector3 rayDirWS;
            internal Vector3 rayLocWS;

            public void Execute(int i)
            {
                GeometryDataModels.Target target = targets[i];
                Vector3 targetLocation = target.position;
                Vector3 rayDirection = rayDirWS;
                Vector3 rayDirectionEndPoint = rayDirWS;
                targetLocation = pointToRaySpace(rayLocWS, targetLocation);
                rayDirectionEndPoint = pointToRaySpace(rayLocWS, rayDirection);

                Vector3 pointToRaySpace(Vector3 rayLocation, Vector3 point)
                {
                    return point - rayLocation;
                }


                target.projectedTargetPosition = Project(targetLocation, rayDirectionEndPoint) + rayLocWS;

                Vector3 Project(Vector3 vector, Vector3 onNormal)
                {
                    float num1 = Vector3.Dot(onNormal, onNormal);
                    if (num1 < float.Epsilon)
                        return Vector3.zero;
                    float num2 = Vector3.Dot(vector, onNormal);
                    return new Vector3(onNormal.x * num2 / num1, onNormal.y * num2 / num1,
                        onNormal.z * num2 / num1);
                }

                target.position = pointFromRaySpaceToObjectSpace(targetLocation, rayLocWS);

                Vector3 pointFromRaySpaceToObjectSpace(Vector3 rayLocation, Vector3 target3)
                {
                    return target3 + rayLocation;
                }

                target.isEntity = true;
                target.distanceToRay = Vector3.Distance(target.position, target.projectedTargetPosition);
                target.distanceToCastOrigin = Vector3.Distance(rayLocWS, target.projectedTargetPosition);
                targets[i] = target;
            }
        }

        const float kEpisilon = 1.17549435E-38f;

        public GeometryVision GeoVision
        {
            get { return geoVision; }
            set { geoVision = value; }
        }

        public List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirectionWS,
            List<GeometryDataModels.GeoInfo> targets)
        {
            this.rayLocation = rayLocation;
            this.rayDirectionWS = rayDirectionWS;
            Update();
            sharedTargets.OrderBy(target => target.distanceToRay).ToList();
            //      UnityEngine.Debug.Log(sharedTargets.Count);
            if (sharedTargets.Count > 0 && sharedTargets[0].distanceToRay == 0 &&
                sharedTargets[0].distanceToCastOrigin == 0)
            {
                sharedTargets = new List<GeometryDataModels.Target>();
            }

            return sharedTargets;
        }

        public GeometryType TargetedType
        {
            get { return GeometryType.Objects; }
        }

        public bool IsForEntities()
        {
            return true;
        }
    }
}