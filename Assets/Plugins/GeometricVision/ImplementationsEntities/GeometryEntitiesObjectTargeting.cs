using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.Interfaces;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsEntities
{
    /// <inheritdoc />
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class GeometryEntitiesObjectTargeting : SystemBase, IGeoTargeting
    {
        private EntityQuery entityQuery;
        private GeometryVision geoVision;
        Vector3 rayLocation = Vector3.zero;
        Vector3 rayDirectionWS = Vector3.zero;
        NativeArray<GeometryDataModels.Target> targets;
        private Type entityFilterComponent;

        protected override void OnUpdate()
        {
            if (entityFilterComponent != null)
            {
                entityQuery = GetEntityQuery(entityFilterComponent, typeof(Translation),
                    typeof(GeometryDataModels.Target));
            }

            else
            {
                entityQuery = GetEntityQuery(typeof(Translation), typeof(GeometryDataModels.Target));
            }

            if (entityQuery.IsEmptyIgnoreFilter == false)
            {
                var job2 = new GetTargetsInParallel()
                {
                    targets = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.TempJob),
                    rayDirWS = this.rayDirectionWS,
                    rayLocWS = rayLocation,
                };

                this.Dependency = job2.Schedule(job2.targets.Length, 100);
                this.Dependency.Complete();

                var job3 = new CollectSeenTargets()
                {
                    targets = job2.targets,
                    seenTargets = new NativeList<GeometryDataModels.Target>(Allocator.TempJob),
                };

                this.Dependency = job3.Schedule(job2.targets.Length, this.Dependency);
                this.Dependency.Complete();
                this.targets = new NativeArray<GeometryDataModels.Target>(job3.seenTargets, Allocator.TempJob);

                job3.targets.Dispose();
                job3.seenTargets.Dispose();
            }
            else
            {
                this.targets = new NativeArray<GeometryDataModels.Target>(0, Allocator.Temp);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
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

        [BurstCompile]
        public struct CollectSeenTargets : IJobFor
        {
            [System.ComponentModel.ReadOnly(true)] public NativeArray<GeometryDataModels.Target> targets;

            [WriteOnly] [NativeDisableParallelForRestriction]
            public NativeList<GeometryDataModels.Target> seenTargets;

            public void Execute(int index)
            {
                if (targets[index].isSeen)
                {
                    seenTargets.Add(targets[index]);
                }
            }
        }

        public NativeArray<GeometryDataModels.Target> GetTargetsAsNativeArray(Vector3 rayLocation, Vector3 rayDirection,GeometryVision geometryVision, TargetingInstruction targetingInstruction)
        {
            geoVision = geometryVision;
            this.rayLocation = rayLocation;
            this.rayDirectionWS = rayDirection;
            this.entityFilterComponent = targetingInstruction.GetCurrentEntityFilterType();
            Update();

            return this.targets;
        }

        List<GeometryDataModels.Target> IGeoTargeting.GetTargets(Vector3 rayLocation, Vector3 rayDirection,
            GeometryVision geometryVision, TargetingInstruction targetingInstruction)
        {
            geoVision = geometryVision;
            this.rayLocation = rayLocation;
            this.rayDirectionWS = rayDirection;
            this.entityFilterComponent = targetingInstruction.GetCurrentEntityFilterType();
            Update();

            return this.targets.ToList();
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