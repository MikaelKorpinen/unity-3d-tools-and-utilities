using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsEntities
{
    /// <inheritdoc cref="Plugins.GeometricVision.Interfaces.IGeoProcessor" />
    /// 
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateAfter(typeof(GeometryVisionEntityEye))]
    public class GeometryVisionEntityProcessor : SystemBase, IGeoProcessor
    {
        private bool collidersTargeted;

        //private bool extractGeometry;
        private BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;
        [System.ComponentModel.ReadOnly(true)] public EntityCommandBuffer.ParallelWriter ConcurrentCommands;
        private int currentObjectCount;
        private GeometryVision geoVision;
        private EntityQuery entityQuery = new EntityQuery();
        private EntityManager entityManager;


        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            entityManager = World.EntityManager;
        }

        protected override void OnUpdate()
        {

            entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            EntityQuery entitiesWithoutTargetComponent = GetEntityQuery(
                new EntityQueryDesc()
                {
                    None = new ComponentType[] {typeof(GeometryDataModels.Target)},
                }

                );

            if ( entitiesWithoutTargetComponent.CalculateEntityCount() != 0)
            {
                var entitiesWithOutComponent = entitiesWithoutTargetComponent.ToEntityArray(Allocator.TempJob);
                entityManager.AddComponent<GeometryDataModels.Target>(entitiesWithOutComponent);
                entitiesWithOutComponent.Dispose();
            }

            entityQuery = GetEntityQuery(typeof(Translation),typeof(GeometryDataModels.Target) );

            var job2 = new ModifyTargets()
            {
                targets = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.TempJob),
                translations = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob),
                entities= entityQuery.ToEntityArray(Allocator.TempJob),
            };

            this.Dependency = job2.Schedule(job2.targets.Length, 6);
            this.Dependency.Complete();
            
            entityQuery.CopyFromComponentDataArray<Translation>(job2.translations);
            entityQuery.CopyFromComponentDataArray<GeometryDataModels.Target>(job2.targets);

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            currentObjectCount = entityQuery.CalculateEntityCount();


            job2.translations.Dispose();
            job2.targets.Dispose();
            job2.entities.Dispose();
        }
        
        [BurstCompile]
        public struct ModifyTargets : IJobParallelFor
        {
            [System.ComponentModel.ReadOnly(true)] public NativeArray<GeometryDataModels.Target> targets;

            [System.ComponentModel.ReadOnly(true)] public NativeArray<Translation> translations;
            public NativeArray<Entity> entities;

            public void Execute(int index)
            {
                GeometryDataModels.Target target = targets[index];
                target.position = translations[index].Value;
                target.isEntity = true;
                target.entity = entities[index];
                targets[index] = target;
            }
        }

        /// <summary>
        /// Used to check, if things inside scene has changed. Like if new object has been removed or moved.
        /// </summary>
        public void CheckSceneChanges(GeometryVision geoVision)
        {
            this.GeoVision = geoVision;
            Update();
        }
        
        public int CountSceneObjects()
        {
            return currentObjectCount;
        }
        
        public GeometryVision GeoVision
        {
            get { return geoVision; }
            set { geoVision = value; }
        }
    }
}