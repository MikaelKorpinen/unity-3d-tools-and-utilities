using System;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.Utilities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    /// <summary>
    /// Class that is responsible for seeing objects and geometry.
    /// It checks, if object is inside visibility area and filters out unwanted objects and geometry.
    ///
    /// Usage: Add to objects you want it to. The component will handle the rest. Component has list of geometry types.
    /// These are used to see certain type of objects and clicking the targeting option from the inspector UI the user can
    /// add option to find the closest element of this type.
    /// </summary>
    public class GeometryVisionEntityEye : SystemBase, IGeoEye
    {
        public GeometryVisionEntityEye ()
        {
            enabled = true;
        }

        public string Id { get; set; }
        private bool enabled = false;
        [SerializeField] private bool debugMode;
        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();

        public GeometryVision GeoVision { get; set; }
        public Plane[] planes = new Plane[6];
        [SerializeField] public HashSet<Transform> seenTransforms;
        private EyeDebugger _debugger;
        private bool _addedByFactory;
        [SerializeField,  Tooltip(" Geometry is extracted from collider instead of renderers mesh")] private bool targetColliderMeshes;
        [SerializeField] private List<VisionTarget> targetedGeometries = new List<VisionTarget>(); 

        private IDisposable entityToggleObservable = null;
        private int lastCount;

        void OnCreate()
        {
            Initialize();
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

        protected override void OnUpdate()
        {
           // UpdateVisibility();
           // Debug();
        }

        /// <summary>
        /// Updates visibility of the objects in the eye and brain/manager
        /// </summary>
        /// <param name="seenTransforms"></param>
        /// <param name="seenGeoInfos"></param>
        public void UpdateVisibility(List<Transform> objectsToUpdate, List<GeometryDataModels.GeoInfo> geoInfos)
        {
            if (enabled)
            {
                this.seenTransforms = UpdateObjectVisibility(GeoVision.Head.GetProcessor<GeometryVisionEntityProcessor>().GetAllObjects(), seenTransforms);
                SeenGeoInfos = UpdateGeometryVisibility(planes, GeoVision.Head.GetProcessor<GeometryVisionEntityProcessor>().GeoInfos, seenGeoInfos);
            }
        }

        public NativeArray<GeometryDataModels.Edge> GetSeenEdges()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update gameobject visibility. Object that do not have geometry in it
        /// </summary>
        private HashSet<Transform> UpdateObjectVisibility(List<Transform> listToCheck,
            HashSet<Transform> seenTransforms)
        {

            seenTransforms = GetObjectsInsideFrustum(seenTransforms, listToCheck);


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
            foreach (var transform in allTransforms)
            {
                if (MeshUtilities.IsInsideFrustum(transform.position, planes))
                {
                    seenTransforms.Add(transform);
                    lastCount = seenTransforms.Count;
                }
            }

            return seenTransforms;
        }
        
        public List<VisionTarget> TargetedGeometries
        {
            get { return targetedGeometries; }
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