using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.EntityScripts;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
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
        public string Id { get; set; }

        [SerializeField, Tooltip("Enables editor drawings for seeing targeting data")]
        private bool debugMode;

        //[SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private float farDistanceOfView = 1500f;

        [SerializeField, Tooltip("User given parameters as set of targeting instructions")]
        private List<TargetingInstruction> targetingInstructions;

        [SerializeField, Tooltip("Will enable the system to use GameObjects")]
        private BoolReactiveProperty gameObjectProcessing = new BoolReactiveProperty();

        [SerializeField, Tooltip("Include entities")]
        private BoolReactiveProperty entityBasedProcessing = new BoolReactiveProperty();
        private string defaultTag = "";
        private IDisposable gameObjectProcessingObservable = null;
        private IDisposable entityToggleObservable = null;
        private HashSet<IGeoEye> eyes = new HashSet<IGeoEye>();
        private Camera hiddenUnityCamera;
        private Plane[] planes = new Plane[6];
        private Vector3 forwardWorldCoordinate = Vector3.zero;
        private Transform cachedTransform;
        private List<GeometryDataModels.Target> closestTargets = new List<GeometryDataModels.Target>();
        private List<GeometryDataModels.Target> closestEntityTargets;
        private World entityWorld;
        private TransformEntitySystem transformEntitySystem;
        private GeometryVisionRunner runner;
        private bool favorDistanceToCameraInsteadDistanceToPointer = false;
        private bool useBounds;
        
        void Reset()
        {
            targetingInstructions = new List<TargetingInstruction>();
            InitializeGeometricVision();
            if (targetingInstructions.Count == 0)
            {
                targetingInstructions.Add(
                    new TargetingInstruction(GeometryType.Objects, DefaultTag, new GeometryObjectTargeting(), true,
                        null));
            }
        }

        //Awake is called when script is instantiated.
        //Call initialize on Awake to init systems in case Component is created on the factory method.
        void Awake()
        {
            InitializeGeometricVision();
        }

        // Start is called before the first frame update
        void Start()
        {
            InitializeGeometricVision();
        }
        
#if UNITY_EDITOR
        // On validate is called when there is a change in the UI
        void OnValidate()
        {
            cachedTransform = transform;
            InitializeTargeting(TargetingInstructions);
            ValidateTargetingSystems(targetingInstructions);
  
            ApplyEntityFilterChanges(targetingInstructions);
     
        }

        /// <summary>
        /// Applies changes made to entity filter type by the user from editor.
        /// 
        /// </summary>
        /// <remarks>It seems like the object type of script doesn't get saved, so it needs two variables to hold up information
        /// about the script</remarks>
        /// <param name="instructions"></param>
        private void ApplyEntityFilterChanges(List<TargetingInstruction> instructions)
        {
            foreach (var targetingInstruction in instructions)
            {
                targetingInstruction.SetCurrentEntityFilterType(targetingInstruction.EntityQueryFilter);
            }
        }
