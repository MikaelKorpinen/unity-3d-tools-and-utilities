using System.Collections.Generic;
using Plugins.GeometricVision.EntityScripts;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.ImplementationsEntities
{
    /// <inheritdoc />
    [AlwaysUpdateSystem]     [DisableAutoCreation]
    public class GeometryVisionEntityProcessor : SystemBase, IGeoProcessor
    {
        public GeometryVisionEntityProcessor()
        {

        }
        
        private int lastCount = 0;
        private bool collidersTargeted;

        private bool extractGeometry;
        private bool calculateEntities = true;
        private BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        private int currentObjectCount;
        private List<VisionTarget> _targetedGeometries = new List<VisionTarget>();
        private GeometryVision geoVision;
        private EntityQuery entityQuery = new EntityQuery();


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
            currentObjectCount = entityQuery.CalculateEntityCountWithoutFiltering();
            entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<Translation>()
            );
            lastCount = entityQuery.CalculateEntityCount();
           UnityEngine.Debug.Log("entities " +lastCount);
            // Schedule the job that will add Instantiate commands to the EntityCommandBuffer.
            // Since this job only runs on the first frame, we want to ensure Burst compiles it before running to get the best performance (3rd parameter of WithBurst)
            // The actual job will be cached once it is compiled (it will only get Burst compiled once).
            Entities
                .WithStoreEntityQueryInField(ref entityQuery)
                .WithName("geos")

                .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
                .ForEach((Entity entity, int entityInQueryIndex, in Spawner_SpawnAndRemove spawner,
                    in LocalToWorld location) =>
                {
                    for (var x = 0; x < spawner.CountX; x++)
                    {
                        for (var y = 0; y < spawner.CountY; y++)
                        {
                            var instance = commandBuffer.Instantiate(entityInQueryIndex, spawner.Prefab);

                            // Place the instantiated in a grid with some noise
                            var position = math.transform(location.Value,
                                new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F));
                            commandBuffer.SetComponent(entityInQueryIndex, instance,
                                new Translation {Value = position});
                            //   commandBuffer.SetComponent(entityInQueryIndex, instance, new LifeTime { Value = 100f });
                        }
                    }

                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }).ScheduleParallel();
            float deltaTime = Time.DeltaTime;

            // Schedule job to rotate around up vector
            Entities
                .WithName("RotationSpeedSystem_ForEach")
                .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
                .ForEach((ref Rotation rotation, in RotationSpeed_ForEach rotationSpeed) =>
                {
                    rotation.Value = math.mul(
                        math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * deltaTime));
                })
                .ScheduleParallel();
            // SpawnJob runs in parallel with no sync point until the barrier system executes.
            // When the barrier system executes we want to complete the SpawnJob and then play back the commands
            // (Creating the entities and placing them). We need to tell the barrier system which job it needs to
            // complete before it can play back the commands.
            m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);

            
           // CheckSceneChanges(GeoVision);
            if (extractGeometry)
            {
                ExtractGeometry(commandBuffer,  _targetedGeometries);
                extractGeometry = false;
            }
            
        }
        
        /// <summary>
        /// Extracts geometry from Unity Mesh to geometry object
        /// </summary>
        /// <param name="commandBuffer"></param>
        /// <param name="geoInfos"></param>
        private void ExtractGeometry(EntityCommandBuffer.Concurrent commandBuffer, List<VisionTarget> targetedGeometries)
        {
           // var tG = targetedGeometries;
           if (geometryIsTargeted(targetedGeometries, GeometryType.Lines))
           {
               List <GeometryDataModels.GeoInfo> geos = new List<GeometryDataModels.GeoInfo>();
               GeometryDataModels.GeoInfo geo = new GeometryDataModels.GeoInfo();
               Entities
                   .WithChangeFilter<GeoInfoEntityComponent>()
                   .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
                   .ForEach(( DynamicBuffer<VerticesBuffer> vBuffer, DynamicBuffer<TrianglesBuffer> tBuffer, LocalToWorldMatrix localToWorldMatrix, DynamicBuffer<EdgesBuffer> edgeBuffer,  GeoInfoEntityComponent geoE)  =>
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
            Enabled = true;
            Update();
            this.GeoVision = geoVision;
            if (currentObjectCount != lastCount)
            {
                lastCount = currentObjectCount;
                extractGeometry = true;
                _targetedGeometries = geoVision.TargetedGeometries;
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