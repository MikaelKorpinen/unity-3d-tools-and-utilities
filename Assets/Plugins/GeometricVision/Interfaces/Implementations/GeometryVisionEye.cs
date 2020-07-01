using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.Utilities;
using Unity.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
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
    [DisallowMultipleComponent]
    public class GeometryVisionEye : MonoBehaviour, IGeoEye
    {
        public string Id { get; set; }
        [SerializeField] private bool debugMode;
        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private int lastCount = 0;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();

        [SerializeField] public HashSet<Transform> seenTransforms;

        [SerializeField, Tooltip(" Geometry is extracted from collider instead of renderers mesh")]
        private bool targetColliderMeshes;

        [SerializeField] private List<VisionTarget> targetedGeometries = new List<VisionTarget>();
        private EyeDebugger _debugger;
   
        public Plane[] planes = new Plane[6];
        private GeometryVisionProcessor controllerProcessor;
        public GeometryVisionHead Head { get; set; }

        void Reset()
        {
            Initialize();
        }

        // Start is called before the first frame update
        void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
            Debugger = new EyeDebugger();
            seenTransforms = new HashSet<Transform>();
        }
        
        /// <summary>
        /// Updates visibility of the objects in the eye and processor/manager
        /// </summary>
        public void UpdateVisibility()
        {
            seenTransforms = UpdateObjectVisibility(controllerProcessor.GetAllObjects(), seenTransforms);
            SeenGeoInfos = UpdateGeometryVisibility(planes, controllerProcessor.GeoInfos, seenGeoInfos);
        }

        public NativeArray<GeometryDataModels.Edge> GetSeenEdges()
        {
            throw new NotImplementedException();
        }

        public GeometryVision GeoVision { get; }


        /// <summary>
        /// Update gameobject visibility. Object that do not have geometry in it
        /// </summary>
        private HashSet<Transform> UpdateObjectVisibility(List<Transform> listToCheck,
            HashSet<Transform> seenTransforms)
        {
            seenTransforms = new HashSet<Transform>();

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

        public void Debug()
        {
            if (DebugMode)
            {
                Debugger.Debug(GeoVision.Camera1, this, true);
            }
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

        public GeometryVisionProcessor ControllerProcessor
        {
            get
            {
                return (GeometryVisionProcessor) Head.GetProcessor<GeometryVisionProcessor>();
            }
            set { controllerProcessor = ControllerProcessor; }
        }
    }
}