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
    [DisallowMultipleComponent]
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

        private World entityWorld;
        private EndSimulationEntityCommandBufferSystem commandBufferSystem;

        public List<GeometryDataModels.Target> ClosestTargets
        {
            get { return closestTargets; }
        }

        void Reset()
        {
            InitializeSystems();
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
            ValidateTargetingSystems(targetingInstructions);
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
            
            // Handles target initialization. Adds needed components and subscribes changing variables to logic that updates the targeting system.
            void InitializeTargeting(List<VisionTarget> targets)
            {
                var geoTargetingSystemsContainer = HandleAddingGeometryTargetingComponent();
                foreach (var targetingInstructions in targets)
                {
                    if (targetingInstructions.Target.Value == true)
                    {
                        AssignActionsForTargeting(targetingInstructions, targets.IndexOf(targetingInstructions));
                        OnTargetingEnabled(targetingInstructions, geoTargetingSystemsContainer);
                    }
                }

                ValidateTargetingSystems(targets);
            }
            
            GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView =  fieldOfView,
                processGameObjects = gameObjectProcessing.Value,
                processEntities = entityBasedProcessing.Value,
                defaultTargeting = true,
            };
            
            GeometryVisionUtilities.SetupGeometryVision(Head, this, targetingInstructions, factorySettings);
            InitEntitySwitch();
            InitGameObjectSwitch();
            UpdateTargetingSystemsContainer();
        }

        private void UpdateTargetingSystemsContainer()
        {
            var tatgetingSystemsContainer = GetComponent<GeometryTargetingSystemsContainer>();
            tatgetingSystemsContainer.TargetingPrograms = new HashSet<IGeoTargeting>();
            foreach (var targetingInstruction in targetingInstructions)
            {
                if (targetingInstruction.TargetingSystemGameObjects != null)
                {
                    tatgetingSystemsContainer.TargetingPrograms.Add(targetingInstruction.TargetingSystemGameObjects);
                }       
                if (targetingInstruction.TargetingSystemEntities != null)
                {
                    tatgetingSystemsContainer.TargetingPrograms.Add(targetingInstruction.TargetingSystemEntities);
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
        /// The functionality is subscribed to a button on the inspector GUI
        /// </summary>
        private void InitGameObjectSwitch()
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
                        GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
                        {
                            fielOfView =  fieldOfView,
                            processGameObjects = gameObjectProcessing.Value,
                            processEntities = entityBasedProcessing.Value,
                            defaultTargeting = true,
                        };
                        GeometryVisionUtilities.SetupGeometryVisionEye(Head, this, factorySettings);
                    }

                    InitGeometryProcessorForGameObjects();
                    IfNoDefaultTargetingAddOne();
            
                    void IfNoDefaultTargetingAddOne()
                    {
                        //Handle gameObject targeting system
                        if (GameObjectBasedProcessing.Value == true && isGeometryTypeTargetingSystemAdded(targetingInstructions, GeometryType.Objects) == true)
                        {
                            GetTargetingInstructionOfType(GeometryType.Objects).TargetingSystemGameObjects =
                                new GeometryObjectTargeting();
                        }
                        else if (GameObjectBasedProcessing.Value == true && isGeometryTypeTargetingSystemAdded(targetingInstructions, GeometryType.Objects) == false)
                        {
                            targetingInstructions.Add(new VisionTarget(GeometryType.Objects, 0, new GeometryObjectTargeting(), true));
                        }
                    }
                }
                else if (gameObjectBasedProcessing == false)
                {
                    
                    Head.RemoveProcessor<GeometryVisionProcessor>();
                    
                    DestroyGameObjectBasedGeometryCamera(geoEye);
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
            
            void DestroyGameObjectBasedGeometryCamera(GeometryVisionEye eye)
            {
                while (GetEye<GeometryVisionEye>())
                {
                    RemoveEye<GeometryVisionEye>();
                }

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
        /// Handles all the required operation for GeometricVision to work with entities.
        /// Such as GeometricVision eye/camera, processor for the data and targeting system
        /// The functionality is subscribed to a button on the inspector GUI
        /// </summary>
        private void InitEntitySwitch()
        {
            if (entityToggleObservable != null)
            {
                return;
            }
            entityToggleObservable = EntityBasedProcessing.Subscribe(entitiesEnabled =>
            {
                InitEntities(entitiesEnabled);

                void InitEntities(bool entitiesBasedProcessing)
                {
                    entityWorld = World.DefaultGameObjectInjectionWorld;

                    InitGeometryProcessorForEntities(entitiesBasedProcessing, entityWorld);
                    InitGeometryCameraForEntities(entitiesBasedProcessing, entityWorld);
                    IfNoDefaultTargetingAddOne();
                }
            });
            
            void IfNoDefaultTargetingAddOne()
            {
                //Handle entity targeting system
                if (entityBasedProcessing.Value == true && isGeometryTypeTargetingSystemAdded(targetingInstructions, GeometryType.Objects)== true)
                {
                    GetTargetingInstructionOfType(GeometryType.Objects).TargetingSystemEntities = new GeometryEntitiesObjectTargeting();
                }
                else if (entityBasedProcessing.Value == true && isGeometryTypeTargetingSystemAdded(targetingInstructions, GeometryType.Objects) == false)
                {
                    targetingInstructions.Add(new VisionTarget(GeometryType.Objects, 0, new GeometryEntitiesObjectTargeting(), true));
                }
            }
        }

        private void InitGeometryProcessorForEntities(bool toEntities, World world)
        {
            if (toEntities)
            {
                while (Head.GetProcessor<GeometryVisionEntityProcessor>() != null)
                {
                    Head.RemoveProcessor<GeometryVisionEntityProcessor>();
                }
                world.CreateSystem<GeometryVisionEntityProcessor>();
                IGeoProcessor eProcessor = world.GetExistingSystem<GeometryVisionEntityProcessor>();
                Head.AddProcessor(eProcessor);
                var addedProcessor = Head.GetProcessor<GeometryVisionEntityProcessor>();
                addedProcessor.GeoVision = this;
                addedProcessor.CheckSceneChanges(this);
                addedProcessor.Enabled = true;
                addedProcessor.Update();
            }

            if (toEntities == false)
            {
                while (Head.GetProcessor<GeometryVisionEntityProcessor>() != null)
                {
                    Head.RemoveProcessor<GeometryVisionEntityProcessor>();
                }
            }
        }

        private void InitGeometryCameraForEntities(bool toEntities, World world)
        {
            if (toEntities)
            {
                RemoveEye<GeometryVisionEntityEye>();

                world.CreateSystem<GeometryVisionEntityEye>();
                GeometryVisionEntityEye eEey =
                    (GeometryVisionEntityEye) world.GetExistingSystem(typeof(GeometryVisionEntityEye));
                AddEye(eEey);
                var eye = GetEye<GeometryVisionEntityEye>();
                eye.GeoVision = this;
                eye.TargetedGeometries = targetingInstructions;

                eye.Enabled = true;
                eye.Update();
            }
            else
            {
                RemoveEye<GeometryVisionEntityEye>();
            }
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
        bool isGeometryTypeTargetingSystemAdded(List<VisionTarget> targetedGeometries, GeometryType geoType)
        {
            bool targetingTypeFound = false;
            foreach (var geometryType in targetedGeometries)
            {
                if (geometryType.GeometryType == geoType)
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
        private void OnTargetingEnabled(VisionTarget targetingInstruction, GeometryTargetingSystemsContainer geoTargetingSystemsContainer)
        {
            if (!targetingInstruction.Subscribed)
            {
                targetingInstruction.Target.Subscribe(targeting =>
                {
                    AddRemoveDefaultTargeting(targetingInstruction, geoTargetingSystemsContainer, targeting);
                });
                targetingInstruction.Subscribed = true;
            }
            
            void AddRemoveDefaultTargeting(VisionTarget visionTarget, GeometryTargetingSystemsContainer geometryTargetingSystemsContainer, bool targetingEnabled)
            {
                if (targetingEnabled)
                {
                    //Cannot get Reactive value from serialized property, so this boolean variable handles it job on the inspector gui under the hood.
                    //The other way is to find out how to get reactive value out of serialized property. Shows option for adding actions template from the inspector GUI
                    visionTarget.TargetActionsTemplateSlotVisible = true;
                    
                    geometryTargetingSystemsContainer.AddTargetingProgram(visionTarget);
                }
                else
                {
                    //Do the same thing here
                    visionTarget.TargetActionsTemplateSlotVisible = false;
                    geometryTargetingSystemsContainer.RemoveTargetingProgram(visionTarget);
                }
            }
        }

        /// <summary>
        /// In case the user plays around with the settings on the inspector and changes thins this needs to be run.
        /// It checks that the targeting system implementations are correct.
        /// </summary>
        /// <param name="targets"></param>
        private void ValidateTargetingSystems(List<VisionTarget> targets)
        {
            foreach (var visionTarget2 in targets)
            {
                if (gameObjectProcessing.Value == true)
                {
                    visionTarget2.TargetingSystemGameObjects = RunValidation( visionTarget2.TargetingSystemGameObjects, visionTarget2, new GeometryObjectTargeting(),  new GeometryLineTargeting());
                }
                if (entityBasedProcessing.Value == true)
                {
                    visionTarget2.TargetingSystemEntities = RunValidation( visionTarget2.TargetingSystemEntities, visionTarget2, new GeometryEntitiesObjectTargeting(), new GeometryEntitiesLineTargeting());
                }
            }
            
            IGeoTargeting RunValidation( IGeoTargeting targetingToValidate, VisionTarget visionTarget, IGeoTargeting newObjectTargeting, IGeoTargeting newLineTargeting)
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
            
            IGeoTargeting AssignNewTargetIngSystem(VisionTarget visionTarget1, IGeoTargeting newObjectTargeting, IGeoTargeting newLineTargeting)
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
        }

        public void RemoveEye<T>()
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList(typeof(T), ref eyes);
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
            foreach (var targetingInstructions in TargetingInstructions)
            {
                if (gameObjectProcessing.Value)
                {
                    closestTargets.AddRange( targetingInstructions.TargetingSystemGameObjects.GetTargets(transform.position, ForwardWorldCoordinate, GeoInfos));  
                }

                if (entityBasedProcessing.Value)
                {
                    closestTargets.AddRange( targetingInstructions.TargetingSystemEntities.GetTargets(transform.position, ForwardWorldCoordinate, GeoInfos)); 
                }
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
            if (!debugMode)
            {
                return;
            }

            var transform1 = transform;
            UnityEngine.Debug.DrawLine(transform1.position, ForwardWorldCoordinate, Color.blue, 1);



            DrawTargets(ClosestTargets);

            void DrawTargets(List<GeometryDataModels.Target> closestTargetsIn)
            {
                foreach (var closestTarget in closestTargetsIn)
                {
                    Vector3 resetToVector = Vector3.zero;
                    var position = DrawVisualIndicator(closestTarget.position, transform.position,
                        closestTarget.distanceToCastOrigin, Color.blue);
                    DrawVisualIndicator(closestTarget.projectedTargetPosition, closestTarget.position, closestTarget.distanceToRay,
                        Color.green);
                    DrawVisualIndicator(position, closestTarget.projectedTargetPosition, closestTarget.distanceToCastOrigin,
                        Color.red);

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
                }
            }
        }
#endif
        
    }
}