using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.EntityScripts;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
using Plugins.GeometricVision.UI;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using Plugins.GeometricVision.Utilities;
using UniRx;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
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


        //[SerializeField, Tooltip("Use the mesh from collider")]
        // private bool targetColliderMeshes;

        [SerializeField] private List<VisionTarget> targetingInstructions = new List<VisionTarget>();

        [SerializeField, Tooltip("Include entities")]
        private BoolReactiveProperty entityBasedProcessing = new BoolReactiveProperty();

        [SerializeField, Tooltip("Entity component to use in filtering queries. If none uses all entities.")]
        private UnityEngine.Object entityFilterComponent;

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
        private List<GeometryDataModels.Target> closestEntityTargets;
        private World entityWorld;
        private EndSimulationEntityCommandBufferSystem commandBufferSystem;
        private GeometryDataModels.Target closestTarget;
        private TransformEntitySystem transformEntitySystem;

        void Reset()
        {
            InitializeSystems();
            if (targetingInstructions.Count == 0)
            {

                targetingInstructions.Add(
                    new VisionTarget(GeometryType.Objects, "", new GeometryObjectTargeting(), true,
                        GetCurrentEntityFilterType()));
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
            closestEntityTargets = new List<GeometryDataModels.Target>();
            InitUnityCamera();
            InitializeTargeting(TargetingInstructions);

            GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView = fieldOfView,
                processGameObjects = gameObjectProcessing.Value,
                processEntities = entityBasedProcessing.Value,
                defaultTargeting = true,
                defaultTag = ""
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

        /// <summary>
        /// Inits unity camera which provides some needed features for geometric vision like Gizmos and matrices.
        /// The camera is disable dot prevent rendering and frustum culling.
        /// </summary>
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
        
        /// <summary>
        /// Provides Needed default object targeting for the system in case there is none. Otherwise replaces one from the current users
        /// targeting instructions. 
        /// </summary>
        /// <param name="targetingSystem"></param>
        private void IfNoDefaultTargetingAddOne(IGeoTargeting targetingSystem)
        {
            var targetingInstruction = GetTargetingInstructionOfType(GeometryType.Objects);
            if (targetingInstruction == null)
            {

                targetingInstruction = new VisionTarget(GeometryType.Objects, "", targetingSystem, true, GetCurrentEntityFilterType());
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

        private Type GetCurrentEntityFilterType()
        {
            if (EntityFilterComponent != null)
            {
                var mS = (MonoScript) EntityFilterComponent;
                Type type = mS.GetClass().UnderlyingSystemType;
                return type;
            }
            else
            {
                return null;
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
                    //Handle toggle button use cases
                    IfEntitiesEnabled(entitiesEnabled, entitiesBasedProcessing);
                    IfEntitiesDisabled(entitiesEnabled);
                }
            });
            
            //Handles toggle button enabled use case
            void IfEntitiesEnabled(bool entitiesEnabled, bool entitiesBasedProcessing)
            {
                if (Application.isPlaying && entitiesEnabled)
                {
                    InitGeometryProcessorForEntities(entitiesBasedProcessing, entityWorld);
                    InitGeometryCameraForEntities(entityWorld);

                    IGeoTargeting targetingSystem =
                        entityWorld.GetOrCreateSystem<GeometryEntitiesObjectTargeting>();
                    IfNoDefaultTargetingAddOne(targetingSystem);
                }
            }
            
            //Handles toggle button disabled use case
            void IfEntitiesDisabled(bool entitiesEnabled)
            {
                if (Application.isPlaying && entitiesEnabled == false)
                {
                    RemoveEntityProcessors();
                    DisableEntityCameras();
                    if (entityWorld.GetExistingSystem<TransformEntitySystem>() != null)
                    {
                        entityWorld.DestroySystem(transformEntitySystem);
                    }

                    transformEntitySystem = null;

                    foreach (var targetinginstruction in targetingInstructions)
                    {
                        if (targetinginstruction.TargetingSystemEntities != null)
                        {                       
                            entityWorld.DestroySystem((ComponentSystemBase) targetinginstruction.TargetingSystemEntities);
                        }
                    }
                }
            }
        }
        

        /// <summary>
        /// Remove entity cameras eye so the Head won't be iterating through them.
        /// Also destroy system just in case
        /// </summary>
        private void DisableEntityCameras()
        {
            // Currently only one system
            var eye = GetEye<GeometryVisionEntityEye>();
            if (eye != null)
            {
                RemoveEye<GeometryVisionEntityEye>();
                if (entityWorld.GetExistingSystem<GeometryVisionEntityEye>() != null)
                {
                    entityWorld.DestroySystem(eye);
                }
            }
        }

        private void RemoveEntityProcessors()
        {
            var processor = Head.GetProcessor<GeometryVisionEntityProcessor>();
            if (processor != null)
            {
                Head.RemoveProcessor<GeometryVisionEntityProcessor>();
                if (entityWorld.GetExistingSystem<GeometryVisionEntityProcessor>() != null)
                {
                    entityWorld.DestroySystem(processor);
                }
            }
        }

        public GeometryDataModels.Target GetClosestTarget(bool favorDistanceToCameraInsteadDistanceToRay)
        {
            if (favorDistanceToCameraInsteadDistanceToRay == false)
            {
                closestTarget = closestTargets[0];
            }
            else
            {
                throw new NotImplementedException();
            }

            return closestTarget;
        }

        /// <summary>
        /// Move entity or closest target
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="speedMultiplier"></param>
        public void MoveClosestTarget(Vector3 newPosition, float speedMultiplier)
        {
            var closestTarget = closestTargets[0];
            float speedHoldUp = 0.05f; //Needs to offset speed otherwise its too fast
            float movementSpeed = (closestTarget.distanceToCastOrigin * Time.deltaTime * speedHoldUp) * speedMultiplier;
            if (closestTarget.isEntity)
            {
                if (transformEntitySystem == null)
                {
                    transformEntitySystem = entityWorld.CreateSystem<TransformEntitySystem>();
                }

                StartCoroutine(moveEntityTarget(transformEntitySystem, newPosition, movementSpeed, closestTarget));
            }
            else
            {
                ///Since target component is multithreading friendly it cannot store transform, so this just uses the geoInfoObjects that is made for the gameobjects
                var geoInfo = GetGeoInfoBasedOnHashCode(closestTarget.GeoInfoHashCode);
                StartCoroutine(moveTarget(geoInfo.transform, newPosition, movementSpeed));
            }
        }

        public GeometryDataModels.GeoInfo GetGeoInfoBasedOnHashCode(int geoInfoHashCode)
        {
            var geoInfo = Head.GeoMemory.GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode() == geoInfoHashCode);
            return geoInfo;
        }

        public Transform GetTransformBasedOnGeoHashCode(int geoInfoHashCode)
        {
            var geoInfo = Head.GeoMemory.GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode() == geoInfoHashCode);
            return geoInfo.transform;
        }

        private IEnumerator moveTarget(Transform target, Vector3 newPosition, float speedMultiplier)
        {
            float timeOut = 10f;
            float stopMovingTreshold = 0.1f;
            while (Vector3.Distance(target.position, newPosition) > stopMovingTreshold)
            {
                var animatedPoint = Vector3.MoveTowards(target.position, newPosition, speedMultiplier);
                target.position = animatedPoint;
                if (timeOut < 0.1f)
                {
                    break;
                }

                timeOut -= 0.1f;

                yield return new WaitForSeconds(Time.deltaTime * 0.1f);
            }
        }

        private IEnumerator moveEntityTarget(TransformEntitySystem transformEntitySystem, Vector3 newPosition,
            float speedMultiplier, GeometryDataModels.Target target)
        {
            float timeOut = 10f;
            while (Vector3.Distance(target.position, newPosition) > 0.1)
            {
                var animatedPoint =
                    transformEntitySystem.MoveEntityToPosition(newPosition, ref closestTarget, speedMultiplier);

                target.position = animatedPoint;

                if (timeOut < 0.1f)
                {
                    break;
                }

                timeOut -= 0.1f;

                yield return new WaitForSeconds(Time.deltaTime * 0.1f);
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

        private void InitGeometryCameraForEntities(World world)
        {
            DisableEntityCameras();
            GeometryVisionEntityEye eEey = world.CreateSystem<GeometryVisionEntityEye>();
            AddEye(eEey);
            var eye = GetEye<GeometryVisionEntityEye>();
            eye.GeoVision = this;
            eye.TargetingInstructions = targetingInstructions;
            eye.Update();
        }

        /// <summary>
        /// Checks if geometrytype is targeted.
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

            //Creates default template scriptable object that can hold actions on what to do when targeting
            void AssignActionsForTargeting(int indexOf)
            {
                if (targetingInstruction.targetingActions == null)
                {
                    var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
                    newActions.name += "_" + indexOf;
                    targetingInstruction.targetingActions = newActions;
                }
            }

            //AddDefaultTargeting for both game objects and entities
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

            //RemoveDefaultTargeting for both game objects and entities
            void RemoveDefaultTargeting(
                GeometryTargetingSystemsContainer geometryTargetingSystemsContainer)
            {
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
        internal void ValidateTargetingSystems(List<VisionTarget> targetingInstructions)
        {
            foreach (var targetingInstruction in targetingInstructions)
            {
                if (gameObjectProcessing.Value == true)
                {
                    targetingInstruction.TargetingSystemGameObjects = RunValidation(
                        targetingInstruction.TargetingSystemGameObjects,
                        targetingInstruction, new GeometryObjectTargeting(), new GeometryLineTargeting());
                }

                if (entityBasedProcessing.Value == true && Application.isPlaying)
                {
                    if (entityWorld == null)
                    {
                        entityWorld = World.DefaultGameObjectInjectionWorld;
                    }
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

                //Same here
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
            this.fieldOfView = fieldOfView;
            this.camera.fieldOfView = fieldOfView;
            this.camera.farClipPlane = farDistanceOfView;
            GeometryUtility.CalculateFrustumPlanes(this.camera.projectionMatrix * this.camera.worldToCameraMatrix,
                planes);

            return planes;
        }

        /// <summary>
        /// When the camera is moved, rotated or both the frustum planes/view area thats
        /// are used to filter out what objects are processed needs to be refreshes/regenerated
        /// </summary>
        /// <param name="fieldOfView"></param>
        /// <returns>void</returns>
        /// <remarks>Faster way to get the current situation for planes might be to store planes into an object and move them with the eye</remarks>
        public void RegenerateVisionArea(float fieldOfView)
        {
            Camera1.fieldOfView = fieldOfView;
            Camera1.farClipPlane = farDistanceOfView;
            GeometryUtility.CalculateFrustumPlanes(this.camera.projectionMatrix * this.camera.worldToCameraMatrix,
                planes);
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
        public void AddEye<T>(T eye)
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
        public void AddEye<T>()
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
                if (entityWorld == null)
                {
                    entityWorld = World.DefaultGameObjectInjectionWorld;
                }

                var eye = entityWorld.CreateSystem<GeometryVisionEntityEye>();
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

        /// <summary>
        /// Gets targets for both entities and GameObject. Then sorts them.
        /// </summary>
        /// <returns>List<GeometryDataModels.Target> - new list of sorted targets.</returns>
        public List<GeometryDataModels.Target> GetClosestTargets()
        {
            List<GeometryDataModels.Target> newClosestTargets =
                new List<GeometryDataModels.Target>(GetTargetsForGameObjectsAndEntities());
            if (newClosestTargets.Count > 0)
            {
                closestTargets = newClosestTargets.OrderBy(target => target.distanceToRay).ToList();
            }

            return newClosestTargets;
        }


        /// <summary>
        /// Updates components internal targets list for both entities and GameObject. Then sorts them.
        /// </summary>
        public void UpdateClosestTargets()
        {
            List<GeometryDataModels.Target> newClosestTargets =
                new List<GeometryDataModels.Target>(GetTargetsForGameObjectsAndEntities());

            if (newClosestTargets.Count > 0)
            {
                closestTargets = newClosestTargets.OrderBy(target => target.distanceToRay).ToList();
            }
        }

        private List<GeometryDataModels.Target> GetTargetsForGameObjectsAndEntities()
        {
            List<GeometryDataModels.Target> newClosestTargets = new List<GeometryDataModels.Target>();
            foreach (var targetingInstruction in TargetingInstructions)
            {
                if (targetingInstruction.IsTargetingEnabled.Value == false)
                {
                    continue;
                }
                if (gameObjectProcessing.Value == true)
                {
                    newClosestTargets = targetingInstruction.TargetingSystemGameObjects.GetTargets(transform.position,
                        ForwardWorldCoordinate, this, targetingInstruction);
                }

                if (entityBasedProcessing.Value == true)
                {
                    var entityTargets =
                        targetingInstruction.TargetingSystemEntities.GetTargets(transform.position,
                            ForwardWorldCoordinate, this, targetingInstruction).ToList();
                    //Only update entities if the burst compiled job has finished its job OnUpdate
                    //If it has not finished it returns empty list.

                    closestEntityTargets = entityTargets;
                }
            }

            if (closestEntityTargets.Count > 0)
            {
                newClosestTargets.AddRange(closestEntityTargets); //add range but arrays(closestEntityTargets);
            }

            return newClosestTargets;
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

        public bool TargetsAreStatic { get; set; }

        public Object EntityFilterComponent
        {
            get { return entityFilterComponent; }
            set { entityFilterComponent = value; }
        }

        public List<VisionTarget> TargetingInstructions
        {
            get { return targetingInstructions; }
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

            DrawTargets(closestTargets);

            void DrawTargets(List<GeometryDataModels.Target> closestTargetsIn)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

                for (var i = 0; i < closestTargetsIn.Count; i++)
                {
                    var closestTarget = closestTargetsIn[i];
                    if (closestTarget.distanceToCastOrigin == 0f)
                    {
                        continue;
                    }

                    Vector3 resetToVector = Vector3.zero;
                    var position = DrawVisualIndicator(closestTarget.position, transform.position,
                        closestTarget.distanceToCastOrigin, Color.blue);
                    DrawVisualIndicator(closestTarget.projectedTargetPosition, closestTarget.position,
                        closestTarget.distanceToRay,
                        Color.green);
                    DrawVisualIndicator(position, closestTarget.projectedTargetPosition,
                        closestTarget.distanceToCastOrigin, Color.red);

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