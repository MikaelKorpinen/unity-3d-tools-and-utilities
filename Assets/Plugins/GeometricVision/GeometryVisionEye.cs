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
    public class GeometryVisionEye : MonoBehaviour
    {
        [SerializeField] private bool debugMode;
        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private int lastCount = 0;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
        [SerializeField] private IGeoBrain controllerBrain;
        private new Camera camera;
        public Plane[] planes = new Plane[6];
        [SerializeField] public HashSet<Transform> seenTransforms;
        private EyeDebugger _debugger;
        private bool _addedByFactory;

        [SerializeField] private List<VisionTarget>
            targetedGeometries =
                new List<VisionTarget>(); //TODO: Make it reactive and dispose subscribers on array resize in case they are not cleaned up by the gc

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

        void OnValidate()
        {
            InitializeTargeting(TargetedGeometries);
        }

        private void Initialize()
        {
            if (isObjectsTargeted(targetedGeometries) == false)
            {
                targetedGeometries = new List<VisionTarget>();
                IGeoTargeting targeting = new GeometryObjectTargeting();
                targetedGeometries.Add(new VisionTarget(GeometryType.Objects, 0, targeting));
            }

            Camera1 = gameObject.GetComponent<Camera>();
            if (Camera1 == null)
            {
                gameObject.AddComponent<Camera>();
                Camera1 = gameObject.GetComponent<Camera>();
            }

            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
            ControllerBrain =
                GeometryVisionUtilities.getControllerFromGeometryManager(FindObjectOfType<GeometryVisionHead>(), this);
            Debugger = new EyeDebugger();
            seenTransforms = new HashSet<Transform>();
            Camera1.enabled = false;
            Debugger.Planes = RegenerateVisionArea(fieldOfView, planes);

            InitializeTargeting(TargetedGeometries);
        }

        /// <summary>
        /// Handles target initialization. Adds needed components and subscribes changing variables to logic that updates the targeting system.
        /// So it can keep working under use.
        /// </summary>
        void InitializeTargeting(List<VisionTarget> targets)
        {
            foreach (var geometryType in targets)
            {
                if (geometryType.Target.Value == true)
                {
                    var geoTargeting = AssignGeometryTargeting();
                    AssignActionsForTargeting(geometryType, targets.IndexOf(geometryType));
                    OnTargetingEnabled(geometryType, geoTargeting);
                }
            }
        }

        private GeometryTargeting AssignGeometryTargeting()
        {
            var geoTargeting = gameObject.GetComponent<GeometryTargeting>();
            if (gameObject.GetComponent<GeometryTargeting>() == null)
            {
                gameObject.AddComponent<GeometryTargeting>();
                geoTargeting = gameObject.GetComponent<GeometryTargeting>();
            }

            return geoTargeting;
        }

        private void AssignActionsForTargeting(VisionTarget geometryType, int indexOf)
        {
            if (geometryType.targetingActions == null)
            {
                var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
                newActions.name += "_" + indexOf;
                geometryType.targetingActions = newActions;
            }
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
                UnityEngine.Debug.Log(geometryType.Target);
                if (geometryType.GeometryType == GeometryType.Objects)
                {
                    objectsTargetingTypeFound = true;
                }
            }

            return objectsTargetingTypeFound;
        }

        /// <summary>
        /// Add targeting implementation based on, if it is enabled on the inspector.
        /// Subscribes the targeting toggle button to functionality than handles creation of targeting implementation for the
        /// targeted geometry type
        /// </summary>
        /// <param name="geometryType"></param>
        /// <param name="geoTargeting"></param>
        private void OnTargetingEnabled(VisionTarget geometryType, GeometryTargeting geoTargeting)
        {
            if (!geometryType.Subscribed)
            {
                geometryType.Target.Subscribe(targeting =>
                {
                    if (targeting)
                    {
                        //Cannot get Reactive value from serialized property, so this boolean variable handles it job on the inspector gui under the hood.
                        //The other way is to find out how to get reactive value out of serialized property
                        geometryType.TargetHidden = true;
                        geoTargeting.AddTarget(geometryType);
                    }
                    else
                    {
                        geometryType.TargetHidden = false;
                        geoTargeting.RemoveTarget(geometryType);
                    }
                });
                geometryType.Subscribed = true;
            }
        }

        private static void RefreshTargeting(List<VisionTarget> targets, string message)
        {
            UnityEngine.Debug.Log(message);
            foreach (var visionTarget in targets)
            {
                if (visionTarget.TargetingSystem == null)
                {
                    if (visionTarget.GeometryType == GeometryType.Objects)
                    {
                        visionTarget.TargetingSystem = new GeometryObjectTargeting();
                    }

                    if (visionTarget.GeometryType == GeometryType.Lines)
                    {
                        visionTarget.TargetingSystem = new GeometryLineTargeting();
                    }
                }
                else if (visionTarget.GeometryType != visionTarget.TargetingSystem.TargetedType)
                {
                    if (visionTarget.GeometryType == GeometryType.Objects)
                    {
                        visionTarget.TargetingSystem = new GeometryObjectTargeting();
                    }

                    if (visionTarget.GeometryType == GeometryType.Lines)
                    {
                        visionTarget.TargetingSystem = new GeometryLineTargeting();
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
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
                Debugger.Debug(Camera1, SeenGeoInfos, true);
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
    }
}