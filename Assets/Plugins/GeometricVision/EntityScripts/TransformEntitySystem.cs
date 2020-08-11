using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
        private int entityIdToTransform;
        private int entityVersionToTransform;
        private NativeArray<Entity> entityToMove;
        private NativeArray<Translation> translationComponent;
        private NativeArray<GeometryDataModels.Target> target;
        private bool initLookForEntity;
        private float speedMultiplier;
        private NativeArray<bool> moveEntityDone;

        public bool InitLookForEntity
        {
            set { initLookForEntity = value; }
        }
        //   private BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {

            initLookForEntity = true;
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            //     entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy()
        {

            translationComponent.Dispose();
            target.Dispose();
            entityToMove.Dispose();
            moveEntityDone.Dispose();
        }


        protected override void OnUpdate()
        {
            translationComponent = new NativeArray<Translation>(1, Allocator.TempJob);
            float speed = speedMultiplier;
            moveEntityDone = new NativeArray<bool>(1, Allocator.TempJob);
            //    var transHold = translation;

            //   entityQuery = GetEntityQuery(typeof(Translation), typeof(GeometryDataModels.Target));
            entityQuery = EntityManager.CreateEntityQuery(typeof(Translation), typeof(GeometryDataModels.Target));
            var entities = entityQuery.ToEntityArray(Allocator.TempJob);

            
            /*if (initLookForEntity)
            {

                Debug.Log("target id and version  " +target[0].entityId + " " + target[0].entityVersion);
                Debug.Log("target is entity  " +target[0].isEntity);
                var job = new FindEntity()
                {
                    entities = new NativeArray<Entity>(entities, Allocator.TempJob),
                    id = target[0].entityId,
                    version = target[0].entityVersion,
                    entityToMove = new NativeArray<Entity>(1,Allocator.TempJob),
                    translationComponent = new NativeArray<Translation>(1, Allocator.TempJob),
                    translationComponents = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob),
                    moveEntityDone = false,
                };
                this.Dependency = job.Schedule();
                //Wait for job completion
                Dependency.Complete();
                job.entities.Dispose();
                job.translationComponents.Dispose();
                entityToMove[0] = job.entityToMove[0];
                initLookForEntity = false;
                translationComponent[0] = job.translationComponent[0];
                job.translationComponent.Dispose();
                moveEntityDone[0] = job.moveEntityDone;

                job.entityToMove.Dispose();
            }*/
            if(EntityManager.Exists(entityToMove[0]))
            {

                translationComponent[0] = EntityManager.GetComponentData<Translation>(entityToMove[0]);
                var job2 = new MoveEntity()
                {
                    entityToMove = new NativeArray<Entity>(entityToMove, Allocator.TempJob),
                    translation = new NativeArray<Translation>(this.translationComponent, Allocator.TempJob),
       
                    target = target,
                    moveSpeed = Time.DeltaTime * speed,
                    newPosition = this.newPosition
                };
               
                Dependency = job2.Schedule(Dependency);
                //Wait for job completion
                Dependency.Complete();
                this.translationComponent[0] = job2.translation[0];
                job2.entityToMove.Dispose();
                job2.translation.Dispose();
                target[0] = job2.target[0];
                EntityManager.SetComponentData<Translation>(entityToMove[0], translationComponent[0]);
                //job2.target.Dispose();
            }
           
            translationComponent.Dispose();
            entities.Dispose();
            moveEntityDone.Dispose();
        }

        [BurstCompile]
        public struct FindEntity : IJob
        {
             [ReadOnly]internal NativeArray<Entity> entities;
             internal NativeArray<Translation> translationComponent;
             [ReadOnly] internal int id;
             [ReadOnly] internal int version;
             internal bool moveEntityDone;
             internal NativeArray<Entity> entityToMove;
             [ReadOnly]internal NativeArray<Translation> translationComponents;
             

            public void Execute()
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].Index == id &&
                        entities[i].Version == version)
                    {
                        entityToMove[0] = entities[i];
                        moveEntityDone = false;
                        translationComponent[0] = translationComponents[i];
                    } 
                }
            }
        }

        [BurstCompile]
        public struct MoveEntity : IJob
        {
            internal NativeArray<Entity> entityToMove;
            internal NativeArray<Translation> translation;
            internal NativeArray<GeometryDataModels.Target> target;
            internal GeometryDataModels.Target targ;
            internal Vector3 newPosition;
            internal float moveSpeed;
            
            public void Execute()
            {
                 targ = target[0];
                targ.position = Vector3.MoveTowards(targ.position, newPosition, moveSpeed * 0.1f);
                target[0] = targ;
                var trans = translation[0];
                trans.Value = targ.position;
                translation[0] = trans;
            }
        }

        public Vector3 MoveEntityToPosition(int entityId, int entityVersion, Vector3 newPosition,
            ref GeometryDataModels.Target closestTarget, float speedMultiplier)
        {
            this.speedMultiplier = speedMultiplier;
            this.newPosition = newPosition;
            this.entityVersionToTransform = entityVersion;
            this.entityIdToTransform = entityId;
            target = new NativeArray<GeometryDataModels.Target>(1, Allocator.TempJob);
            entityToMove = new NativeArray<Entity>(1, Allocator.TempJob);
            entityToMove[0] = closestTarget.entity;
            this.target[0] = closestTarget;
            this.Update();

            var toReturn= this.target[0].position;
            closestTarget = target[0];
            target.Dispose();
            entityToMove.Dispose();
            return toReturn;
        }
    }
}