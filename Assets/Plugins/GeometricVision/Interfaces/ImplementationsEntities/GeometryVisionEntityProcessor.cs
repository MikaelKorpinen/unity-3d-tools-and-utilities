using System.Collections.Generic;
using System.ComponentModel;
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
    [UpdateAfter(typeof(GeometryVisionEntityEye))]
    public class GeometryVisionEntityProcessor : SystemBase, IGeoProcessor
    {
        private int lastCount = 0;
        private bool collidersTargeted;

        private bool extractGeometry;
        private BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        [System.ComponentModel.ReadOnly(true)] public EntityCommandBuffer.Concurrent ConcurrentCommands;
        private int currentObjectCount;
        private List<VisionTarget> _targetedGeometries = new List<VisionTarget>();
        private GeometryVision geoVision;
        private EntityQuery entityQuery = new EntityQuery();
        private EntityManager entityManager;


        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            entityManager = World.EntityManager;
        }

        protected override void OnUpdate()
        {

            var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
            var localEntityManager = entityManager;


            EntityQuery entitiesWithoutTargetComponent = GetEntityQuery(
                new EntityQueryDesc()
                {
                    None = new ComponentType[] {typeof(GeometryDataModels.Target)},
                }

                );

            if ( entitiesWithoutTargetComponent.CalculateEntityCount() != 0)
            {
                entityManager.AddComponent<GeometryDataModels.Target>( entitiesWithoutTargetComponent.ToEntityArray(Allocator.Temp));
            }

            entityQuery = GetEntityQuery(typeof(Translation),typeof(GeometryDataModels.Target) );
            
            UnityEngine.Debug.Log("entityQuery.ToEntityArray " + entityQuery.ToEntityArray(Allocator.Temp).Length);


            var job2 = new ModifyTargets()
            {
                targets = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.Temp),
                translations = entityQuery.ToComponentDataArray<Translation>(Allocator.Temp),
            };


            this.Dependency = job2.Schedule(job2.targets.Length, 6);
            this.Dependency.Complete();
            
            entityQuery.CopyFromComponentDataArray<Translation>(job2.translations);
            entityQuery.CopyFromComponentDataArray<GeometryDataModels.Target>(job2.targets);
            job2.translations.Dispose();
            job2.targets.Dispose();

            currentObjectCount = entityQuery.CalculateEntityCountWithoutFiltering();
            UnityEngine.Debug.Log("currentObjectCount " + currentObjectCount);

            entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<GeoInfoEntityComponent>()
            );


            Entities
                .WithStoreEntityQueryInField(ref entityQuery)
                .WithName("GeometryVision")
                .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
                .ForEach((Entity entity, int entityInQueryIndex, in GeoInfoEntityComponent geoInfo,
                    in LocalToWorld location) =>
                {
                    //  commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }).ScheduleParallel();
            float deltaTime = Time.DeltaTime;


            m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);


            // CheckSceneChanges(GeoVision);
            if (extractGeometry)
            {
                ExtractGeometry(commandBuffer, _targetedGeometries);
                extractGeometry = false;
            }
        }
        
        [BurstCompile]
        public struct ModifyTargets : IJobParallelFor
        {
            [System.ComponentModel.ReadOnly(true)] public NativeArray<GeometryDataModels.Target> targets;

            [System.ComponentModel.ReadOnly(true)] public NativeArray<Translation> translations;

            public void Execute(int index)
            {
                GeometryDataModels.Target target = targets[index];
                target.position = translations[index].Value;
                targets[index] = target;
            }
        }
        
        /// <summary>
        /// Extracts geometry from Unity Mesh to geometry object
        /// </summary>
        /// <param name="commandBuffer"></param>
        /// <param name="geoInfos"></param>
        private void ExtractGeometry(EntityCommandBuffer.Concurrent commandBuffer,
            List<VisionTarget> targetedGeometries)
        {
            // var tG = targetedGeometries;
            if (geometryIsTargeted(targetedGeometries, GeometryType.Lines))
            {
                List<GeometryDataModels.GeoInfo> geos = new List<GeometryDataModels.GeoInfo>();
                // GeometryDataModels.GeoInfo geo = new GeometryDataModels.GeoInfo();
                Entities
                    .WithChangeFilter<GeoInfoEntityComponent>()
                    .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
                    .ForEach((DynamicBuffer<VerticesBuffer> vBuffer, DynamicBuffer<TrianglesBuffer> tBuffer,
                        LocalToWorldMatrix localToWorldMatrix, DynamicBuffer<EdgesBuffer> edgeBuffer,
                        GeoInfoEntityComponent geoE) =>
                    {
                        //    MeshUtilities.BuildEdgesFromNativeArrays(localToWorldMatrix.Matrix, tBuffer, vBuffer).CopyTo(geo.edges);
                        //      geos.Add(geo);
                    }).ScheduleParallel();
            }

            if (geometryIsTargeted(targetedGeometries, GeometryType.Vertices))
            {
            }
        }

        /// <summary>
        /// Used to check, if things inside scene has changed. Like if new object has been removed or moved.
        /// </summary>
        public void CheckSceneChanges(GeometryVision geoVision)
        {
            Update();
            this.GeoVision = geoVision;
            if (currentObjectCount != lastCount)
            {
                lastCount = currentObjectCount;
                extractGeometry = true;
                _targetedGeometries = geoVision.TargetingInstructions;
            }
        }

        /// <summary>
        /// Check if user has selected mesh geometry as target for the operation
        /// </summary>
        /// <param name="targetedGeometries"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        private bool geometryIsTargeted(List<VisionTarget> targetedGeometries, GeometryType type)
        {
            bool found = false;
            foreach (var visionTarget in targetedGeometries)
            {
                if (visionTarget.GeometryType == type)
                {
                    found = true;
                }
            }

            return found;
        }

        public int CountSceneObjects()
        {
            return currentObjectCount;
        }

        public HashSet<Transform> GetTransforms(List<GameObject> objs)
        {
            throw new System.NotImplementedException();
        }

        public void Debug(GeometryVision geoVisions)
        {
        }

        public List<Transform> GetAllObjects()
        {
            throw new System.NotImplementedException();
        }

        public GeometryVision GeoVision
        {
            get { return geoVision; }
            set { geoVision = value; }
        }
    }
}