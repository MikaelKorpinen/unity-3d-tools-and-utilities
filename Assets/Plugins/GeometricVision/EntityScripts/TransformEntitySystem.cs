using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.EntityScripts
{
    /// <inheritdoc />
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class TransformEntitySystem : SystemBase
    {
        private EntityQuery entityQuery = new EntityQuery();
        private Vector3 newPosition;
        private NativeArray<Entity> entityToMove;
        private NativeArray<Translation> translationComponent;
        private NativeArray<GeometryDataModels.Target> target;
        private float speedMultiplier;
        private NativeArray<bool> moveEntityDone;

        private EntityCommandBuffer ecb;
        EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            // Find the ECB system once and store it for later usage
            endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            
            translationComponent = new NativeArray<Translation>(1, Allocator.TempJob);
            float speed = speedMultiplier;
            moveEntityDone = new NativeArray<bool>(1, Allocator.TempJob);
            entityQuery = EntityManager.CreateEntityQuery(typeof(Translation), typeof(GeometryDataModels.Target));
            var entities = entityQuery.ToEntityArray(Allocator.TempJob);
            
            if(EntityManager.Exists(entityToMove[0]) && EntityManager.HasComponent<Translation>(entityToMove[0]))
            {

                translationComponent[0] = EntityManager.GetComponentData<Translation>(entityToMove[0]);
                var job2 = new MoveEntity()
                {
                    entityToMove = new NativeArray<Entity>(entityToMove, Allocator.TempJob),
                    translation = new NativeArray<Translation>(this.translationComponent, Allocator.TempJob),
       
                    target = target,
                    moveSpeed = speed,
                    newPosition = this.newPosition
                };
               
                Dependency = job2.Schedule(Dependency);
                //Wait for job completion
                Dependency.Complete();
                this.translationComponent[0] = job2.translation[0];
                job2.entityToMove.Dispose();
                job2.translation.Dispose();
                target[0] = job2.target[0];
                EntityManager.SetComponentData(entityToMove[0], translationComponent[0]);
            }
           
            translationComponent.Dispose();
            entities.Dispose();
            moveEntityDone.Dispose();
        }
        
        [BurstCompile]
        public struct MoveEntity : IJob
        {
            internal NativeArray<Entity> entityToMove;
            internal NativeArray<Translation> translation;
            internal NativeArray<GeometryDataModels.Target> target;
            private GeometryDataModels.Target tempJobTarget;
            internal float3 newPosition;
            internal float moveSpeed;
            
            public void Execute()
            {
                 tempJobTarget = target[0];
                tempJobTarget.position = Vector3.MoveTowards(tempJobTarget.position, newPosition, moveSpeed);
                target[0] = tempJobTarget;
                var trans = translation[0];
                trans.Value = tempJobTarget.position;
                translation[0] = trans;
            }
        }

        public Vector3 MoveEntityToPosition( Vector3 newPosition, GeometryDataModels.Target closestTarget, float speed)
        {
            this.speedMultiplier = speed;
            this.newPosition = newPosition;
            this.target = new NativeArray<GeometryDataModels.Target>(1, Allocator.TempJob);
            this.entityToMove = new NativeArray<Entity>(1, Allocator.TempJob);
            this.entityToMove[0] = closestTarget.entity;
            this.target[0] = closestTarget;
            this.Update();

            var toReturn= this.target[0].position;
            closestTarget = target[0];
            target.Dispose();
            entityToMove.Dispose();
            return toReturn;
        }

        public void DestroyTargetEntity(GeometryDataModels.Target target1)
        {
            ecb   = endSimulationEcbSystem.CreateCommandBuffer();
            if (World.EntityManager.Exists(target1.entity))
            {
                if (World.EntityManager.HasComponent<Child>(target1.entity))
                {
                    DynamicBuffer<Child> childs = World.EntityManager.GetBuffer<Child>(target1.entity);
                    foreach (var child in childs)
                    {
                        ecb.DestroyEntity(child.Value);
                    }
                }

                ecb.DestroyEntity(target1.entity);
            }
        }
    }
}