#endif

        /// <summary>
        /// Should be run in case making changes to GUI values from code and want the changes to happen before next frame(instantly).
        /// </summary>
        public void InitializeGeometricVision()
        {
            cachedTransform = transform;
            closestEntityTargets = new List<GeometryDataModels.Target>();
            InitUnityCamera();
            planes = RegenerateVisionArea(FieldOfView, planes);
            targetingInstructions = InitializeTargeting(targetingInstructions);

            GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView = fieldOfView,
                processGameObjects = gameObjectProcessing.Value,
                processEntities = entityBasedProcessing.Value,
                defaultTargeting = true,
                defaultTag = ""
            };

            GeometryVisionUtilities.SetupGeometryVision(this, targetingInstructions, factorySettings);
            InitEntitySwitch();
            InitGameObjectSwitch();
            UpdateTargetingSystemsContainer();
        }

        // Handles target initialization. Adds needed components and subscribes changing variables to logic that updates the targeting system.
        private List<TargetingInstruction> InitializeTargeting(List<TargetingInstruction> targetingInstructions)
        {
            var geoTargetingSystemsContainer = HandleAddingGeometryTargetingComponent();
            
            if (targetingInstructions == null)
            {
                targetingInstructions = new List<TargetingInstruction>();
            }

            foreach (var targetingInstruction in targetingInstructions)
            {
                OnTargetingEnabled(targetingInstruction, geoTargetingSystemsContainer);
            }

            return targetingInstructions;
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
        /// Inits unity camera which provides some needed features for geometric vision like Gizmos, planes and matrices.
        /// </summary>
        public void InitUnityCamera()
        {
            HiddenUnityCamera = gameObject.GetComponent<Camera>();

            if (HiddenUnityCamera == null)
            {
                HiddenUnityCamera = gameObject.AddComponent<Camera>();
            }
            #if STEAMVR
            HiddenUnityCamera.stereoTargetEye = StereoTargetEyeMask.None;
            #endif
            HiddenUnityCamera.usePhysicalProperties = true;

            HiddenUnityCamera.cameraType = CameraType.Game;
            HiddenUnityCamera.clearFlags = CameraClearFlags.Nothing;

            HiddenUnityCamera.enabled = false;
        }

        /// <summary>
        /// Handles all the required operation for GeometricVision to work with game objects.
        /// Such as GeometricVision eye/camera and processor for the data.
        /// The functionality is subscribed to a toggle that exists in the inspector GUI
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
                    Runner.RemoveProcessor<GeometryVisionProcessor>();
                    RemoveEye<GeometryVisionEye>();
                }
            }

            void InitGeometryProcessorForGameObjects()
            {
                if (Runner.gameObject.GetComponent<GeometryVisionProcessor>() == null)
                {
                    Runner.gameObject.AddComponent<GeometryVisionProcessor>();
                }

                Runner.AddProcessor(Runner.gameObject.GetComponent<GeometryVisionProcessor>());
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
                targetingInstruction = new TargetingInstruction(GeometryType.Objects, DefaultTag, targetingSystem, true,
                    null);
                AssignTargetingSystem(targetingSystem);
                targetingInstructions.Add(targetingInstruction);
            }
            else
            {
                AssignTargetingSystem(targetingSystem);
            }

            void AssignTargetingSystem(IGeoTargeting geoTargeting)
            {
                if (geoTargeting.IsForEntities())
                {
                    targetingInstruction.TargetingSystemEntities = geoTargeting;
                }
                else
                {
                    targetingInstruction.TargetingSystemGameObjects = geoTargeting;
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
                            entityWorld.DestroySystem(
                                (ComponentSystemBase) targetinginstruction.TargetingSystemEntities);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Remove entity cameras eye so the Head won't be iterating through them.
        /// Also destroy entity system.
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
            var processor = Runner.GetProcessor<GeometryVisionEntityProcessor>();
            if (processor != null)
            {
                Runner.RemoveProcessor<GeometryVisionEntityProcessor>();
                if (entityWorld.GetExistingSystem<GeometryVisionEntityProcessor>() != null)
                {
                    entityWorld.DestroySystem(processor);
                }
            }
        }

        public GeometryDataModels.Target GetClosestTarget()
        {
            if (closestTargets.Count == 0)
            {
                UpdateClosestTargets();
            }

            if (closestTargets.Count > 0)
            {
                return closestTargets[0];
            }

            return new GeometryDataModels.Target();
        }

        public GeometryDataModels.GeoInfo GetGeoInfoBasedOnHashCode(int geoInfoHashCode)
        {
            var geoInfo = Runner.GeoMemory.GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode() == geoInfoHashCode);
            return geoInfo;
        }

        public Transform GetTransformBasedOnGeoHashCode(int geoInfoHashCode)
        {
            var geoInfo = Runner.GeoMemory.GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode() == geoInfoHashCode);
            return geoInfo.transform;
        }


        private void InitGeometryProcessorForEntities(bool toEntities, World world)
        {
            RemoveEntityProcessors();
            world.CreateSystem<GeometryVisionEntityProcessor>();
            IGeoProcessor eProcessor = world.GetExistingSystem<GeometryVisionEntityProcessor>();
            Runner.AddProcessor(eProcessor);
            var addedProcessor = Runner.GetProcessor<GeometryVisionEntityProcessor>();
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
        /// Add targeting implementation, if it is enabled on the inspector.
        /// Subscribes the targeting toggle button to functionality than handles creation of default targeting implementation for the
        /// targeted geometry type
        /// </summary>
        /// <param name="targetingInstruction"></param>
        /// <param name="geoTargetingSystemsContainer"></param>
        private void OnTargetingEnabled(TargetingInstruction targetingInstruction,
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
                if (targetingInstruction.TargetingActions == null)
                {
                    var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
                    newActions.name += "_" + indexOf;
                    targetingInstruction.TargetingActions = newActions;
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
        internal void ValidateTargetingSystems(List<TargetingInstruction> targetingInstructions)
        {
            foreach (var targetingInstruction in targetingInstructions)
            {
                
                if (gameObjectProcessing.Value == true)
                {
                    targetingInstruction.TargetingSystemGameObjects = AssignNewTargetingSystem(targetingInstruction,
                        new GeometryObjectTargeting(), new GeometryLineTargeting());
                }

                if (entityBasedProcessing.Value == true && Application.isPlaying)
                {
                    if (entityWorld == null)
                    {
                        entityWorld = World.DefaultGameObjectInjectionWorld;
                    }

                    var newObjectTargetng = entityWorld.CreateSystem<GeometryEntitiesObjectTargeting>();
                    var newLineTargetng = entityWorld.CreateSystem<GeometryEntitiesLineTargeting>();

                    targetingInstruction.TargetingSystemEntities =
                        AssignNewTargetingSystem(targetingInstruction, newObjectTargetng, newLineTargetng);
                }
            }

            IGeoTargeting AssignNewTargetingSystem(TargetingInstruction visionTarget1, IGeoTargeting newObjectTargeting,
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
            this.hiddenUnityCamera.fieldOfView = fieldOfView;
            this.hiddenUnityCamera.farClipPlane = farDistanceOfView;
            GeometryUtility.CalculateFrustumPlanes(
                this.hiddenUnityCamera.projectionMatrix * this.hiddenUnityCamera.worldToCameraMatrix,
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
            this.HiddenUnityCamera.fieldOfView = fieldOfView;
            this.HiddenUnityCamera.farClipPlane = farDistanceOfView;
            GeometryUtility.CalculateFrustumPlanes(
                this.HiddenUnityCamera.projectionMatrix * this.HiddenUnityCamera.worldToCameraMatrix, planes);
        }
        
        /// <summary>
        /// Use this to remove eye game object or entity implementation.
        /// Also handles removing the MonoBehaviour component if the implementation is one 
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// <summary>
        /// Used to get the eye implementation for either game object or entities from hash set.
        /// </summary>
        /// <typeparam name="T">Implementation to get. If none exists return default T</typeparam>
        /// <returns></returns>
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
        /// <typeparam name="T"></typeparam>
        public void AddEye<T>()
        {
            if (eyes == null)
            {
                eyes = new HashSet<IGeoEye>();
            }

            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                HandleGameObjectEyeAddition();
                
            }
            else
            {
                HandleEntityEyeAddition();
            }
            
            void HandleGameObjectEyeAddition()
            {
                var eye = IfNoEyeComponentAddAndInit();
                
                AddEyeToCollectionOfInterfaceImplementations(eye);
            }
            
            void HandleEntityEyeAddition()
            {
                if (GetEye<T>() == null)
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
            //Creates the eye
            Component IfNoEyeComponentAddAndInit()
            {
                var eye = gameObject.GetComponent(typeof(T));
                if (eye == null)
                {
                    gameObject.AddComponent(typeof(T));
                    var addedEye = (IGeoEye) gameObject.GetComponent(typeof(T));
                    eye = InitEye(addedEye);
                }
                else
                {
                    var addedEye = (IGeoEye) gameObject.GetComponent(typeof(T));
                    eye = InitEye(addedEye);
                }
                return eye;
                
                Component InitEye(IGeoEye addedEye)
                {
                    addedEye.Runner = Runner;
                    addedEye.Id = new Hash128().ToString();
                    addedEye.GeoVision = this;
                    return(Component) addedEye;
                }
            }
            //Checks that the eye is valid and add it to hashset for faster access avoiding get component calls
            void AddEyeToCollectionOfInterfaceImplementations(Component eye)
            {
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
        }


        /// <summary>
        /// Gets the first targeting instruction matching the give type as GeometryType.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TargetingInstruction GetTargetingInstructionOfType(GeometryType type)
        {
            TargetingInstruction instructionToReturn = null;
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

            return closestTargets;
        }

        /// <summary>
        /// Updates components internal targets list for both entities and GameObject. Then sorts them.
        /// It has currently hidden feature to allow target sorting on based how close the targets are to the camera.
        /// </summary>
        public void UpdateClosestTargets()
        {
            List<GeometryDataModels.Target> newClosestTargets =
                new List<GeometryDataModels.Target>(GetTargetsForGameObjectsAndEntities());
            if (newClosestTargets.Count > 0)
            {
                if (favorDistanceToCameraInsteadDistanceToPointer == false)
                {
                    //TODO: Native list and burst compile in job this
                    closestTargets = newClosestTargets.OrderBy(target => target.distanceToRay).ToList();
                }
                else
                {
                    //TODO: Native list and burst ompile in job this
                    //TODO:After 1.0 Find a good use case where this can be used and improve it.
                    closestTargets = newClosestTargets.OrderBy(target => target.distanceToCastOrigin).ToList();
                }
            }
            else
            {
                closestTargets.Clear();
            }
        }

        /// <summary>
        /// Move entity or closest target
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="speedMultiplier"></param>
        /// <param name="distanceToStop"></param>
        public void MoveClosestTargetToPosition(Vector3 newPosition, float speedMultiplier, float distanceToStop)
        {
            var closestTarget = GetClosestTarget();
            float movementSpeed = closestTarget.distanceToCastOrigin * Time.deltaTime * speedMultiplier;

            if (closestTarget.isEntity)
            {
                MainThreadDispatcher.StartUpdateMicroCoroutine(MoveEntityTarget(newPosition, movementSpeed,
                    closestTarget, distanceToStop));
            }
            else
            {
                //Since target component is multi threading friendly it cannot store transform, so this just uses the geoInfoObject that is made for the game objects
                var geoInfo = GetGeoInfoBasedOnHashCode(closestTarget.GeoInfoHashCode);
                MainThreadDispatcher.StartUpdateMicroCoroutine(
                    GeometryVisionUtilities.MoveTarget(geoInfo.transform, newPosition, movementSpeed, distanceToStop));
            }
        }

        public void DestroyTarget(GeometryDataModels.Target target)
        {
            if (target.isEntity)
            {
                if (transformEntitySystem == null)
                {
                    transformEntitySystem = entityWorld.CreateSystem<TransformEntitySystem>();
                }

                transformEntitySystem.DestroyTargetEntity(target);
            }
            else
            {
                var geoInfo = GetGeoInfoBasedOnHashCode(target.GeoInfoHashCode);
                Destroy(geoInfo.gameObject);
            }
        }

        public IEnumerator DestroyTargetAtDistance(GeometryDataModels.Target target, float distanceToDestroyAt)
        {
            var targetToBeDestroyed = target;
            if (targetToBeDestroyed.distanceToCastOrigin == 0)
            {
                yield break;
            }

            float timeOut = 1f;
            while (targetToBeDestroyed.distanceToCastOrigin > distanceToDestroyAt)
            {
                if (timeOut < 0.1f)
                {
                    DestroyTarget(targetToBeDestroyed);
                    break;
                }

                timeOut -= Time.deltaTime;
                yield return null;
            }

            DestroyTarget(targetToBeDestroyed);
        }

        /// <summary>
        /// Trigger assets and parameters from actions template object on each targeting instruction
        /// </summary>
        public void TriggerTargetingActions()
        {
            foreach (var targetingInstruction in targetingInstructions)
            {
                var actions = targetingInstruction.TargetingActions;
                if (actions.StartActionEnabled)
                {
                    MainThreadDispatcher.StartUpdateMicroCoroutine(TimedSpawnDespawn.TimedSpawnDeSpawnService(actions.StartActionObject, actions.StartDelay,
                        actions.StartDuration, cachedTransform, GeometryVisionSettings.NameOfStartingEffect));  
                }

                if (actions.MainActionEnabled)
                {
                    MainThreadDispatcher.StartUpdateMicroCoroutine(TimedSpawnDespawn.TimedSpawnDeSpawnService(
                        actions.MainActionObject,
                        actions.MainActionDelay, actions.MainActionDuration, cachedTransform,
                        GeometryVisionSettings.NameOfMainEffect));
                }

                if (actions.EndActionEnabled)
                {
                    MainThreadDispatcher.StartUpdateMicroCoroutine(TimedSpawnDespawn.TimedSpawnDeSpawnService(
                        actions.EndActionObject,
                        actions.EndDelay, actions.EndDuration, cachedTransform,
                        GeometryVisionSettings.NameOfEndEffect));
                }
            }
        }

        private IEnumerator MoveEntityTarget(Vector3 newPosition,
            float speedMultiplier, GeometryDataModels.Target target, float distanceToStop)
        {
            if (transformEntitySystem == null)
            {
                transformEntitySystem = entityWorld.CreateSystem<TransformEntitySystem>();
            }

            float timeOut = 2f;
            while (Vector3.Distance(target.position, newPosition) > distanceToStop)
            {
                var animatedPoint =
                    transformEntitySystem.MoveEntityToPosition(newPosition, target, speedMultiplier);

                target.position = animatedPoint;
                if (closestTargets.Count != 0)
                {
                    closestTargets[0] = target;
                }

                if (timeOut < 0.1f)
                {
                    break;
                }

                timeOut -= Time.deltaTime;

                yield return null;
            }
        }

        /// <summary>
        /// Use to get list of targets containing data from entities and gameObjects. 
        /// </summary>
        /// <returns>List of target objects that can be used to find out closest target.</returns>
        private List<GeometryDataModels.Target> GetTargetsForGameObjectsAndEntities()
        {
            List<GeometryDataModels.Target> newClosestTargets = new List<GeometryDataModels.Target>();
            foreach (var targetingInstruction in TargetingInstructions)
            {
                if (targetingInstruction.IsTargetingEnabled.Value == false)
                {
                    continue;
                }

                newClosestTargets = GetGameObjectTargets(targetingInstruction);
                closestEntityTargets = GetEntityTargets(targetingInstruction);
            }

            newClosestTargets = CombineEntityAndGameObjectTargets(newClosestTargets, closestEntityTargets);

            return newClosestTargets;

            //
            //Functions//
            //

            //Runs the gameObject implementation of the IGeoTargeting interface 
            List<GeometryDataModels.Target> GetGameObjectTargets(TargetingInstruction targetingInstruction)
            {
                if (gameObjectProcessing.Value == true)
                {
                    newClosestTargets = targetingInstruction.TargetingSystemGameObjects.GetTargets(transform.position,
                        ForwardWorldCoordinate, this, targetingInstruction);
                }

                return newClosestTargets;
            }

            //Runs the entity implementation of the IGeoTargeting interface 
            List<GeometryDataModels.Target> GetEntityTargets(TargetingInstruction targetingInstruction)
            {
                if (entityBasedProcessing.Value == true)
                {
                    var entityTargets =
                        targetingInstruction.TargetingSystemEntities.GetTargets(transform.position,
                            ForwardWorldCoordinate, this, targetingInstruction).ToList();
                    //Only update entities if the burst compiled job has finished its job OnUpdate
                    //If it has not finished it returns empty list.

                    return entityTargets;
                }

                return new List<GeometryDataModels.Target>();
            }

            //Combines 2 lists of targets and return both list as one.
            List<GeometryDataModels.Target> CombineEntityAndGameObjectTargets(
                List<GeometryDataModels.Target> gameObjectTargets,
                List<GeometryDataModels.Target> entityTargets)
            {
                if (entityTargets.Count > 0)
                {
                    gameObjectTargets.AddRange(entityTargets); //add range but arrays(closestEntityTargets);
                }

                return gameObjectTargets;
            }
        }


        public void ApplyDefaultTagToTargetingInstructions()
        {
            foreach (var targetingInstruction in TargetingInstructions)
            {
                targetingInstruction.TargetTag = defaultTag;
            }
        }

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

        public List<TargetingInstruction> TargetingInstructions
        {
            get { return targetingInstructions; }
        }

        public Plane[] Planes
        {
            get { return planes; }
            set { planes = value; }
        }

        public Camera HiddenUnityCamera
        {
            get { return hiddenUnityCamera; }
            set { hiddenUnityCamera = value; }
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

        public string DefaultTag
        {
            get { return defaultTag; }
            set { defaultTag = value; }
        }
        
        public GeometryVisionRunner Runner
        {
            get { return runner; }
            set { runner = value; }
        }
        
        public int GetClosestTargetCount()
        {
            return closestTargets.Count;
        }
        
        public bool UseBounds
        {
            get { return useBounds; }
            set { useBounds = value; }
        }

        public bool CollidersTargeted { get; set; }
#if UNITY_EDITOR

        /// <summary>
        /// Used for debugging geometry vision and is responsible for drawing debugging info from the data providid by
        /// GeometryVision plugin
        /// </summary>
        private void OnDrawGizmos()
        {
            if (HiddenUnityCamera)
            {
                HiddenUnityCamera.fieldOfView = this.fieldOfView;
            }

            InitUnityCamera();
            RegenerateVisionArea(fieldOfView);
            if (!debugMode)
            {
                return;
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, ForwardWorldCoordinate);

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
                    var position = DrawTargetingVisualIndicators(closestTarget.position, transform.position,
                        closestTarget.distanceToCastOrigin, Color.blue);
                    DrawTargetingVisualIndicators(closestTarget.projectedTargetPosition, closestTarget.position,
                        closestTarget.distanceToRay, Color.green);
                    DrawTargetingVisualIndicators(position, closestTarget.projectedTargetPosition,
                        closestTarget.distanceToCastOrigin, Color.red);
                    DrawTargetingInfo(closestTarget.position, Vector3.down, i);

                    Vector3 DrawTargetingVisualIndicators(Vector3 spherePosition, Vector3 lineStartPosition,
                        float distance,
                        Color color)
                    {
                        Gizmos.color = color;
                        Gizmos.DrawLine(lineStartPosition, spherePosition);
                        Gizmos.DrawSphere(spherePosition, 0.3f);
                        resetToVector = closestTarget.projectedTargetPosition - lineStartPosition;
                        Handles.Label((resetToVector / 2) + lineStartPosition, "distance: \n" + distance);
                        return lineStartPosition;
                    }

                    void DrawTargetingInfo(Vector3 textLocation, Vector3 offset, int order)
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