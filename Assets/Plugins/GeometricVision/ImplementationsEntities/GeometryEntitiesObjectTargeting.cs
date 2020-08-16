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
        NativeArray<GeometryDataModels.Target> targets;

        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            entityQuery = GetEntityQuery(typeof(Translation), typeof(GeometryDataModels.Target));

            
            var job2 = new GetTargetsInParallel()
            {
                targets = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.TempJob),
                rayDirWS = rayDirectionWS,
                rayLocWS = rayLocation
            };
            
            this.Dependency = job2.Schedule(job2.targets.Length, 6);

            //Wait for job completion
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            
            targets = new NativeArray<GeometryDataModels.Target>(job2.targets.Length, Allocator.TempJob);
            targets.CopyFrom(job2.targets);
            job2.targets.Dispose();
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
          
                Vector3 Project(Vector3 position, Vector3 direction)
                {
                    float num1 = Vector3.Dot(direction, direction);
                    if (num1 < float.Epsilon)
                        return Vector3.zero;
                    float num2 = Vector3.Dot(position, direction);
                    return new Vector3(direction.x * num2 / num1, direction.y * num2 / num1,
                        direction.z * num2 / num1);
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

        public List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirectionWS, List<GeometryDataModels.GeoInfo> targets)
        {
            this.rayLocation = rayLocation;
            this.rayDirectionWS = rayDirectionWS;
            Update();
            this.targets.OrderBy(target => target.distanceToRay).ToList();
            List<GeometryDataModels.Target> targetsToReturn = this.targets.ToList();
            this.targets.Dispose();
            return targetsToReturn;
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