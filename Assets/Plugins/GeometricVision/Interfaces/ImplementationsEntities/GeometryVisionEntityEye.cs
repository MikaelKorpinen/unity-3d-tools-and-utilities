using System;
using System.Collections.Generic;
using Plugins.GeometricVision.EntityScripts;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Plugins.GeometricVision.GeometryDataModels.Boolean;

namespace Plugins.GeometricVision.Interfaces.ImplementationsEntities
{
    [AlwaysUpdateSystem]
    [DisableAutoCreation]
    public class GeometryVisionEntityEye : SystemBase, IGeoEye
    {
        public GeometryVisionEntityEye()
        {

        }

        public string Id { get; set; }

        [SerializeField] private bool debugMode;
        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();

        public GeometryVision GeoVision { get; set; }
        public Plane[] planes = new Plane[6];
        [SerializeField] public HashSet<Transform> seenTransforms;
        private EyeDebugger _debugger;
        private bool _addedByFactory;

        [SerializeField, Tooltip(" Geometry is extracted from collider instead of renderers mesh")]
        private bool targetColliderMeshes;

        [SerializeField] private List<VisionTarget> targetedGeometries = new List<VisionTarget>();

        private IDisposable entityToggleObservable = null;
        private int lastCount;
        private EntityQuery query = new EntityQuery();
        private BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        protected override void OnCreate()
        {
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            Initialize();
            Enabled = false;
        }

        private void Initialize()
        {
            if (isObjectsTargeted(targetedGeometries) == false)
            {
                targetedGeometries = new List<VisionTarget>();
                IGeoTargeting targeting = new GeometryObjectTargeting();
                targetedGeometries.Add(new VisionTarget(GeometryType.Objects, 0, targeting));
            }

            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
            Debugger = new EyeDebugger();
            seenTransforms = new HashSet<Transform>();
        }

        /// <summary>
        /// Checks if objects are targeted. At least one GeometryType.Objects_ needs to be in the list in order for the plugin to see something that it can use
        /// </summary>
        /// <param name="targetedGeometries"></param>
        /// <returns></returns>
        bool isObjectsTargeted(List<VisionTarget> targetedGeometries)
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
        /// Here is handled object visibility proessing and filtering
        /// </summary>
        protected override void OnUpdate()
        {
            // UpdateVisibility();

            query = GetEntityQuery(
                ComponentType.ReadOnly<GeoInfoEntityComponent>()
            );

            lastCount = query.CalculateEntityCount();
            Debug.Log(lastCount);
            // Schedule job to check visibilities
            NativeArray<Plane> planes2 = new NativeArray<Plane>(planes, Allocator.Temp);
            bool objectsTargeted = false, linesTargeted = false;
            for (var index = 0; index < targetedGeometries.Count; index++)
            {
                var geometryType = targetedGeometries[index];
                if (geometryType.GeometryType == GeometryType.Objects && geometryType.Enabled)
                {
                    objectsTargeted = true;
                }

                if (geometryType.GeometryType == GeometryType.Lines && geometryType.Enabled)
                {
                    linesTargeted = true;
                }
            }

            GeometryDataModels.Edge edge = new GeometryDataModels.Edge();
            Entities
                .WithStoreEntityQueryInField(ref query)
                .WithName("GeoInfoVisibility_Checks")
                .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
                .ForEach((ref Translation translation, ref Visible visible, ref Entity entity,
                    in GeoInfoEntityComponent geoInfo) =>
                {
                    //   float deltaTime = Time.DeltaTime;

                    if (objectsTargeted)
                    {
                        if (IsInsideFrustum(translation.Value, planes2))
                        {
                            visible.IsVisible = GeometryDataModels.Boolean.True;
                        }
                    }

                    if (linesTargeted)
                    {
                        var edgeBuffer = geoInfo.Edges;

                        for (var index = 0; index < edgeBuffer.Length; index++)
                        {
                            var edgeBuf = edgeBuffer[index];
                            if (IsInsideFrustum(edgeBuffer[index], planes2))
                            {
                                edgeBuf.isVisible = True;
                            }
                            else
                            {
                                edgeBuf.isVisible = False;
                            }

                            edgeBuffer[index] = edge;
                        }

                        //  MeshUtilities.UpdateEdgesVisibility(planes2, edgeBuffer);
                    }


                    //    ProcessTargetedGeometriesVisibility(entity, translation, visible);
                })
                .ScheduleParallel();
            // this.seenTransforms = UpdateObjectVisibility( );
            //     SeenGeoInfos = UpdateGeometryVisibility(planes, GeoVision.Head.GetProcessor<GeometryVisionEntityProcessor>().GeoInfos, seenGeoInfos);
            //   enabled = false;
            //   run = false;

            // Debug();
            lastCount = query.CalculateEntityCount();
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

        private void ProcessTargetedGeometriesVisibility(Entity entity, Translation translation, Visible visible)
        {
            foreach (var geometryType in TargetedGeometries)
            {
                if (geometryType.GeometryType == GeometryType.Objects && geometryType.Enabled)
                {
                    visible = UpdateEntityVisibility(translation, visible);
                }

                if (geometryType.GeometryType == GeometryType.Lines && geometryType.Enabled)
                {
                    var edgeBuffer = EntityManager.GetBuffer<EdgesBuffer>(entity);
                    // MeshUtilities.UpdateEdgesVisibility(planes, edgeBuffer);
                }
            }
        }

        private Visible UpdateEntityVisibility(Translation translation, Visible visible)
        {
            if (MeshUtilities.IsInsideFrustum(translation.Value, planes))
            {
                visible.IsVisible = GeometryDataModels.Boolean.True;
                lastCount = seenTransforms.Count;
            }

            return visible;
        }

        /// <summary>
        /// Updates visibility status of the objects
        /// </summary>
        public void UpdateVisibility()
        {
            this.Enabled = true;
            Update();
        }

        public NativeArray<GeometryDataModels.Edge> GetSeenEdges()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update gameobject visibility. Object that do not have geometry in it
        /// </summary>
        private HashSet<Transform> UpdateObjectVisibility()
        {
            //    seenTransforms = GetObjectsInsideFrustum(seenTransforms, listToCheck);

            return seenTransforms;
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

            foreach (var geometryType in TargetedGeometries)
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

                if (GeometryUtility.TestPlanesAABB(planes, allGeoInfos[i].renderer.bounds) &&
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

        public List<VisionTarget> TargetedGeometries
        {
            get { return targetedGeometries; }
            set { targetedGeometries = value; }
        }

        public List<GeometryDataModels.GeoInfo> SeenGeoInfos
        {
            get { return seenGeoInfos; }
            set { seenGeoInfos = value; }
        }

        public Plane[] Planes
        {
            get { return planes; }
            set { planes = value; }
        }

        public bool DebugMode
        {
            get { return debugMode; }
            set { debugMode = value; }
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