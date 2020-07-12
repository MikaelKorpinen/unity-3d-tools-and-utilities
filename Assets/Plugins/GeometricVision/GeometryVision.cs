using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using GeometricVision.Jobs;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
using Plugins.GeometricVision.UI;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using Plugins.GeometricVision.Utilities;
using UniRx;
using Unity.EditorCoroutines.Editor;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

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

        [SerializeField, Tooltip("Use the mesh from collider")]
        private bool targetColliderMeshes;

        [SerializeField] private List<VisionTarget> targetedGeometries = new List<VisionTarget>();

        [SerializeField, Tooltip("Include entities")]
        private BoolReactiveProperty entityBasedProcessing = new BoolReactiveProperty();

        [SerializeField, Tooltip("Will enable the system to use GameObjects")]
        private BoolReactiveProperty gameObjectProcessing = new BoolReactiveProperty();

        private IDisposable gameObjectProcessingObservable = null;
        private IDisposable entityToggleObservable = null;
        public GeometryVisionHead Head { get; set; }
        private HashSet<IGeoEye> eyes = new HashSet<IGeoEye>();
        private new Camera camera;
        private Plane[] planes = new Plane[6];
        private Vector3 forwardWorldCoordinate = Vector3.zero;
        private Transform cachedTransform;
        private List<GeometryDataModels.Target> closestTargets = new List<GeometryDataModels.Target>();
        private World entityWorld;
        private EndSimulationEntityCommandBufferSystem commandBufferSystem;
        public List<GeometryDataModels.Target> ClosestTargets
        {
            get { return closestTargets; }
        }

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
            cachedTransform = transform;
            InitUnityCamera();
            InitializeTargeting(TargetedGeometries);
        }
        private void OnDrawGizmos()
        {
            if (debugMode)
            {
                var transform1 = transform;
                UnityEngine.Debug.DrawLine(transform1.position, ForwardWorldCoordinate, Color.blue, 1);
                foreach (var closestTarget in ClosestTargets)
                {
                    Gizmos.color = Color.blue;
                    var position = transform1.position;
                    Gizmos.DrawLine(position, closestTarget.position);
                    Gizmos.DrawSphere(closestTarget.position, 0.3f);
                    var reset = closestTarget.projectedTargetPosition - position;
                    Handles.Label((reset / 2) + position, "distance: \n" + closestTarget.distanceToCastOrigin);

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(closestTarget.position, closestTarget.projectedTargetPosition);
                    Gizmos.DrawSphere(closestTarget.projectedTargetPosition, 0.3f);
                    reset = closestTarget.projectedTargetPosition - closestTarget.position;
                    Handles.Label((reset / 2) + closestTarget.position, "distance: \n" + closestTarget.distanceToRay);

                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(position, closestTarget.projectedTargetPosition);
                    Gizmos.DrawSphere(position, 0.3f);
                    reset = closestTarget.projectedTargetPosition - position;
                    Handles.Label((reset / 2) + position, "distance: \n" + closestTarget.distanceToCastOrigin);
                }
            }
        }

        private void Initialize()
        {
            cachedTransform = transform;
            seenTransforms = new HashSet<Transform>();
            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
            targetedGeometries.Add(new VisionTarget(GeometryType.Objects, 0, new GeometryObjectTargeting()));
            InitUnityCamera();
            GeometryVisionUtilities.SetupGeometryVision(Head, this, targetedGeometries);
            InitEntitySwitch();
            InitGameObjectSwitch();
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
        
        /// <summary>
        /// Handles all the required operation for GeometricVision to work with game objects.
        /// Such as GeometricVision eye/camera, processor for the data and targeting system
        /// The functionality is subscribed to a button on the inspector GUI
        /// </summary>
        private void InitGameObjectSwitch()
        {
            if (gameObjectProcessingObservable == null)
            {
                gameObjectProcessingObservable = gameObjectProcessing.Subscribe(gOProcessing =>
                {
                    InitGameObjectBasedSystem(gOProcessing);
                    
                    void InitGameObjectBasedSystem(bool objectProcessing)
                    {
                        var geoEye = GetComponent<GeometryVisionEye>();
                        if (objectProcessing)
                        {
                            if (geoEye == null)
                            {
                                GeometryVisionUtilities.SetupGeometryVisionEye(Head, this, fieldOfView);
                            }

                            InitGeometryProcessorForGameObjects(false);
                            void InitGeometryProcessorForGameObjects(bool gameObjectsEnabled)
                            {
                                if (gameObjectsEnabled)
                                {
                                    if (Head.gameObject.GetComponent<GeometryVisionProcessor>() != null)
                                    {
                                        Head.RemoveProcessor<GeometryVisionProcessor>();
                                        //also remove the mono behaviour from gameObject
                                        DestroyImmediate(Head.gameObject.GetComponent<GeometryVisionProcessor>());
                                    }

                                    Head.gameObject.AddComponent<GeometryVisionProcessor>();
                                    Head.AddProcessor(Head.gameObject.GetComponent<GeometryVisionProcessor>());
                                }
                                if(gameObjectsEnabled == false)
                                {
                                    if (Head.gameObject.GetComponent<GeometryVisionProcessor>() != null)
                                    {
                                        Head.RemoveProcessor<GeometryVisionProcessor>();
                                        //also remove the mono behaviour from gameObject
                                        DestroyImmediate(Head.gameObject.GetComponent<GeometryVisionProcessor>());
                                    }
                                }
                            }
                        }
                        else if (objectProcessing == false && geoEye)
                        {
                            DestroyEye(geoEye);
                            
                            void DestroyEye(GeometryVisionEye eye)
                            {
                                if (Application.isPlaying && eye != null)
                                {
                                    Destroy(eye);
                                }
                                else if (Application.isPlaying == false && eye != null)
                                {
                                    DestroyImmediate(eye);
                                }
                            }
                        }
                    }
                });
            }
            InitializeTargeting(TargetedGeometries);
            // Handles target initialization. Adds needed components and subscribes changing variables to logic that updates the targeting system.
            // So it can keep working under use.
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

                ValidateTargetingSystems(targets);
            }
        }
        
        /// <summary>
        /// Handles all the required operation for GeometricVision to work with entities.
        /// Such as GeometricVision eye/camera, processor for the data and targeting system
        /// The functionality is subscribed to a button on the inspector GUI
        /// </summary>
        private void InitEntitySwitch()
        {
            if (entityToggleObservable == null)
            {
                entityToggleObservable = EntityBasedProcessing.Subscribe(x =>
                {
                    InitEntities(EntityBasedProcessing.Value);
                    void InitEntities(bool switchToEntities)
                    {
                        entityWorld = World.DefaultGameObjectInjectionWorld;

                        InitGeometryProcessorForEntities(switchToEntities, entityWorld);
                        InitGeometryCameraForEntities(switchToEntities, entityWorld);
                        InitializeTargetingForEntities(TargetedGeometries);
                    }
                });
            }
        }

        private void InitializeTargetingForEntities(List<VisionTarget> visionTargets)
        {
            throw new NotImplementedException();
        }


        private void InitGeometryProcessorForEntities(bool toEntities, World world)
        {
            if (toEntities)
            {
                Head.RemoveProcessor<GeometryVisionEntityProcessor>();
                world.CreateSystem<GeometryVisionEntityProcessor>();

                IGeoProcessor eProcessor = world.GetExistingSystem<GeometryVisionEntityProcessor>();
                Head.AddProcessor(eProcessor);
                var addedProcessor = Head.GetProcessor<GeometryVisionEntityProcessor>();
                addedProcessor.GeoVision = this;
                addedProcessor.CheckSceneChanges(this);
                addedProcessor.Enabled = true;
                addedProcessor.Update();
            }
            if(toEntities == false)
            {
                Head.RemoveProcessor<GeometryVisionEntityProcessor>();
                
                if (Head.gameObject.GetComponent<GeometryVisionProcessor>() != null)
                {
                    Head.RemoveProcessor<GeometryVisionProcessor>();
                    //also remove mono behaviour
                    DestroyImmediate(Head.gameObject.GetComponent<GeometryVisionProcessor>());
                }

                Head.gameObject.AddComponent<GeometryVisionProcessor>();
                Head.AddProcessor(Head.gameObject.GetComponent<GeometryVisionProcessor>());
            }
        }

        private void InitGeometryCameraForEntities(bool toEntities, World world)
        {
            if (toEntities)
            {
                RemoveEye<GeometryVisionEntityEye>();

                world.CreateSystem<GeometryVisionEntityEye>();
                GeometryVisionEntityEye eEey =  (GeometryVisionEntityEye) world.GetExistingSystem(typeof(GeometryVisionEntityEye));
                AddEye(eEey);
                var eye = GetEye<GeometryVisionEntityEye>();
                eye.GeoVision = this;
                eye.TargetedGeometries = targetedGeometries;
                
                eye.Enabled = true;
                eye.Update();
            }
            else
            {
                RemoveEye<GeometryVisionEntityEye>();
            }
        }


        private GeometryTargetingSystemsContainer HandleAddingGeometryTargetingComponent()
        {
            var geoTargeting = gameObject.GetComponent<GeometryTargetingSystemsContainer>();
            if (gameObject.GetComponent<GeometryTargetingSystemsContainer>() == null)
            {
                gameObject.AddComponent<GeometryTargetingSystemsContainer>();
                geoTargeting = gameObject.GetComponent<GeometryTargetingSystemsContainer>();
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
        /// <param name="geoTargetingSystemsContainer"></param>
        private void OnTargetingEnabled(VisionTarget geometryType, GeometryTargetingSystemsContainer geoTargetingSystemsContainer)
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
                        geoTargetingSystemsContainer.AddTarget(geometryType);
                    }
                    else
                    {
                        geometryType.TargetHidden = false;
                        geoTargetingSystemsContainer.RemoveTarget(geometryType);
                    }
                });
                geometryType.Subscribed = true;
            }
        }

        private static void ValidateTargetingSystems(List<VisionTarget> targets)
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

        public HashSet<IGeoEye> Eyes
        {
            get { return eyes; }
            set { eyes = value; }
        }

        public void RemoveEye<T>()
        {
            InterfaceUtilities.RemoveInterfacesOfTypeFromList(typeof(T), ref eyes);
        }

        public T GetEye<T>()
        {
            return (T) InterfaceUtilities.GetInterfaceOfTypeFromList(typeof(T), eyes);
        }

        /// <summary>
        /// Adds eye component to the list and makes sure that the implementation to be added is unique.
        /// Does not add duplicate implementation.
        /// </summary>
        /// <param name="eye"></param>
        /// <typeparam name="T"></typeparam>
        public void AddEye<T>(T eye)
        {
            if (eyes == null)
            {
                eyes = new HashSet<IGeoEye>();
            }

            if (InterfaceUtilities.ListContainsInterfaceOfType(eye.GetType(), eyes) == false)
            {
                var defaultEyeFromTypeT = (IGeoEye) default(T);
                //Check that the implementation is not the default one
                if (Object.Equals(eye, defaultEyeFromTypeT) == false)
                {
                    eyes.Add((IGeoEye) eye);
                }
            }
        }

        public string Id { get; set; }

        public Vector3 ForwardWorldCoordinate
        {
            get
            {
                forwardWorldCoordinate = cachedTransform.position + cachedTransform.forward;
                return forwardWorldCoordinate;
            }
        }


        public List<GeometryDataModels.Target> GetClosestTargets(List<GeometryDataModels.GeoInfo> GeoInfos)
        {
            foreach (var targetedGeometry in TargetedGeometries)
            {
                closestTargets = targetedGeometry.TargetingSystem.GetTargets(gameObject.transform.position,
                    ForwardWorldCoordinate, GeoInfos);
            }

            return closestTargets;
        }
    }
}