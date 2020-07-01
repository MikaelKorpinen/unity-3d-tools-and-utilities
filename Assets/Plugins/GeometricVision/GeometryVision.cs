using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using GeometricVision.Jobs;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.UI;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using Plugins.GeometricVision.Utilities;
using UniRx;
using Unity.EditorCoroutines.Editor;
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
        [SerializeField] public HashSet<Transform> seenTransforms;
        [SerializeField, Tooltip("Use the mesh from collider")]private bool targetColliderMeshes;
        [SerializeField] private List<VisionTarget> targetedGeometries = new List<VisionTarget>();
        [SerializeField, Tooltip("Include entities")] private BoolReactiveProperty entityBasedProcessing = new BoolReactiveProperty();
        [SerializeField, Tooltip("Will enable the system to use GameObjects")]
        private BoolReactiveProperty gameObjectProcessing = new BoolReactiveProperty();

        private IDisposable gameObjectProcessingObservable = null;
        private IDisposable entityToggleObservable = null;
        public GeometryVisionHead Head { get; set; }
        private List<IGeoEye> eyes = new List<IGeoEye>();
        private new Camera camera;
        public Plane[] planes = new Plane[6];

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
            InitUnityCamera();
            InitializeTargeting(TargetedGeometries);
        }

        private void Initialize()
        {
            CreateDefaultTargeting();
            seenTransforms = new HashSet<Transform>();
            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
            
            InitUnityCamera();
            GeometryVisionUtilities.CreateGeometryVision(Head, this);
            InitEntitySwitch();
            InitGameObjectSwitch();
            InitializeTargeting(TargetedGeometries);
        }

        private void CreateDefaultTargeting()
        {
            if (isObjectsTargeted(targetedGeometries) == false)
            {
                targetedGeometries = new List<VisionTarget>();
                IGeoTargeting targeting = new GeometryObjectTargeting();
                targetedGeometries.Add(new VisionTarget(GeometryType.Objects, 0, targeting));
            }
        }

        public void InitUnityCamera()
        {
            Camera1 = gameObject.GetComponent<Camera>();
            if (Camera1 == null)
            {
                gameObject.AddComponent<Camera>();
                Camera1 = gameObject.GetComponent<Camera>();
            }

            planes = RegenerateVisionArea(fieldOfView, planes);
            Camera1.enabled = false;
        }

        private void InitGameObjectSwitch()
        {
            if (gameObjectProcessingObservable == null)
            {
                gameObjectProcessingObservable = gameObjectProcessing.Subscribe(gameObjectProcessing =>
                {
                    InitGameObjectBasedSystem(gameObjectProcessing);
                });
            }
        }

        private void InitGameObjectBasedSystem(bool objectProcessing)
        {
            var geoEye = GetComponent<GeometryVisionEye>();
            if (objectProcessing)
            {
                DestroyEye(geoEye);
                gameObject.AddComponent<GeometryVisionEye>();
                eyes.Add(gameObject.GetComponent<GeometryVisionEye>());
                ReplaceProcessor(false);
            }
            else if (objectProcessing == false && geoEye)
            {
                DestroyEye(geoEye);
            }
        }

        private static void DestroyEye(GeometryVisionEye geoEye)
        {
            if (Application.isPlaying && geoEye != null)
            {
                Destroy(geoEye);
            }
            else if(Application.isPlaying == false && geoEye != null) 
            {
                DestroyImmediate(geoEye); 
            }
        }

        private void InitEntitySwitch()
        {
            if (entityToggleObservable == null)
            {
                entityToggleObservable = EntityBasedProcessing.Subscribe(x =>
                {
                    InitEntities(EntityBasedProcessing.Value);
                });
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
                    var geoTargeting = HandleAddingGeometryTargetingComponent();
                    AssignActionsForTargeting(geometryType, targets.IndexOf(geometryType));
                    OnTargetingEnabled(geometryType, geoTargeting);
                }
            }

            RefreshTargetingSystems(targets);
        }

        void InitEntities(bool switchToEntities)
        {
            
            ReplaceProcessor(switchToEntities);
            InitEntityEye(Head.GetProcessor<GeometryVisionEntityProcessor>());
        }
        
        private void InitEntityEye(GeometryVisionEntityProcessor eProcessor)
        {
            var eEye = new GeometryVisionEntityEye();
            eEye.ControllerProcessor = eProcessor;
            eEye.Id = new Hash128().ToString();
            Head.Eyes.Add(eEye);
            Eyes.Add(eEye);
        }
        private void ReplaceProcessor(bool toEntities)
        {

            if (toEntities)
            {
                Head.RemoveProcessor<GeometryVisionEntityProcessor>();
                Head.AddProcessor<GeometryVisionEntityProcessor>();
            }
            else
            {
                //TODO: Make it single line to remove the monobehaviour
                if (Head.gameObject.GetComponent<GeometryVisionProcessor>() != null)
                {
                    Head.RemoveProcessor<GeometryVisionProcessor>();
                    //also remove mono behaviour
                    DestroyImmediate(Head.gameObject.GetComponent<GeometryVisionProcessor>());
                }

                Head.gameObject.AddComponent<GeometryVisionProcessor>();
                Head.AddProcessor<GeometryVisionProcessor>();
            }
        }

        private GeometryTargeting HandleAddingGeometryTargetingComponent()
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

        private static void RefreshTargetingSystems(List<VisionTarget> targets)
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
            foreach (var geoEye in Eyes)
            {
                geoEye.UpdateVisibility();
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

        public bool DebugMode
        {
            get { return debugMode; }
            set { debugMode = value; }
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

        public List<IGeoEye> Eyes
        {
            get { return eyes; }
            set { eyes = value; }
        }

        public string Id { get; set; }
    }
}