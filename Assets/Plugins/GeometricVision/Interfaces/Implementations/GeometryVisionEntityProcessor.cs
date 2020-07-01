using System.Collections.Generic;
using System.Linq;
using GeometricVision;
using Plugins.GeometricVision.Utilities;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = UnityEngine.Random;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    /// <inheritdoc />
    public class GeometryVisionEntityProcessor : SystemBase, IGeoProcessor
    {
        [SerializeField] private int lastCount = 0;
        [SerializeField] private List<GeometryDataModels.GeoInfo> _geoInfos = new List<GeometryDataModels.GeoInfo>();
        public HashSet<Transform> AllObjects;
        public List<GameObject> RootObjects;
        private bool collidersTargeted;

        private bool extractGeometry;
        private bool calculateEntities = true;
        BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        private int currentObjectCount;
        private List<VisionTarget> _targetedGeometries;
        private GeometryVision geometryVisiom;

        public GeometryVisionEntityProcessor()
        {
            GeoInfos = new List<GeometryDataModels.GeoInfo>();
            AllObjects = new HashSet<Transform>();
            RootObjects = new List<GameObject>();
        }

        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to
            // perform such changes on the main thread after the Job has finished. Command buffers allow you to perform
            // any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and
            // deletions for later.
            var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

            // Schedule the job that will add Instantiate commands to the EntityCommandBuffer.
            // Since this job only runs on the first frame, we want to ensure Burst compiles it before running to get the best performance (3rd parameter of WithBurst)
            // The actual job will be cached once it is compiled (it will only get Burst compiled once).
            Entities
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

    
            var query = new EntityQueryDesc
            {
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<Translation>()
                }
            };

            EntityQuery entityQuery = GetEntityQuery(query);
            this.currentObjectCount = entityQuery.CalculateEntityCount();
            CheckSceneChanges(geometryVisiom);
            if (extractGeometry)
            {
                ExtractGeometry(commandBuffer, GeoInfos, _targetedGeometries);
                extractGeometry = false;
            }
            
        }
        
        /// <summary>
        /// Extracts geometry from Unity Mesh to geometry object
        /// </summary>
        /// <param name="commandBuffer"></param>
        /// <param name="geoInfos"></param>
        private void ExtractGeometry(EntityCommandBuffer.Concurrent commandBuffer, List<GeometryDataModels.GeoInfo> geoInfos,  List<VisionTarget> targetedGeometries)
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
                       geo.edges = MeshUtilities.BuildEdgesFromNativeArrays(localToWorldMatrix.Matrix, tBuffer, vBuffer);

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
            geometryVisiom = geoVision;
            if (currentObjectCount != lastCount)
            {
                lastCount = currentObjectCount;
                extractGeometry = true;
               // _targetedGeometries = targetedGeometries;
            }
        }

        /// <summary>
        /// Gets all the trasforms from list of objects
        /// </summary>
        /// <param name="rootObjects"></param>
        /// <param name="targetTransforms"></param>
        /// <returns></returns>
        public void GetTransforms(List<GameObject> rootObjects, ref HashSet<Transform> targetTransforms)
        {
            int numberOfObjects = 0;

            for (var index = 0; index < rootObjects.Count; index++)
            {
                var root = rootObjects[index];
                targetTransforms.Add(root.transform);
                getObjectsInTransformHierarchy(root.transform, ref targetTransforms, numberOfObjects + 1);
            }
        }

        private static int getObjectsInTransformHierarchy(Transform root, ref HashSet<Transform> targetList,
            int numberOfObjects)
        {
            int childCount = root.childCount;
            for (var index = 0; index < childCount; index++)
            {
                targetList.Add(root.GetChild(index));
                getObjectsInTransformHierarchy(root.GetChild(index), ref targetList, numberOfObjects + 1);
            }

            return childCount;
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

        public void Debug(GeometryVision geoVisions)
        {
            
        }

        List<GeometryDataModels.GeoInfo> IGeoProcessor.GeoInfos()
        {
            return _geoInfos;
        }

        public HashSet<Transform> GetTransforms(List<GameObject> objs)
        {
            var result = new HashSet<Transform>();
            GetTransforms(objs, ref result);
            return result;
        }

        public List<Transform> GetAllObjects()
        {
            return AllObjects.ToList();
        }

        public List<GeometryDataModels.GeoInfo> GeoInfos
        {
            get { return _geoInfos; }
            set { _geoInfos = value; }
        }
    }
}