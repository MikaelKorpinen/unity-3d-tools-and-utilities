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
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
        public UnityEngine.Hash128 Id { get; set; }

        [SerializeField, Tooltip("Enables editor drawings for seeing targeting data")]
        private bool debugMode;

        [SerializeField] private float fieldOfView = 25f;
        [SerializeField] private float farDistanceOfView = 1500f;

        [SerializeField, Tooltip("User given parameters as set of targeting instructions")]
        private List<TargetingInstruction> targetingInstructions;

        [SerializeField, Tooltip("Will enable the system to use GameObjects")]
        private BoolReactiveProperty gameObjectProcessing = new BoolReactiveProperty();

        [SerializeField, Tooltip("Will enable the system to use entities")]
        private BoolReactiveProperty entityProcessing = new BoolReactiveProperty();

        private IDisposable gameObjectProcessingObservable = null;
        private IDisposable entityToggleObservable = null;
        private HashSet<IGeoEye> eyes = new HashSet<IGeoEye>();
        private Camera hiddenUnityCamera;
        private Plane[] planes = new Plane[6];
        private Vector3 forwardWorldCoordinate = Vector3.zero;
        private Transform cachedTransform;
        private NativeArray<GeometryDataModels.Target> closestTargetsContainer;
        private NativeSlice<GeometryDataModels.Target> closestTargets;
        private NativeSlice<GeometryDataModels.Target> closestEntityTargets;
        private NativeSlice<GeometryDataModels.Target> closestGameObjectTargets;
        private World entityWorld;
        private TransformEntitySystem transformEntitySystem;
        private GeometryVisionRunner runner;
        private bool favorDistanceToCameraInsteadDistanceToPointer = false;
        private bool useBounds = false;
        private string defaultTag = "";
        [SerializeField] private int maxTargets = 100000;
        private int amountOfTargets = 0;

        [SerializeField,
         Tooltip(
             "How often system will check for object scene changes like new or destroyed game objects. This can improve performance in case you have many GameObjects and the scene doesn't get a lot of changes like gameObjects spawning in and out. Zero is checks every frame.")]
        private float checkEnvironmentChangesTimeInterval = 0.0f;

        void Reset()
        {
            InitializeGeometricVision(new List<GeometryType>());
        }

        //Awake is called when script is instantiated.
        //Call initialize on Awake to init systems in case Component is created on the factory method.
        void Awake()
        {
            if (EntityWorld == null)
            {
                entityWorld = World.DefaultGameObjectInjectionWorld;
            }

            if (maxTargets == 0)
            {
                maxTargets = 1000000;
            }
            closestTargetsContainer = new NativeArray<GeometryDataModels.Target>(maxTargets, Allocator.Persistent);
        }

        // Start is called before the first frame update
        void Start()
        {
            InitializeGeometricVision(new List<GeometryType>());
        }
        
        /// <summary>
        /// Should be run before start or in case making changes to GUI values from code and want the changes to happen before next frame(instantly).
        /// 
        /// </summary>
        public void InitializeGeometricVision(List<GeometryType> additionalGeometryTypesToProcess)
        {
            cachedTransform = transform;
            InitUnityCamera();
            planes = RegenerateVisionArea(FieldOfView, planes);
            var factory = new GeometryVisionFactory();
            factory.AddAdditionalTargets(this, additionalGeometryTypesToProcess);
            targetingInstructions = factory.InitializeTargeting(targetingInstructions, this, entityProcessing, gameObjectProcessing);
            factory.CreateGeometryVisionRunner(this);
            factory.InitEntitySwitch(entityProcessing, entityToggleObservable, this, transformEntitySystem);
            factory.InitGameObjectSwitch(gameObjectProcessingObservable, gameObjectProcessing,this );
        }

        private void OnApplicationQuit()
        {
            closestTargetsContainer.Dispose();
        }
        
        void OnValidate()
        {
            cachedTransform = transform;
            targetingInstructions = AddDefaultInstructionIfNone(targetingInstructions, entityProcessing.Value,
                gameObjectProcessing.Value);
            targetingInstructions = GeometryVisionUtilities.ValidateTargetingSystems(targetingInstructions,
                gameObjectProcessing.Value, entityProcessing.Value, entityWorld);
            if (targetingInstructions.Count != 1)
            {
                var currentTargetingInstruction = targetingInstructions[0];
                targetingInstructions = new List<TargetingInstruction>(1);
                targetingInstructions.Add(currentTargetingInstruction);
            }
            
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

        internal void ApplyActionsTemplateObject(ActionsTemplateObject template)
        {
            foreach (var targetingInstruction in targetingInstructions)
            {
                targetingInstruction.TargetingActions = template;
            }
        }

        internal List<TargetingInstruction> AddDefaultInstructionIfNone(
            List<TargetingInstruction> targetingInstructionsIn, bool entityBased, bool gameObjectBased)
        {
            if (targetingInstructionsIn == null)
            {
                targetingInstructionsIn = new List<TargetingInstruction>();
            }

            if (targetingInstructionsIn.Count == 0)
            {
                GeometryEntitiesObjectTargeting entityTargeting = null;
                GeometryObjectTargeting objectTargeting = null;

                if (entityBased)
                {
                    var world = World.DefaultGameObjectInjectionWorld;
                    entityTargeting = world.CreateSystem<GeometryEntitiesObjectTargeting>();
                }

                if (gameObjectBased)
                {
                    objectTargeting = new GeometryObjectTargeting();
                }

                targetingInstructionsIn.Add(
                    new TargetingInstruction(GeometryType.Objects, defaultTag, (entityTargeting, objectTargeting), true,
                        null));
            }

            return targetingInstructionsIn;
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
            HiddenUnityCamera.aspect = 1f;

            HiddenUnityCamera.cameraType = CameraType.Game;
            HiddenUnityCamera.clearFlags = CameraClearFlags.Nothing;

            HiddenUnityCamera.enabled = false;
        }

        /// <summary>
        /// Gets closest entity or gameObject as target according to parameters set by the user from this component.
        /// If there is no targets it tries to first update the closest target list.
        /// </summary>
        /// <returns>GeometryDataModels.Target that contain information about gameObject or entity</returns>
        public GeometryDataModels.Target GetClosestTarget()
        {
            if (closestTargets.Length > 0)
            {
                return closestTargets[0];
            }

            return new GeometryDataModels.Target();
        }

        /// <summary>
        /// Fetch the data structure for gameObject based on the hash code usually got from the target object.
        /// Target object cannot contain transforms or gameObjects, because its not safe.
        /// </summary>
        /// <param name="geoInfoHashCode">Hash code that you can get from closest target</param>
        /// <returns>geoInfo object that contains reference to gameObject related data</returns>
        public GeometryDataModels.GeoInfo GetGeoInfoBasedOnHashCode(int geoInfoHashCode)
        {
            var geoInfo = Runner.GeoMemory.GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode() == geoInfoHashCode);
            return geoInfo;
        }

        /// <summary>
        /// Gets you a transform of a target object based on the hashCode inside it.
        /// </summary>
        /// <param name="geoInfoHashCode">The hashcode from target object</param>
        /// <returns>related gameObjects transform</returns>
        public Transform GetTransformBasedOnGeoHashCode(int geoInfoHashCode)
        {
            var geoInfo = Runner.GeoMemory.GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode() == geoInfoHashCode);
            return geoInfo.transform;
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
        /// Use this to remove eye game object or entity implementation.
        /// Also handles removing the MonoBehaviour component if the implementation is one 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveEntityEye<T>() where T : ComponentSystemBase
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList(typeof(T), ref eyes);

            var system = EntityWorld.GetExistingSystem<T>();
            if (system != null)
            {
                EntityWorld.DestroySystem(system);
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
        /// <typeparam name="T"></typeparam>
        public void AddGameObjectEye<T>()
        {
            HandleGameObjectEyeAddition();

            //local functions//

            void HandleGameObjectEyeAddition()
            {
                InterfaceUtilities.AddImplementation<IGeoEye, GeometryVisionEye>(InitEye, Eyes, gameObject);

                IGeoEye InitEye()
                {
                    var addedEye = (IGeoEye) gameObject.GetComponent(typeof(T));
                    addedEye.Runner = Runner;
                    addedEye.Id = new Hash128().ToString();
                    addedEye.GeoVision = this;
                    var added = (Component) addedEye;
                    return (IGeoEye) added;
                }
            }
        }

        /// <summary>
        /// Adds eye/camera component to the list and makes sure that the implementation to be added is unique.
        /// Does not add duplicate implementation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void AddEntityEye<T>() where T : ComponentSystemBase, IGeoEye, new()
        {
            GeometryVisionUtilities.HandleEntityImplementationAddition(GetEye<T>(), eyes, EntityWorld, InitEye);

            //local functions//

            void InitEye()
            {
                var addedEye = (IGeoEye) GetEye<T>();
                addedEye.Runner = runner;
                addedEye.Id = new Hash128().ToString();
                addedEye.GeoVision = this;
            }
        }

        /// <summary>
        /// Gets the first targeting instruction matching the give type as GeometryType.
        /// </summary>
        /// <param name="geometryType">Targeting instruction search parameter. GeometryType to look for. Default use case is GeometryType.Objects</param>
        /// <returns></returns>
        public TargetingInstruction GetTargetingInstructionOfType(GeometryType geometryType)
        {
            TargetingInstruction instructionToReturn = null;
            foreach (var instruction in TargetingInstructions)
            {
                if ((int) instruction.GeometryType == (int) geometryType)
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
        /// <returns>New list of sorted targets </returns>
        public NativeSlice<GeometryDataModels.Target> GetClosestTargets()
        {
            closestTargetsContainer = GetTargetsForGameObjectsAndEntities(closestTargetsContainer);
            var sortJob = new SortTargets
            {
                newClosestTargets = closestTargetsContainer, 
                favorDistanceToCameraInsteadDistanceToPointer = false,
                comparer = new GeometryVisionUtilities.DistanceComparer()
            };
            sortJob.Schedule().Complete();
            closestTargetsContainer = sortJob.newClosestTargets;
            closestTargets = new NativeSlice<GeometryDataModels.Target>(closestTargetsContainer,0,amountOfTargets);
            return closestTargets;
        }

        /// <summary>
        /// Updates components internal targets list for both entities and GameObject. Then sorts them.
        /// It has currently hidden feature to allow target sorting on based how close the targets are to the camera.
        /// </summary>
        public void UpdateClosestTargets()
        {
            closestTargetsContainer = GetTargetsForGameObjectsAndEntities(closestTargetsContainer);
            new SortTargets
            {
                newClosestTargets = closestTargetsContainer, 
                favorDistanceToCameraInsteadDistanceToPointer = false,
                comparer = new GeometryVisionUtilities.DistanceComparer()
            }.Schedule().Complete();
            closestTargets = new NativeSlice<GeometryDataModels.Target>(closestTargetsContainer,0,amountOfTargets);
        }

        [BurstCompile]
        private struct SortTargets : IJob
        {
            public NativeArray<GeometryDataModels.Target> newClosestTargets;
            public bool favorDistanceToCameraInsteadDistanceToPointer;
            public GeometryVisionUtilities.DistanceComparer comparer;

            public void Execute()
            {
                if (newClosestTargets.Length > 0)
                {
                    if (favorDistanceToCameraInsteadDistanceToPointer == false)
                    {
                        //TODO: Native list and burst compile in job this
                        newClosestTargets.Sort(comparer);
                    }
                    else
                    {
                        //TODO: Native list and burst compile in job this
                        //TODO:After 1.0 Find a good use case where this can be used and improve it.
                        newClosestTargets.Sort(comparer);
                    }
                }
            }
        }

        /// <summary>
        /// Moves closest target with give instructions
        /// </summary>
        /// <param name="newPosition">Position to move</param>
        /// <param name="speedMultiplier">Gives extra speed</param>
        /// <param name="distanceToStop">0 value means it will travel to target. 1 value means it will stop 1 unit before reaching destination. If distance to stop is larger than distance to travel the target will not move.</param>
        public void MoveClosestTargetToPosition(Vector3 newPosition, float speedMultiplier, float distanceToStop)
        {
            var closestTarget = GetClosestTarget();
            float movementSpeed = closestTarget.distanceToCastOrigin * Time.deltaTime * speedMultiplier;

            if (closestTarget.isEntity)
            {
                MainThreadDispatcher.StartUpdateMicroCoroutine(GeometryVisionUtilities.MoveEntityTarget(newPosition,
                    movementSpeed,
                    closestTarget, distanceToStop, transformEntitySystem, entityWorld, closestTargets));
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
                    transformEntitySystem = EntityWorld.CreateSystem<TransformEntitySystem>();
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
                if (actions == null)
                {
                    continue;
                }

                if (actions.StartActionEnabled)
                {
                    MainThreadDispatcher.StartUpdateMicroCoroutine(TimedSpawnDespawn.TimedSpawnDeSpawnService(
                        actions.StartActionObject, actions.StartDelay,
                        actions.StartDuration, cachedTransform,
                        GeometryVisionSettings.NameOfStartingEffect.ToString()));
                }

                if (actions.MainActionEnabled)
                {
                    MainThreadDispatcher.StartUpdateMicroCoroutine(TimedSpawnDespawn.TimedSpawnDeSpawnService(
                        actions.MainActionObject,
                        actions.MainActionDelay, actions.MainActionDuration, cachedTransform,
                        GeometryVisionSettings.NameOfMainEffect.ToString()));
                }

                if (actions.EndActionEnabled)
                {
                    MainThreadDispatcher.StartUpdateMicroCoroutine(TimedSpawnDespawn.TimedSpawnDeSpawnService(
                        actions.EndActionObject,
                        actions.EndDelay, actions.EndDuration, cachedTransform,
                        GeometryVisionSettings.NameOfEndEffect.ToString()));
                }
            }
        }

        /// <summary>
        /// Use to get list of targets containing data from entities and gameObjects. 
        /// </summary>
        /// <returns>List of target objects that can be used to find out closest target.</returns>
        private NativeArray<GeometryDataModels.Target> GetTargetsForGameObjectsAndEntities(
            NativeArray<GeometryDataModels.Target> closestTargetsIn)
        {
            var offsetGameObjects = 0;
            var offsetEntities = 0;

            foreach (var targetingInstruction in TargetingInstructions)
            {
                if (targetingInstruction.IsTargetingEnabled.Value == false)
                {
                    continue;
                }

                var gameObjects =
                    new NativeArray<GeometryDataModels.Target>(GetGameObjectTargets(targetingInstruction).ToArray(),
                        Allocator.TempJob);
                var entities =GetEntityTargets(targetingInstruction);
                
                if (gameObjects.Length > 0)
                {
                    var job1 = new CombineEntityAndGameObjectTargets2();
                    job1.offset = 0;
                    job1.finalTargets = closestTargetsIn;
                    job1.targetsToInsert = gameObjects;
                    job1.target = new GeometryDataModels.Target();
                    var handle = job1.Schedule();
                    handle.Complete();
                }

                if (entities.Length > 0)
                {
                    var job2 = new CombineEntityAndGameObjectTargets2();
                    job2.offset = gameObjects.Length;
                    job2.finalTargets = closestTargetsIn;
                    job2.targetsToInsert = entities;
                    job2.target = new GeometryDataModels.Target();
                    var handle = job2.Schedule();
                    handle.Complete();
                }

                amountOfTargets = gameObjects.Length + entities.Length;

                gameObjects.Dispose();
                entities.Dispose();


            }

            return closestTargetsIn;
            
            //
            // Local functions//
            // 
            
            //Runs the gameObject implementation of the IGeoTargeting interface 
            List<GeometryDataModels.Target> GetGameObjectTargets(TargetingInstruction targetingInstruction)
            {
                if (gameObjectProcessing.Value == true)
                {
                    return targetingInstruction.TargetingSystemGameObjects.GetTargets(
                        transform.position,
                        ForwardWorldCoordinate, this, targetingInstruction);
                }

                return new List<GeometryDataModels.Target>();
            }

            //Runs the entity implementation of the IGeoTargeting interface 
            NativeArray<GeometryDataModels.Target> GetEntityTargets(TargetingInstruction targetingInstruction)
            {
                if (entityProcessing.Value == true)
                {
                    var closestentityTargetsIn =
                        targetingInstruction.TargetingSystemEntities.GetTargetsAsNativeArray(transform.position,
                            ForwardWorldCoordinate, this, targetingInstruction);
                    //Only update entities if the burst compiled job has finished its job OnUpdate
                    //If it has not finished it returns empty list.

                    return closestentityTargetsIn;
                }

                return new NativeArray<GeometryDataModels.Target>(0, Allocator.TempJob);
            }
        }

        [BurstCompile]
        struct CombineEntityAndGameObjectTargets2 : IJob
        {
            public NativeArray<GeometryDataModels.Target> finalTargets;
            public NativeArray<GeometryDataModels.Target> targetsToInsert;
            internal GeometryDataModels.Target target;
            public int offset;
            

            public void Execute()
            {
                for (int i = offset, j = 0; i < finalTargets.Length; i++, j++)
                {
                    if (j < targetsToInsert.Length)
                    {
                        finalTargets[i] = targetsToInsert[j];
                    }
                   
                }
            }
        }

        public void ApplyTagToTargetingInstructions(string tag)
        {
            foreach (var targetingInstruction in TargetingInstructions)
            {
                targetingInstruction.TargetTag = tag;
            }
        }

        /// <summary>
        /// Applies changes made to entity filter type by the user from outside
        /// 
        /// </summary>
        /// <remarks>It seems like the object type of script doesn't get saved during build, so it needs to be saved to serializable string to hold up the information
        /// about the script.</remarks>
        /// <param name="entityQueryFilter"></param>
        internal void ApplyEntityComponentFilterToTargetingInstructions(Object entityQueryFilter)
        {
            foreach (var targetingInstruction in targetingInstructions)
            {
                targetingInstruction.SetCurrentEntityFilterType(entityQueryFilter);
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

        public BoolReactiveProperty EntityProcessing
        {
            get { return entityProcessing; }
            set { entityProcessing = value; }
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

        public GeometryVisionRunner Runner
        {
            get { return runner; }
            set { runner = value; }
        }

        public int GetClosestTargetCount()
        {
            return closestTargets.Length;
        }

        public bool UseBounds
        {
            get { return useBounds; }
            set { useBounds = value; }
        }

        public bool CollidersTargeted { get; set; }

        public World EntityWorld
        {
            get { return entityWorld; }
            set { entityWorld = value; }
        }

        public float CheckEnvironmentChangesTimeInterval
        {
            get { return checkEnvironmentChangesTimeInterval; }
        }
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

            void DrawTargets(NativeSlice<GeometryDataModels.Target> closestTargetsIn)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

                for (var i = 0; i < closestTargetsIn.Length; i++)
                {
                    var closestTarget = closestTargetsIn[i];
                    if (closestTarget.distanceToCastOrigin == 0f)
                    {
                        continue;
                    }

                    float3 resetToVector = Vector3.zero;
                    var position = DrawTargetingVisualIndicators(closestTarget.position, transform.position,
                        closestTarget.distanceToCastOrigin, Color.blue);
                    DrawTargetingVisualIndicators(closestTarget.projectedTargetPosition, closestTarget.position,
                        closestTarget.distanceToRay, Color.green);
                    DrawTargetingVisualIndicators(position, closestTarget.projectedTargetPosition,
                        closestTarget.distanceToCastOrigin, Color.red);
                    DrawTargetingInfo(closestTarget.position, Vector3.down, i);

                    Vector3 DrawTargetingVisualIndicators(Vector3 spherePosition, float3 lineStartPosition,
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

                    void DrawTargetingInfo(float3 textLocation, float3 offset, int order)
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