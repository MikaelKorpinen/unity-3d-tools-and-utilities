﻿using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.EntityScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.ImplementationsEntities
{
    /// <inheritdoc />
    [DisableAutoCreation]
    public class GeometryEntitiesObjectTargeting : SystemBase, IGeoTargeting
    {
        private BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        private EntityQuery entityQuery = new EntityQuery();
        private GeometryVision geoVision;
        Vector3 rayLocation = Vector3.zero;
        Vector3 rayDirectionWS = Vector3.zero;
        List<GeometryDataModels.Target> sharedTargets = new List<GeometryDataModels.Target>();

        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }


        protected override void OnUpdate()
        {
            entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<GeometryDataModels.Target>()
            );
            Vector3 rayDirWS = rayDirectionWS;
            Vector3 rayLocWS = rayLocation;
            
            entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<GeometryDataModels.Target>()
            );
            
            entityQuery = GetEntityQuery(
                new EntityQueryDesc()
                {
                    All = new ComponentType[] {ComponentType.ChunkComponent<GeometryDataModels.Target>()},
                });
            
            entityQuery = GetEntityQuery(typeof(Translation),typeof(GeometryDataModels.Target) );




            var currentObjectCount = entityQuery.CalculateEntityCountWithoutFiltering();

            var job2 = new GetTargetsFromChunk()
            {
                targets  = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.Temp),
                translations = entityQuery.ToComponentDataArray<Translation>(Allocator.Temp),
                rayDirWS = rayDirectionWS,
                rayLocWS = rayLocation
            };
            this.Dependency = job2.Schedule(job2.targets.Length, 6);

            NativeArray<GeometryDataModels.Target> targets = job2.targets;

            //Wait for job completion
            m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            UnityEngine.Debug.Log("targets: " + targets.Length);
            sharedTargets = targets.ToList();
            targets.Dispose();
        }
        
        [BurstCompile]
        public struct GetTargetsFromChunk : IJobParallelFor
        {
            [System.ComponentModel.ReadOnly(true)]
            public NativeArray<GeometryDataModels.Target> targets;

            [System.ComponentModel.ReadOnly(true)] public NativeArray<Translation> translations;
            internal Vector3 rayDirWS;
            internal Vector3 rayLocWS;

            public void Execute(int i)
            {
                GeometryDataModels.Target target = targets[i];
                Vector3 targetLocation =  target.position;
                Vector3 rayDirection = rayDirWS;
                Vector3 rayDirectionEndPoint = rayDirWS;
                targetLocation = pointToRaySpace(rayLocWS, targetLocation);
                rayDirectionEndPoint = pointToRaySpace(rayLocWS, rayDirection);

                Vector3 pointToRaySpace(Vector3 rayLocation, Vector3 target2)
                {
                    return target2 - rayLocation;
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

        public static float3 Project2(float3 vector, float3 onNormal)
        {
            float3 result;

            float sqrMag = math.dot(onNormal, onNormal);
            if (sqrMag < kEpisilon)
                result = float3.zero;
            else
                result = onNormal * math.dot(vector, onNormal) / sqrMag;

            return result;
        }

        public void Debug(GeometryVision geoVisions)
        {
        }

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
            if (sharedTargets.Count > 0 &&  sharedTargets[0].distanceToRay == 0 && sharedTargets[0].distanceToCastOrigin == 0)
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