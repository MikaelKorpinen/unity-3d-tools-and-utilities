using System;
using System.Collections.Generic;
using GeometricVision;
using GeometricVision.Jobs;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.UI;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using Plugins.GeometricVision.Utilities;
using UniRx;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins.GeometricVision
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
        [SerializeField] private bool debugMode;
        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
        [SerializeField] private IGeoBrain controllerBrain;
        private new Camera camera;
        public Plane[] planes = new Plane[6];
        [SerializeField] public HashSet<Transform> seenTransforms;
        private EyeDebugger _debugger;
        private bool _addedByFactory;
        [SerializeField,  Tooltip(" Geometry is extracted from collider instead of renderers mesh")] private bool targetColliderMeshes;
        [SerializeField] private List<VisionTarget>
            targetedGeometries =
                new List<VisionTarget>(); //TODO: Make it reactive and dispose subscribers on array resize in case they are not cleaned up by the gc

        private IDisposable entityToggleObservable = null;
        private int lastCount;

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

            Debugger.Planes = RegenerateVisionArea(fieldOfView, planes);

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
            planes = RegenerateVisionArea(fieldOfView, planes);

            UpdateVisibility(seenTransforms, seenGeoInfos);
            Debug(); 
            
        }

        /// <summary>
        /// Updates visibility of the objects in the eye and brain/manager
        /// </summary>
        /// <param name="seenTransforms"></param>
        /// <param name="seenGeoInfos"></param>
        private void UpdateVisibility(HashSet<Transform> seenTransforms, List<GeometryDataModels.GeoInfo> seenGeoInfos)
        {
            //TODO: Check if this will be performance issue in case many eyes/cameras are present
            controllerBrain.CheckSceneChanges(targetedGeometries);

            this.seenTransforms = UpdateObjectVisibility(ControllerBrain.GetAllObjects(), seenTransforms);
            SeenGeoInfos = UpdateGeometryVisibility(planes, ControllerBrain.GeoInfos(), seenGeoInfos);
        }

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

        /// <summary>
        /// When the camera is moved, rotated or both the frustum planes that
        /// hold the system together needs to be refreshes/regenerated
        /// </summary>
        /// <param name="fieldOfView"></param>
        /// <returns>Plane[]</returns>
        /// <remarks>Faster way to get the current situation for planes might be to store planes into an object and move them with the eye</remarks>
        private Plane[] RegenerateVisionArea(float fieldOfView, Plane[] planes)
        {
            Camera1.enabled = true;
            Camera1.fieldOfView = fieldOfView;
            planes = GeometryUtility.CalculateFrustumPlanes(Camera1);
            Camera1.enabled = false;
            return planes;
        }

        /// <summary>
        /// When the camera is moved, rotated or both the frustum planes that
        /// hold the system together needs to be refreshes/regenerated
        /// </summary>
        /// <param name="fieldOfView"></param>
        /// <returns>void</returns>
        /// <remarks>Faster way to get the current situation for planes might be to store planes into an object and move them with the eye</remarks>
        public void RegenerateVisionArea(float fieldOfView)
        {
            Camera1.enabled = true;
            Camera1.fieldOfView = fieldOfView;
            planes = GeometryUtility.CalculateFrustumPlanes(Camera1);
            Camera1.enabled = false;
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
                Debugger.Debug(Camera1, controllerBrain, true);
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

        public Camera Camera1
        {
            get { return camera; }
            set { camera = value; }
        }

        public IGeoBrain ControllerBrain
        {
            get { return controllerBrain; }
            set { controllerBrain = value; }
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

        public BoolReactiveProperty EntityBasedProcessing
        {
            get { return entityBasedProcessing; }
            set { entityBasedProcessing = value; }
        }
    }
}