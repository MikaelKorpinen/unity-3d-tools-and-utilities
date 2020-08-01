using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using Hash128 = Unity.Entities.Hash128;
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
    [DisallowMultipleComponent]
    public class GeometryVision : MonoBehaviour
    {
        [SerializeField] private bool debugMode;

        //[SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private float farDistanceOfView = 25f;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
        [SerializeField] public HashSet<Transform> seenTransforms;

        //[SerializeField, Tooltip("Use the mesh from collider")]
        // private bool targetColliderMeshes;

        [SerializeField] private List<VisionTarget> targetingInstructions = new List<VisionTarget>();

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
        private List<GeometryDataModels.Target> closestEntityTargets = new List<GeometryDataModels.Target>();
        private World entityWorld;
        private EndSimulationEntityCommandBufferSystem commandBufferSystem;

        public List<GeometryDataModels.Target> ClosestTargets
        {
            get { return closestTargets; }
        }

        void Reset()
        {
            InitializeSystems();
            if (targetingInstructions.Count == 0)
            {
                targetingInstructions.Add(
                    new VisionTarget(GeometryType.Objects, 0, new GeometryObjectTargeting(), true));
            }
        }

        //Awake is called when script is instantiated.
        //Call initialize on Awake to init systems in case Component is created on the factory method.
        void Awake()
        {
            InitializeSystems();
        }

        // Start is called before the first frame update
        void Start()
        {
            InitializeSystems();
        }

        // On validate is called when there is a change in the UI
        void OnValidate()
        {
            cachedTransform = transform;
            InitializeTargeting(TargetingInstructions);
            ValidateTargetingSystems(targetingInstructions);
        }

        // Handles target initialization. Adds needed components and subscribes changing variables to logic that updates the targeting system.
        private void InitializeTargeting(List<VisionTarget> targetingInstructions)
        {
            var geoTargetingSystemsContainer = HandleAddingGeometryTargetingComponent();
            foreach (var targetingInstruction in targetingInstructions)
            {
                OnTargetingEnabled(targetingInstruction, geoTargetingSystemsContainer);
            }
        }

        /// <summary>
        /// Should be run in case making changes to GUI values from code and want the changes to happen before next frame(instantly).
        /// </summary>
        public void InitializeSystems()
        {
            cachedTransform = transform;
            seenTransforms = new HashSet<Transform>();
            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();

            InitUnityCamera();
            InitializeTargeting(TargetingInstructions);

            GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView = fieldOfView,
                processGameObjects = gameObjectProcessing.Value,
                processEntities = entityBasedProcessing.Value,
                defaultTargeting = true,
            };

            GeometryVisionUtilities.SetupGeometryVision(Head, this, targetingInstructions, factorySettings);
            InitEntitySwitch();
            InitGameObjectSwitch();
            UpdateTargetingSystemsContainer();
        }
        
        /// <summary>
        /// Clears up current targeting and creates a new Hashset, then proceeds to add all the available targeting systems
        /// to the targeting systems container
        /// </summary>
        internal void UpdateTargetingSystemsContainer()
        {
            var targetingSystemsContainer = GetComponent<GeometryTargetingSystemsContainer>();
            targetingSystemsContainer.TargetingPrograms = new HashSet<IGeoTargeting>();
            foreach (var targetingInstruction in targetingInstructions)
            {
                if (targetingInstruction.TargetingSystemGameObjects != null)
                {
                    targetingSystemsContainer.AddTargetingProgram(targetingInstruction.TargetingSystemGameObjects);
                }

                if (targetingInstruction.TargetingSystemEntities != null)
                {
                    targetingSystemsContainer.AddTargetingProgram(targetingInstruction.TargetingSystemEntities);
                }
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

        public void InitUnityCamera()
        {
            Camera1 = gameObject.GetComponent<Camera>();
            if (Camera1 == null)
            {
                gameObject.AddComponent<Camera>();
                Camera1 = gameObject.GetComponent<Camera>();
            }

            planes = RegenerateVisionArea(FieldOfView, planes);
            Camera1.enabled = false;
        }

        /// <summary>
        /// Handles all the required operation for GeometricVision to work with game objects.
        /// Such as GeometricVision eye/camera, processor for the data and targeting system
        /// The functionality is subscribed to a toggle on the inspector GUI
        /// </summary>
        internal void InitGameObjectSwitch()
        {
            SubscribeToButton();

            void SubscribeToButton()
            {
                if (gameObjectProcessingObservable == null)
                {
                    gameObjectProcessingObservable = gameObjectProcessing.Subscribe(InitGameObjectBasedSystem);
                }
            }

            void InitGameObjectBasedSystem(bool gameObjectBasedProcessing)
            {
                var geoEye = GetEye<GeometryVisionEye>();
                if (gameObjectBasedProcessing == true)
                {
                    if (geoEye == null)
                    {
                        AddEye<GeometryVisionEye>();
                    }

                    InitGeometryProcessorForGameObjects();
                    IfNoDefaultTargetingAddOne(new GeometryObjectTargeting());
                }
                else if (gameObjectBasedProcessing == false)
                {
                    Head.RemoveProcessor<GeometryVisionProcessor>();
                    RemoveEye<GeometryVisionEye>();
                }
            }

            void InitGeometryProcessorForGameObjects()
            {
                if (Head.gameObject.GetComponent<GeometryVisionProcessor>() == null)
                {
                    Head.gameObject.AddComponent<GeometryVisionProcessor>();
                }

                Head.AddProcessor(Head.gameObject.GetComponent<GeometryVisionProcessor>());
            }
        }

        private void IfNoDefaultTargetingAddOne(IGeoTargeting targetingSystem)
        {
            var targetingInstruction = GetTargetingInstructionOfType(GeometryType.Objects);
            if (targetingInstruction == null)
            {
                targetingInstruction = new VisionTarget(GeometryType.Objects, 0, targetingSystem, true);
                AssignTargetingSystem(targetingSystem, targetingInstruction);
                targetingInstructions.Add(targetingInstruction);
            }
            else
            {
                AssignTargetingSystem(targetingSystem, targetingInstruction);
            }

            void AssignTargetingSystem(IGeoTargeting geoTargeting, VisionTarget visionTarget)
            {
                if (geoTargeting.IsForEntities())
                {
                    visionTarget.TargetingSystemEntities = geoTargeting;
                }
                else
                {
                    visionTarget.TargetingSystemGameObjects = geoTargeting;
                }
            }
        }

        /// <summary>
        /// Handles all the required operation for GeometricVision to work with entities.
        /// Such as GeometricVision eye/camera, processor for the data and targeting system
        /// The functionality is subscribed to a toggle on the inspector GUI.
        /// Because of some differences on the entity system the entities functionality can only be enabled easily
        /// when the application is running.
        /// </summary>
        internal void InitEntitySwitch()
        {
            if (entityToggleObservable != null)
            {
                return;
            }

            //Add initialization behaviour on the inspector toggle button
            entityToggleObservable = EntityBasedProcessing.Subscribe(entitiesEnabled =>
            {
                InitEntities(entitiesEnabled);

                void InitEntities(bool entitiesBasedProcessing)
                {
                    entityWorld = World.DefaultGameObjectInjectionWorld;
                    if (Application.isPlaying && entitiesEnabled)
                    {
                        InitGeometryProcessorForEntities(entitiesBasedProcessing, entityWorld);
                        InitGeometryCameraForEntities(entitiesBasedProcessing, entityWorld);

                        IGeoTargeting targetingSystem =
                            entityWorld.GetOrCreateSystem<GeometryEntitiesObjectTargeting>();
                        IfNoDefaultTargetingAddOne(targetingSystem);
                    }

                    if (Application.isPlaying && entitiesEnabled == false)
                    {
                        RemoveEntityProcessors();
                        DisableEntityCameras();
                    }
                }
            });
        }


        private void DisableEntityCameras()
        {
            while (GetEye<GeometryVisionEntityEye>() != null)
            {
                RemoveEye<GeometryVisionEntityEye>();
            }
        }

        private void RemoveEntityProcessors()
        {
            while (Head.GetProcessor<GeometryVisionEntityProcessor>() != null)
            {
                Head.RemoveProcessor<GeometryVisionEntityProcessor>();
            }
        }

        private void InitGeometryProcessorForEntities(bool toEntities, World world)
        {
            RemoveEntityProcessors();
            world.CreateSystem<GeometryVisionEntityProcessor>();
            IGeoProcessor eProcessor = world.GetExistingSystem<GeometryVisionEntityProcessor>();
            Head.AddProcessor(eProcessor);
            var addedProcessor = Head.GetProcessor<GeometryVisionEntityProcessor>();
            addedProcessor.GeoVision = this;
            addedProcessor.CheckSceneChanges(this);
            addedProcessor.Update();
        }

        private void InitGeometryCameraForEntities(bool toEntities, World world)
        {
            DisableEntityCameras();
            world.CreateSystem<GeometryVisionEntityEye>();
            GeometryVisionEntityEye eEey =
                (GeometryVisionEntityEye) world.GetExistingSystem(typeof(GeometryVisionEntityEye));
            AddEye(eEey);
            var eye = GetEye<GeometryVisionEntityEye>();
            eye.GeoVision = this;
            eye.TargetedGeometries = targetingInstructions;
            eye.Update();
        }

        /// <summary>
        /// Checks if objects are targeted. At least one GeometryType.Objects_ needs to be in the list in order for the plugin to see something that it can use
        /// </summary>
        /// <param name="targetingInstructions"></param>
        /// <returns></returns>
        bool isGeometryTypeTargetingInstructionAdded(List<VisionTarget> targetingInstructions, GeometryType geoType)
        {
            bool targetingTypeFound = false;
            foreach (var targetingInstruction in targetingInstructions)
            {
                if (targetingInstruction.GeometryType == geoType)
                {
                    targetingTypeFound = true;
                }
            }

            return targetingTypeFound;
        }

        /// <summary>
        /// Add targeting implementation, if it is enabled on the inspector.
        /// Subscribes the targeting toggle button to functionality than handles creation of default targeting implementation for the
        /// targeted geometry type
        /// </summary>
        /// <param name="targetingInstruction"></param>
        /// <param name="geoTargetingSystemsContainer"></param>
        private void OnTargetingEnabled(VisionTarget targetingInstruction,
            GeometryTargetingSystemsContainer geoTargetingSystemsContainer)
        {
            if (!targetingInstruction.Subscribed)
            {
                targetingInstruction.IsTargetingEnabled.Subscribe(targeting =>
                {
                    //Cannot get Reactive value from serialized property, so this boolean variable handles it job on the inspector gui under the hood.
                    //The other way is to find out how to get reactive value out of serialized property. Shows option for adding actions template from the inspector GUI
                    targetingInstruction.IsTargetActionsTemplateSlotVisible = targeting;
                    if (targeting == true)
                    {
                        AssignActionsForTargeting(targetingInstructions.IndexOf(targetingInstruction));
                        
                        AddDefaultTargeting(geoTargetingSystemsContainer);
                    }
                    else
                    {
                        RemoveDefaultTargeting(geoTargetingSystemsContainer);
                    }
                });
                targetingInstruction.Subscribed = true;
            }

            void AssignActionsForTargeting(int indexOf)
            {
                if (targetingInstruction.targetingActions == null)
                {
                    var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
                    newActions.name += "_" + indexOf;
                    targetingInstruction.targetingActions = newActions;
                }
            }
            
            void AddDefaultTargeting(
                GeometryTargetingSystemsContainer geometryTargetingSystemsContainer)
            {
                if (gameObjectProcessing.Value == true)
                {
                    geometryTargetingSystemsContainer.AddTargetingProgram(
                        (IGeoTargeting) targetingInstruction.TargetingSystemGameObjects);
                }
                else if (entityBasedProcessing.Value == true)
                {
                    geometryTargetingSystemsContainer.AddTargetingProgram(
                        (IGeoTargeting) targetingInstruction.TargetingSystemEntities);
                }
            }

            void RemoveDefaultTargeting(
                GeometryTargetingSystemsContainer geometryTargetingSystemsContainer)
            {
                //Do the same thing here
                if (gameObjectProcessing.Value == true)
                {
                    geometryTargetingSystemsContainer.RemoveTargetingProgram(targetingInstruction
                        .TargetingSystemGameObjects);
                }
                else if (entityBasedProcessing.Value == true)
                {
                    geometryTargetingSystemsContainer.RemoveTargetingProgram(targetingInstruction
                        .TargetingSystemEntities);
                }
            }
        }


        /// <summary>
        /// In case the user plays around with the settings on the inspector and changes thins this needs to be run.
        /// It checks that the targeting system implementations are correct.
        /// </summary>
        /// <param name="targetingInstructions"></param>
        private void ValidateTargetingSystems(List<VisionTarget> targetingInstructions)
        {
            foreach (var targetingInstruction in targetingInstructions)
            {
                if (gameObjectProcessing.Value == true)
                {
                    targetingInstruction.TargetingSystemGameObjects = RunValidation(
                        targetingInstruction.TargetingSystemGameObjects,
                        targetingInstruction, new GeometryObjectTargeting(), new GeometryLineTargeting());
                }

                if (entityBasedProcessing.Value == true && Application.isPlaying && entityWorld != null)
                {
                    var newObjectTargetng = entityWorld.CreateSystem<GeometryEntitiesObjectTargeting>();
                    var newLineTargetng = entityWorld.CreateSystem<GeometryEntitiesLineTargeting>();

                    targetingInstruction.TargetingSystemEntities = RunValidation(
                        targetingInstruction.TargetingSystemEntities,
                        targetingInstruction, newObjectTargetng, newLineTargetng);
                }
            }

            IGeoTargeting RunValidation(IGeoTargeting targetingToValidate, VisionTarget visionTarget,
                IGeoTargeting newObjectTargeting, IGeoTargeting newLineTargeting)
            {
                if (targetingToValidate == null)
                {
                    targetingToValidate = AssignNewTargetIngSystem(visionTarget, newObjectTargeting, newLineTargeting);
                }
                //In case user changed something then things needs to change
                else if (targetingToValidate != null && visionTarget.GeometryType != targetingToValidate.TargetedType)
                {
                    targetingToValidate = AssignNewTargetIngSystem(visionTarget, newObjectTargeting, newLineTargeting);
                }

                return targetingToValidate;
            }

            IGeoTargeting AssignNewTargetIngSystem(VisionTarget visionTarget1, IGeoTargeting newObjectTargeting,
                IGeoTargeting newLineTargeting)
            {
                IGeoTargeting targetingToReturn = null;
                //No need to assign in case the system is already what it should be
                if (visionTarget1.GeometryType == GeometryType.Objects)
                {
                    targetingToReturn = newObjectTargeting;
                }

                //No need to assign in case the system is already what it should be
                if (visionTarget1.GeometryType == GeometryType.Lines)
                {
                    targetingToReturn = newLineTargeting;
                }

                return targetingToReturn;
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
            Camera1.farClipPlane = farDistanceOfView;
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
            Camera1.farClipPlane = farDistanceOfView;
            planes = GeometryUtility.CalculateFrustumPlanes(Camera1);
            Camera1.enabled = false;
        }

        public List<VisionTarget> TargetingInstructions
        {
            get { return targetingInstructions; }
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
        }

        public void RemoveEye<T>()
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList(typeof(T), ref eyes);
            if (typeof(T) == typeof(GeometryVisionEye))
            {
                var eye = GetComponent<GeometryVisionEye>();
                //also remove the mono behaviour from gameObject, if it is one. TODO: get the if implements monobehaviour
                //Currently there is only 2 types. Other one is MonoBehaviour and the other one not
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

        public T GetEye<T>()
        {
            return (T) InterfaceUtilities.GetInterfaceImplementationOfTypeFromList(typeof(T), eyes);
        }

        /// <summary>
        /// Adds eye/camera component to the list and makes sure that the implementation to be added is unique.
        /// Does not add duplicate implementation.
        /// </summary>
        /// <param name="eye"></param>
        /// <typeparam name="T"></typeparam>
        private void AddEye<T>(T eye)
        {
            if (eyes == null)
            {
                eyes = new HashSet<IGeoEye>();
            }

            if (InterfaceUtilities.ListContainsInterfaceImplementationOfType(eye.GetType(), eyes) == false)
            {
                var defaultEyeFromTypeT = (IGeoEye) default(T);
                //Check that the implementation is not the default one
                if (Object.Equals(eye, defaultEyeFromTypeT) == false)
                {
                    eyes.Add((IGeoEye) eye);
                }
            }
        }

        /// <summary>
        /// Adds eye/camera component to the list and makes sure that the implementation to be added is unique.
        /// Does not add duplicate implementation.
        /// </summary>
        /// <param name="eye"></param>
        /// <typeparam name="T"></typeparam>
        private void AddEye<T>()
        {
            if (eyes == null)
            {
                eyes = new HashSet<IGeoEye>();
            }

            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                var eye = gameObject.GetComponent(typeof(T));
                if (eye == null)
                {
                    gameObject.AddComponent(typeof(T));
                    var addedEye = (IGeoEye) gameObject.GetComponent(typeof(T));
                    addedEye.Head = Head;
                    addedEye.Id = new Hash128().ToString();
                    addedEye.GeoVision = this;
                    eye = (Component) addedEye;
                }

                if (InterfaceUtilities.ListContainsInterfaceImplementationOfType(typeof(T), eyes) == false)
                {
                    var defaultEyeFromTypeT = (IGeoEye) default(T);
                    //Check that the implementation is not the default one
                    if (Object.Equals(eye, defaultEyeFromTypeT) == false)
                    {
                        eyes.Add((IGeoEye) eye);
                    }
                }
            }
            else if (GetEye<T>() == null)
            {
                var eye = entityWorld.GetOrCreateSystem<GeometryVisionEntityEye>();
                var defaultEyeFromTypeT = (IGeoEye) default(T);
                //Check that the implementation is not the default one
                if (Object.Equals(eye, defaultEyeFromTypeT) == false)
                {
                    eyes.Add((IGeoEye) eye);
                }
            }
        }


        /// <summary>
        /// Gets the first targeting instruction matching the give type as GeometryType.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public VisionTarget GetTargetingInstructionOfType(GeometryType type)
        {
            VisionTarget instructionToReturn = null;
            foreach (var instruction in TargetingInstructions)
            {
                if (instruction.GeometryType == GeometryType.Objects)
                {
                    instructionToReturn = instruction;
                    break;
                }
            }

            return instructionToReturn;
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

        public float FieldOfView
        {
            get { return fieldOfView; }
            set { fieldOfView = value; }
        }

        public List<GeometryDataModels.Target> GetClosestTargets(List<GeometryDataModels.GeoInfo> GeoInfos)
        {
            var newClosestTargets = new List<GeometryDataModels.Target>();
            foreach (var targetingInstruction in TargetingInstructions)
            {
                if (gameObjectProcessing.Value)
                {
                    var closestTargets = targetingInstruction.TargetingSystemGameObjects.GetTargets(transform.position,
                        ForwardWorldCoordinate, GeoInfos);
                    newClosestTargets = closestTargets;
                }

                if (entityBasedProcessing.Value)
                {
                    var closestEntityTargets =
                        targetingInstruction.TargetingSystemEntities.GetTargets(transform.position,
                            ForwardWorldCoordinate, GeoInfos);
                    //Only update entities if the burst compiled job has finished its job OnUpdate
                    //If it has not finished it returns empty list.
                    if (closestEntityTargets.Count > 0)
                    {
                        this.closestEntityTargets = closestEntityTargets;
                    }
                }
            }

            newClosestTargets.AddRange(this.closestEntityTargets);

            if (newClosestTargets.Count > 0)
            {
                closestTargets = newClosestTargets.OrderBy(target => target.distanceToRay).ToList();
            }


            return closestTargets;
        }


#if UNITY_EDITOR


        /// <summary>
        /// Used for debugging geometry vision and is responsible for drawing debugging info from the data providid by
        /// GeometryVision plugin
        /// </summary>
        private void OnDrawGizmos()
        {
            if (Camera1)
            {
                Camera1.fieldOfView = this.fieldOfView;
            }

            if (!debugMode)
            {
                return;
            }

            UnityEngine.Debug.DrawLine(transform.position, ForwardWorldCoordinate, Color.blue, 1);


            DrawTargets(ClosestTargets);

            void DrawTargets(List<GeometryDataModels.Target> closestTargetsIn)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

                for (var i = 0; i < closestTargetsIn.Count; i++)
                {
                    var closestTarget = closestTargetsIn[i];
                    Vector3 resetToVector = Vector3.zero;
                    var position = DrawVisualIndicator(closestTarget.position, transform.position,
                        closestTarget.distanceToCastOrigin, Color.blue);
                    DrawVisualIndicator(closestTarget.projectedTargetPosition, closestTarget.position,
                        closestTarget.distanceToRay,
                        Color.green);
                    DrawVisualIndicator(position, closestTarget.projectedTargetPosition,
                        closestTarget.distanceToCastOrigin,
                        Color.red);

                    DrawInfo(closestTarget.position, Vector3.down, i);

                    Vector3 DrawVisualIndicator(Vector3 spherePosition, Vector3 lineStartPosition, float distance,
                        Color color)
                    {
                        Gizmos.color = color;
                        Gizmos.DrawLine(lineStartPosition, spherePosition);
                        Gizmos.DrawSphere(spherePosition, 0.3f);
                        resetToVector = closestTarget.projectedTargetPosition - lineStartPosition;
                        Handles.Label((resetToVector / 2) + lineStartPosition, "distance: \n" + distance);
                        return lineStartPosition;
                    }

                    void DrawInfo(Vector3 textLocation, Vector3 offset, int order)
                    {
                        Gizmos.DrawSphere(textLocation, 0.3f);
                        resetToVector = closestTarget.projectedTargetPosition - offset;
                        var textToShow = "";
                        if (closestTarget.isEntity)
                        {
                            textToShow = "Target type: Entity\n" + order + "th target\n";
                        }
                        else
                        {
                            textToShow = "Target type: GameObject\n" + order + "th target\n";
                        }

                        Handles.Label((textLocation) + offset, textToShow);
                    }
                }
            }
        }
#endif
    }
}