using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.EntityScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.ImplementationsEntities
{
    /// <inheritdoc />
    [AlwaysUpdateSystem]
    [DisableAutoCreation]
    public class GeometryEntitiesObjectTargeting : SystemBase, IGeoTargeting
    {
        private BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        private EntityQuery entityQuery = new EntityQuery();
        private GeometryVision geoVision;
        Vector3 rayLocation = Vector3.zero;
        Vector3 rayDirectionWS = Vector3.zero;    
        private int targetCount = 0;
        List<GeometryDataModels.Target> sharedTargets = new List<GeometryDataModels.Target>();
        
        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            // Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to
            // perform such changes on the main thread after the Job has finished. Command buffers allow you to perform
            // any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and
            // deletions for later.
            var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
            entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<Translation>()
            );
            targetCount = entityQuery.CalculateEntityCount();


            Vector3 rayDirWS = rayDirectionWS;
            Vector3 rayLocWS = rayLocation;

            NativeArray<GeometryDataModels.Target> targets = new NativeArray<GeometryDataModels.Target>(targetCount, Allocator.Temp);

            // Schedule the job that will add Targeting commands to the EntityCommandBuffer.
            // The actual burst compiled job will be cached once it is compiled (it will only get Burst compiled once).
            Entities
                .WithStoreEntityQueryInField(ref entityQuery)
                .WithName("GeometryObjectTargeting")
                .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
                .ForEach((Entity entity, int entityInQueryIndex, Translation position, GeometryDataModels.Target target,  in GeoInfoEntityComponent geoInfo,
                    in LocalToWorld location) =>
                {
                    Vector3 targetLocation = position.Value;
                    Vector3 rayDirection = rayDirWS;
                    Vector3 rayDirectionEndPoint = rayDirWS;
                    targetLocation = pointToRaySpace(rayLocWS, targetLocation);
                    rayDirectionEndPoint = pointToRaySpace(rayLocWS, rayDirection);
                    
                    Vector3 pointToRaySpace(Vector3 rayLocation,  Vector3 target2)
                    {
                        return target2- rayLocation;
                    }
                    target.projectedTargetPosition = Vector3.Project(targetLocation, rayDirectionEndPoint) + rayLocWS;
                    target.position = pointFromRaySpaceToObjectSpace(targetLocation, rayLocWS);
                    
                    Vector3 pointFromRaySpaceToObjectSpace(Vector3 rayLocation,  Vector3 target3)
                    {
                        return target3 + rayLocation;
                    }
                    
                    target.distanceToRay = Vector3.Distance(target.position, target.projectedTargetPosition);
                    target.distanceToCastOrigin = Vector3.Distance(rayLocWS, target.projectedTargetPosition);
                    targets[entityInQueryIndex] = target;
                }).ScheduleParallel();
            float deltaTime = Time.DeltaTime;
            
            //Wait for job completion
            m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            
            sharedTargets = targets.ToList();
            targets.Dispose();
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
            sharedTargets.OrderBy(target => target.distanceToRay).ToList();
            return sharedTargets;
        }
        
        public GeometryType TargetedType
        {
            get { return GeometryType.Objects; }
        }
    }
}