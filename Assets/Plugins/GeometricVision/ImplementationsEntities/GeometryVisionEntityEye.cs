using System;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsEntities
{
    [AlwaysUpdateSystem]
    [DisableAutoCreation]
    [UpdateBefore(typeof(GeometryVisionEntityProcessor))]
    public class GeometryVisionEntityEye : SystemBase, IGeoEye
    {
        public string Id { get; set; }
        public GeometryVisionRunner Runner { get; set; }


        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;

        public GeometryVision GeoVision { get; set; }
        private EyeDebugger _debugger;
        private bool _addedByFactory;

        [SerializeField, Tooltip(" Geometry is extracted from collider instead of renderers mesh")]
        private bool targetColliderMeshes;

        private List<TargetingInstruction> targetingInstructions = new List<TargetingInstruction>();
        private int lastCount;
        [System.ComponentModel.ReadOnly(true)] private EntityManager entityManager;
        private BeginInitializationEntityCommandBufferSystem entityCommandBuffer;


        protected override void OnCreate()
        {
            entityManager = EntityManager;
            entityCommandBuffer = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            Debugger = new EyeDebugger();
        }

        /// <summary>
        /// Checks if objects are targeted. At least one GeometryType.Objects_ needs to be in the list in order for the plugin to see something that it can use
        /// </summary>
        /// <param name="targetedGeometries"></param>
        /// <returns></returns>
        bool isObjectsTargeted(List<TargetingInstruction> targetedGeometries)
        {
            bool objectsTargetingTypeFound = false;
            foreach (var geometryType in targetedGeometries)
            {
                if (geometryType.GeometryType == GeometryType.Objects)
                {
                    objectsTargetingTypeFound = true;
                }
            }

            return objectsTargetingTypeFound;
        }

        /// <summary>
        /// Here is handled object visibility processing and filtering
        /// </summary>
        protected override void OnUpdate()
        {
            var entityQuery = GetEntityQuery(typeof(GeometryDataModels.Target));
            var planes = new NativeArray<Plane>(6, Allocator.TempJob);

            planes.CopyFrom(GeoVision.Planes);
        //    CheckVisibilityDebug(GeoVision.Planes,  entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.Temp),entityQuery,entityQuery.ToEntityArray(Allocator.Temp) );
            var job2 = new CheckVisibility()
            {
                targets = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.TempJob),
            //    commandBuffer = commandBuffer,
                planes = new NativeArray<Plane>(6, Allocator.TempJob),
                entities = entityQuery.ToEntityArray(Allocator.TempJob)
            };
            job2.planes.CopyFrom(planes);
            Dependency = job2.Schedule(job2.targets.Length, 6);
            Dependency.Complete();
            //Wait for job completion

           
            entityCommandBuffer.AddJobHandleForProducer(Dependency);
            entityQuery.CopyFromComponentDataArray<GeometryDataModels.Target>(job2.targets);

            lastCount = entityQuery.CalculateEntityCount();
            job2.planes.Dispose();
            job2.entities.Dispose();
            job2.targets.Dispose();
            planes.Dispose();
            /*
            bool objectsTargeted = false, linesTargeted = false;
            for (var index = 0; index < targetingInstructions.Count; index++)
            {
                var geometryType = targetingInstructions[index];
                if (geometryType.GeometryType == GeometryType.Objects && geometryType.Enabled)
                {
                    objectsTargeted = true;
                }

                if (geometryType.GeometryType == GeometryType.Lines && geometryType.Enabled)
                {
                    linesTargeted = true;
                }
            }
            */
        }

        [BurstCompile]
        public struct CheckVisibility : IJobParallelFor
        {
            public NativeArray<GeometryDataModels.Target> targets;

            [System.ComponentModel.ReadOnly(true)] [NativeDisableParallelForRestriction]
            public NativeArray<Plane> planes;

          //  public EntityCommandBuffer.Concurrent commandBuffer;
            public NativeArray<Entity> entities;

            public void Execute(int index)
            {
                GeometryDataModels.Target target = targets[index];
                target.isSeen = IsInsideFrustum(target.position, planes);
                targets[index] = target;
               // commandBuffer.SetComponent(index, entities[index], targets[index] );
            }
        }

        public void CheckVisibilityDebug(Plane[] planes, NativeArray<GeometryDataModels.Target> targets,EntityQuery entityQuery, NativeArray<Entity> entities)
        {
            
            for (int i = 0; i < targets.Length; i++)
            {
                GeometryDataModels.Target target = targets[i];
                target.isSeen = IsInsideFrustum(target.position, planes);
                targets[i] = target;
                EntityManager.SetComponentData<GeometryDataModels.Target>(entities[i], targets[i]);
                Debug.Log( target.isSeen);
    

            }
            entityQuery.CopyFromComponentDataArray<GeometryDataModels.Target>(targets);
            targets.Dispose();
            entities.Dispose();
        }

        public static bool IsInsideFrustum(GeometryDataModels.Edge edge, NativeArray<Plane> planes)
        {
            for (var index = 0; index < planes.Length; index++)
            {
                if (planes[index].GetDistanceToPoint(edge.firstVertex) < 0 ||
                    planes[index].GetDistanceToPoint(edge.secondVertex) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInsideFrustum(Vector3 point, NativeArray<Plane> planes)
        {
            for (var index = 0; index < planes.Length; index++)
            {
                var plane = planes[index];
                if (plane.GetDistanceToPoint(point) < 0)
                {
                    return false;
                }
            }

            return true;
        }
        public static bool IsInsideFrustum(Vector3 point, Plane[] planes)
        {
            for (var index = 0; index < planes.Length; index++)
            {
                var plane = planes[index];
                if (plane.GetDistanceToPoint(point) < 0)
                {
                    return false;
                }
            }

            return true;
        }
        private void ProcessTargetedGeometriesVisibility(Entity entity, Translation translation,
            GeometryDataModelsEntities.Visible visible)
        {
            foreach (var geometryType in TargetingInstructions)
            {
                if (geometryType.GeometryType == GeometryType.Objects && geometryType.Enabled)
                {
                    visible = UpdateEntityVisibility(translation, visible);
                }

                if (geometryType.GeometryType == GeometryType.Lines && geometryType.Enabled)
                {
                    var edgeBuffer = EntityManager.GetBuffer<GeometryDataModelsEntities.EdgesBuffer>(entity);
                    // MeshUtilities.UpdateEdgesVisibility(planes, edgeBuffer);
                }
            }
        }

        private GeometryDataModelsEntities.Visible UpdateEntityVisibility(Translation translation,
            GeometryDataModelsEntities.Visible visible)
        {
            if (MeshUtilities.IsInsideFrustum(translation.Value, GeoVision.Planes))
            {
                visible.IsVisible = GeometryDataModels.Boolean.True;
                //    lastCount = seenTransforms.Count;
            }

            return visible;
        }
        ///<inheritdoc cref="IGeoEye"/>
        /// <param name="useBounds"></param>
        public void UpdateVisibility(bool useBounds)
        {
            this.Update();
        }

        public NativeArray<GeometryDataModels.Edge> GetSeenEdges()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Hides Edges, vertices, geometryObject outside th frustum
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="allGeoInfos"></param>
        private List<GeometryDataModels.GeoInfo> UpdateGeometryVisibility(Plane[] planes,
            List<GeometryDataModels.GeoInfo> allGeoInfos, List<GeometryDataModels.GeoInfo> seenGeometry)
        {
            int geoCount = allGeoInfos.Count;
            seenGeometry = new List<GeometryDataModels.GeoInfo>();

            UpdateSeenGeometryObjects(allGeoInfos, seenGeometry, geoCount);

            foreach (var geometryType in TargetingInstructions)
            {
                if (geometryType.GeometryType == GeometryType.Lines && geometryType.Enabled)
                {
                    MeshUtilities.UpdateEdgesVisibilityParallel(planes, seenGeometry);
                }
            }

            return seenGeometry;
        }

        /// <summary>
        /// Updates object collection containing geometry and data related to seen object. Usage is to internally update seen geometry objects by checking objects renderer bounds
        /// against eyes/cameras frustum
        /// </summary>
        /// <param name="allGeoInfos"></param>
        /// <param name="seenGeometry"></param>
        /// <param name="geoCount"></param>
        private void UpdateSeenGeometryObjects(List<GeometryDataModels.GeoInfo> allGeoInfos,
            List<GeometryDataModels.GeoInfo> seenGeometry, int geoCount)
        {
            for (var i = 0; i < geoCount; i++)
            {
                var geInfo = allGeoInfos[i];

                if (GeometryUtility.TestPlanesAABB(GeoVision.Planes, allGeoInfos[i].renderer.bounds) &&
                    hideEdgesOutsideFieldOfView)
                {
                    seenGeometry.Add(geInfo);
                }
            }
        }

        private HashSet<Transform> GetObjectsInsideFrustum(HashSet<Transform> seenTransforms,
            List<Transform> allTransforms)
        {
            return seenTransforms;
        }

        public List<TargetingInstruction> TargetingInstructions
        {
            get { return targetingInstructions; }
            set { targetingInstructions = value; }
        }

        public EyeDebugger Debugger
        {
            get { return _debugger; }
            set { _debugger = value; }
        }

        public bool TargetColliderMeshes
        {
            get { return targetColliderMeshes; }
            set { targetColliderMeshes = value; }
        }
    }
}