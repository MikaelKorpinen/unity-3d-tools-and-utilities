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
    /// Class that shows up as a controller for the user.
    /// It runs user enabled modules and systems.
    ///
    /// Usage: Add to objects you want to act as a camera/eye for geometry vision. The component will handle the rest. 
    /// A lot of the settings are meant to be adjusted from the inspector UI
    /// </summary>
    public class GeometryVision : MonoBehaviour
    {
        [SerializeField] private bool debugMode;
        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private int lastCount = 0;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
        [SerializeField] private List<IGeoBrain> geometryProcessors;
        private new Camera camera;
        public Plane[] planes = new Plane[6];
        [SerializeField] public HashSet<Transform> seenTransforms;
        private EyeDebugger _debugger;
        [SerializeField,  Tooltip(" Geometry is extracted from collider instead of renderers mesh")] private bool targetColliderMeshes;
        [SerializeField] private List<VisionTarget> targetedGeometries = new List<VisionTarget>(); //TODO: Make it reactive and dispose subscribers on array resize in case they are not cleaned up by the gc


        [SerializeField, Tooltip("Will enable the system to use entities")]private BoolReactiveProperty entityBasedProcessing = new BoolReactiveProperty();
        [SerializeField, Tooltip("Will enable the system to use GameObjects")]private BoolReactiveProperty gameObjectProcessing = new BoolReactiveProperty();
        private IDisposable gameObjectProcessingObservable = null;
        private IDisposable entityToggleObservable = null;
        public GeometryVisionHead Head { get; set; }
        private List<IGeoEye> eyes;
        public IGeoEye Eye { get; set; }
        public IGeoEye EntityEye { get; set; }
        void Reset()
        {
            Initialize();
        }

        // Start is called before the first frame update
        void Start()
        {
            Initialize();
        }

        // On validate is called when there is a change in the UI
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


            InitCameraForEye();
            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
            GeometryProcessors = GeometryVisionUtilities.getControllerFromGeometryManager(Head, this);
            Debugger = new EyeDebugger();
            seenTransforms = new HashSet<Transform>();

            Debugger.Planes = RegenerateVisionArea(fieldOfView, planes);
         
            InitEntitySwitch();
            InitGameObjectSwitch();
            InitializeTargeting(TargetedGeometries);
        }
        
        private void InitEntityEye()
        {
            
            EntityEye =  new GeometryVisionEntityEye();
        }
        private void InitCameraForEye()
        {
            Camera1 = gameObject.GetComponent<Camera>();
            if (Camera1 == null)
            {
                gameObject.AddComponent<Camera>();
                Camera1 = gameObject.GetComponent<Camera>();
            }

            Camera1.enabled = false;
        }
        private void InitGameObjectSwitch()
        {
            if (gameObjectProcessingObservable == null)
            {
                gameObjectProcessingObservable = gameObjectProcessing.Subscribe(x => { InitGameObjectBasedSystem(); });
            }
        }

        private void InitGameObjectBasedSystem()
        {
            gameObject.AddComponent<GeometryVisionEye>();
            Eye= gameObject.GetComponent<GeometryVisionEye>();
            SwitchBrain(false);
            
        }

        private void InitEntitySwitch()
        {
            if (entityToggleObservable == null)
            {
                entityToggleObservable = EntityBasedProcessing.Subscribe(x => { InitEntities(EntityBasedProcessing.Value); });
            }
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
            RefreshTargeting(targets);
        }
        
        void InitEntities(bool switchToEntities)
        {                
            UnityEngine.Debug.Log("initializing entities");

            SwitchBrain(switchToEntities);
            
        }

        private void SwitchBrain(bool toEntities)
        {
            if (Head.gameObject.GetComponent<GeometryVisionBrain>() != null)
            {
                DestroyImmediate(Head.gameObject.GetComponent<GeometryVisionBrain>());
            }
            if (toEntities)
            {
                Head.Brain = new GeometryVisionEntityBrain();
            }
            else
            {
                Head.gameObject.AddComponent<GeometryVisionBrain>();
                Head.Brain = Head.GetComponent<GeometryVisionBrain>();
            }
            geometryProcessors = Head.Brain;
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

        private static void RefreshTargeting(List<VisionTarget> targets)
        {
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
            foreach (var geoEye in eyes)
            {
                geoEye.UpdateVisibility();
            }
            foreach (var processor in geometryProcessors)
            {            
                //TODO: Check if this will be performance issue in case many eyes/cameras are present
                processor.CheckSceneChanges(targetedGeometries);

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
                Debugger.Debug(Camera1, geometryProcessors, true);
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

        public List<IGeoBrain> GeometryProcessors
        {
            get { return geometryProcessors; }
            set { geometryProcessors = value; }
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
        
        public BoolReactiveProperty GameObjectBasedProcessing
        {
            get { return gameObjectProcessing; }
            set { gameObjectProcessing = value; }
        }
    }
}