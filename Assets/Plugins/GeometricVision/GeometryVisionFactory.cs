using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.EntityScripts;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using UniRx;
using Unity.Entities;
using UnityEngine;
using Hash128 = UnityEngine.Hash128;

namespace Plugins.GeometricVision
{
    public class GeometryVisionFactory
    {
        private readonly GeometryDataModels.FactorySettings settings;

        public GeometryVisionFactory(GeometryDataModels.FactorySettings settings)
        {
            this.settings = settings;
            this.settings.defaultTag = settings.defaultTag;
            this.settings.entityComponentQueryFilter = settings.entityComponentQueryFilter;
            this.settings.processGameObjects = settings.processGameObjects;
            this.settings.processEntities = settings.processEntities;
        }

        public GeometryVisionFactory()
        {
        }

        public GeometryDataModels.FactorySettings Settings
        {
            get { return settings; }
        }

        public void CreateGeometryVisionRunner(GeometryVision geoVisionComponent)
        {
            var geoVisionRunnerGo = CreateGeometryVisionRunnerGameObject();
            CreateGeometryVisionRunner(geoVisionRunnerGo, geoVisionComponent);
        }

        /// <summary>
        /// Factory method for building up Geometric vision plugin on a scene via code
        /// </summary>
        /// <remarks>By default GeometryType to use is objects since plugin needs that in order to work</remarks>
        /// <param name="startingPosition"></param>
        /// <param name="rotation"></param>
        /// <param name="geoTypes"></param>
        /// <param name="debugModeEnabled"></param>
        /// <returns></returns>
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation,
            List<GeometryType> geoTypes, bool debugModeEnabled)
        {
            var geoVisionManagerGO = CreateGeometryVisionRunnerGameObject();
            var geoVisionComponent = CreateGeoVisionComponent(new GameObject(), debugModeEnabled, geoTypes);
            CreateGeometryVisionRunner(geoVisionManagerGO, geoVisionComponent);

            var geometryVision = geoVisionComponent.gameObject;
            var transform = geometryVision.transform;
            transform.position = startingPosition;
            transform.rotation = rotation;

            return geometryVision;
        }

        internal void AddAdditionalTargets(GeometryVision geoVision, List<GeometryType> geoTypes)
        {
            var targetingSystems = MakeTargetingSystems();

            AssignTargetingInstructions();

            //Make all currently supported targeting systems and return them in a tuple like object

            void AssignTargetingInstructions()
            {
                foreach (var geoType in geoTypes)
                {
                    if (geoType == GeometryType.Objects)
                    {
                        var targetingInstruction = new TargetingInstruction(GeometryType.Objects, settings.defaultTag,
                            (targetingSystems.Item4, targetingSystems.Item1), true, null);
                        geoVision.TargetingInstructions.Add(targetingInstruction);
                    }

                    if (geoType == GeometryType.Lines)
                    {
                        var targetingInstruction = new TargetingInstruction(GeometryType.Lines, settings.defaultTag,
                            (targetingSystems.Item5, targetingSystems.Item2), true, null);
                        geoVision.TargetingInstructions.Add(targetingInstruction);
                    }

                    if (geoType == GeometryType.Vertices)
                    {
                        var targetingInstruction = new TargetingInstruction(GeometryType.Vertices, settings.defaultTag,
                            (targetingSystems.Item6, targetingSystems.Item3), true, null);
                        geoVision.TargetingInstructions.Add(targetingInstruction);
                    }
                }
            }
        }
        //Local function tha makes all the possible targeting systems
        (IGeoTargeting, IGeoTargeting, IGeoTargeting, IGeoTargeting, IGeoTargeting, IGeoTargeting )
            MakeTargetingSystems()
        {
            GeometryEntitiesObjectTargeting objectTargeting = null;
            GeometryEntitiesLineTargeting lineTargeting = null;
            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                objectTargeting = world.CreateSystem<GeometryEntitiesObjectTargeting>();
                lineTargeting = world.CreateSystem<GeometryEntitiesLineTargeting>();
            }

            return (objectTargeting, lineTargeting, null, new GeometryObjectTargeting(), new GeometryLineTargeting(),
                new GeometryVertexTargeting());
        }

        private static GameObject CreateGeometryVisionRunnerGameObject()
        {
            GameObject geoVision = GameObject.Find(GeometryVisionSettings.RunnerName);
            if (geoVision == null)
            {
                geoVision = new GameObject(GeometryVisionSettings.RunnerName);
            }

            return geoVision;
        }

        private GeometryVision CreateGeoVisionComponent(GameObject geoVision, bool debugModeEnabled,
            List<GeometryType> geoTypes)
        {
            geoVision.AddComponent<GeometryVision>();
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();

            geoVisionComponent.InitializeGeometricVision(geoTypes); //Runs the init again but parametrized
            if (settings.fielOfView != 0)
            {
                geoVisionComponent.FieldOfView = settings.fielOfView;
            }

            geoVisionComponent.EntityProcessing.Value = settings.processEntities;
            geoVisionComponent.GameObjectBasedProcessing.Value = settings.processGameObjects;
            geoVisionComponent.ApplyTagToTargetingInstructions(settings.defaultTag);
            geoVisionComponent.ApplyEntityComponentFilterToTargetingInstructions(settings.entityComponentQueryFilter);
            geoVisionComponent.ApplyActionsTemplateObject(settings.actionsTemplateObject);
            geoVisionComponent.Id = new Hash128();
            geoVisionComponent.DebugMode = debugModeEnabled;


            return geoVisionComponent;
        }

        /// <summary>
        /// Handles all the required operation for GeometricVision to work with entities.
        /// Such as adding GeometricVision eye for seeing, processor for the data and a targeting system.
        /// The functionality is subscribed to a toggle on the inspector GUI.
        /// </summary>
        /// <param name="entityBasedProcessing"></param>
        /// <param name="entityToggleObservable"></param>
        /// <param name="geoVision"></param>
        /// <param name="transformEntitySystem"></param>
        /// <remarks>        
        /// Because of some differences on the entity system the entities functionality can only be enabled easily
        /// when the application is running.
        /// </remarks>
        internal void InitEntitySwitch(BoolReactiveProperty entityBasedProcessing, IDisposable entityToggleObservable, GeometryVision geoVision, TransformEntitySystem transformEntitySystem)
        {
            ObserveButton(entityBasedProcessing, entityToggleObservable, geoVision);
            
            //Local functions
            
            //Observe changes in button and react to that
            void ObserveButton(BoolReactiveProperty boolReactiveProperty, IDisposable disposable, GeometryVision geometryVision)
            {
                if (disposable == null)
                {
                    //Add initialization behaviour on the inspector toggle button
                    disposable = boolReactiveProperty.Subscribe(entitiesEnabled =>
                    {
                        InitEntities();

                        void InitEntities()
                        {
                            geometryVision.EntityWorld = World.DefaultGameObjectInjectionWorld;
                            //Handle toggle button use cases
                            IfEntitiesEnabled(entitiesEnabled);
                            IfEntitiesDisabled(entitiesEnabled);
                        }
                    });
                }
            }

            //Handles toggle button enabled use case
            void IfEntitiesEnabled(bool entitiesEnabled)
            {
                if (Application.isPlaying && entitiesEnabled)
                {
                    geoVision.Runner.AddEntityProcessor<GeometryVisionEntityProcessor>(geoVision.EntityWorld);
                    InitGeometryEyeForEntities(geoVision.EntityWorld);

                    IGeoTargeting targetingSystem =
                        geoVision.EntityWorld.GetOrCreateSystem<GeometryEntitiesObjectTargeting>();
                    IfNoDefaultTargetingAddOne(geoVision, targetingSystem);
                }
            }

            //Handles toggle button disabled use case
            void IfEntitiesDisabled(bool entitiesEnabled)
            {
                if (Application.isPlaying && entitiesEnabled == false)
                {
                    geoVision.Runner.RemoveEntityProcessor<GeometryVisionEntityProcessor>();
                    geoVision.RemoveEntityEye<GeometryVisionEntityEye>();

                    if (geoVision.EntityWorld.GetExistingSystem<TransformEntitySystem>() != null)
                    {
                        geoVision.EntityWorld.DestroySystem(transformEntitySystem);
                    }

                    transformEntitySystem = null;

                    foreach (var targetinginstruction in geoVision.TargetingInstructions)
                    {
                        if (targetinginstruction.TargetingSystemEntities != null)
                        {
                            geoVision.EntityWorld.DestroySystem(
                                (ComponentSystemBase) targetinginstruction.TargetingSystemEntities);
                            targetinginstruction.TargetingSystemEntities = null;
                        }
                    }
                    
                    geoVision.UpdateClosestTargets( false, true);
                }
            }
            
            void InitGeometryEyeForEntities(World world)
            {
                geoVision.RemoveEntityEye<GeometryVisionEntityEye>();
                GeometryVisionEntityEye eEey = world.CreateSystem<GeometryVisionEntityEye>();

                InterfaceUtilities.AddImplementation(eEey, geoVision.Eyes);
                var eye = geoVision.GetEye<GeometryVisionEntityEye>();
                eye.GeoVision = geoVision;
                eye.TargetingInstructions = geoVision.TargetingInstructions;
                eye.Update();
            }

        }

        
        /// <summary>
        /// Handles all the required operation for GeometricVision to work with game objects.
        /// Such as GeometricVision eye/camera and processor for the data.
        /// The functionality is subscribed to a toggle that exists in the inspector GUI
        /// </summary>
        internal void InitGameObjectSwitch( IDisposable gameObjectButtonObservable, BoolReactiveProperty gameObjectProcessing, GeometryVision geoVision)
        {
            ObserveButtonChanges();

            void ObserveButtonChanges()
            {
                if (gameObjectButtonObservable == null)
                {
                    gameObjectButtonObservable = gameObjectProcessing.Subscribe(InitGameObjectBasedSystem);
                }
            }

            void InitGameObjectBasedSystem(bool gameObjectBasedProcessing)
            {
                var geoEye = geoVision.GetEye<GeometryVisionEye>();
                if (gameObjectBasedProcessing == true)
                {

                    EnableGameObjects();
                }
                if (gameObjectBasedProcessing == false)
                {
                    DisableGameObjects();
                }
                
                //Local functions
                void EnableGameObjects()
                {
                    if (geoEye == null)
                    {
                        geoVision.AddGameObjectEye<GeometryVisionEye>();
                    }

                    InitGeometryProcessorForGameObjects();
                    IfNoDefaultTargetingAddOne(geoVision, new GeometryObjectTargeting());
                }

                void DisableGameObjects()
                {
                    geoVision.Runner.RemoveGameObjectProcessor<GeometryVisionProcessor>();
                    geoVision.RemoveEye<GeometryVisionEye>();
                    geoVision.UpdateClosestTargets(true, false);
                }
            }

            void InitGeometryProcessorForGameObjects()
            {
                if (geoVision.Runner.gameObject.GetComponent<GeometryVisionProcessor>() == null)
                {
                    geoVision.Runner.gameObject.AddComponent<GeometryVisionProcessor>();
                }

                InterfaceUtilities.AddImplementation(geoVision.Runner.gameObject.GetComponent<GeometryVisionProcessor>(),
                    geoVision.Runner.Processors);
            }
        }


        // Handles target initialization. Adds needed components and subscribes changing variables to logic that updates the targeting system.
        internal List<TargetingInstruction> InitializeTargeting(List<TargetingInstruction> theTargetingInstructionsIn, GeometryVision geoVision, BoolReactiveProperty entityBasedProcessing, BoolReactiveProperty gameObjectProcessing)
        {
            var geoTargetingSystemsContainer = HandleAddingGeometryTargetingSystemsContainer();

            theTargetingInstructionsIn = geoVision.AddDefaultTargetingInstructionIfNone(theTargetingInstructionsIn,
                entityBasedProcessing.Value,
                gameObjectProcessing.Value);

            foreach (var targetingInstruction in theTargetingInstructionsIn)
            {
                AssignActionsTemplate(targetingInstruction, theTargetingInstructionsIn.IndexOf(targetingInstruction));
                OnTargetingEnabled(targetingInstruction, geoTargetingSystemsContainer, entityBasedProcessing, gameObjectProcessing);
            }

            return theTargetingInstructionsIn;

            //Local functions

            //Creates default template scriptable object that can hold actions on what to do when targeting
            void AssignActionsTemplate(TargetingInstruction targetingInstruction, int indexOf)
            {
                if (targetingInstruction.TargetingActions == null)
                {
                    var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
                    newActions.name += "_" + indexOf;
                    targetingInstruction.TargetingActions = newActions;
                }
            }

            //This container is needed so all the targeting systems can be run from a list by the runner/manager.
            GeometryTargetingSystemsContainer HandleAddingGeometryTargetingSystemsContainer()
            {
                var geoTargeting = geoVision.GetComponent<GeometryTargetingSystemsContainer>();
                if (geoVision.GetComponent<GeometryTargetingSystemsContainer>() == null)
                {
                    geoVision.gameObject.AddComponent<GeometryTargetingSystemsContainer>();
                    geoTargeting = geoVision.GetComponent<GeometryTargetingSystemsContainer>();
                }

                return geoTargeting;
            }
        }

        /// <summary>
        /// Add targeting implementation, if it is enabled on the inspector.
        /// Subscribes the targeting toggle button to functionality than handles creation of default targeting implementation for the
        /// targeted geometry type
        /// </summary>
        /// <param name="theTargetingInstructions"></param>
        /// <param name="targetingInstruction"></param>
        /// <param name="geoTargetingSystemsContainer"></param>
        private void OnTargetingEnabled(TargetingInstruction targetingInstruction, GeometryTargetingSystemsContainer geoTargetingSystemsContainer, BoolReactiveProperty entityBasedProcessing, BoolReactiveProperty gameObjectProcessing)
        {
            if (!targetingInstruction.Subscribed)
            {
                SubscribeInstructionToTargetingButton();

                void SubscribeInstructionToTargetingButton()
                {
                    targetingInstruction.IsTargetingEnabled.Subscribe(targeting =>
                    {
                        if (targeting == true)
                        {
                            AddTargetingPrograms(geoTargetingSystemsContainer);
                        }
                        else
                        {
                            RemoveDefaultTargeting(geoTargetingSystemsContainer);
                        }
                    });
                }

                targetingInstruction.Subscribed = true;
            }

            //AddDefaultTargeting for both game objects and entities
            //Default is objects
            void AddTargetingPrograms(
                GeometryTargetingSystemsContainer geometryTargetingSystemsContainer)
            {
                if (gameObjectProcessing.Value == true)
                {
                    if (targetingInstruction.TargetingSystemGameObjects != null)
                    {
                        geometryTargetingSystemsContainer.AddTargetingProgram(
                            (IGeoTargeting) targetingInstruction.TargetingSystemGameObjects);
                    }
                }

                if (entityBasedProcessing.Value == true)
                {
                    if (targetingInstruction.TargetingSystemEntities != null)
                    {
                        geometryTargetingSystemsContainer.AddTargetingProgram(
                            (IGeoTargeting) targetingInstruction.TargetingSystemEntities);
                    }
                }
            }

            //RemoveDefaultTargeting for both game objects and entities
            void RemoveDefaultTargeting(
                GeometryTargetingSystemsContainer geometryTargetingSystemsContainer)
            {
                geometryTargetingSystemsContainer.RemoveTargetingProgram(targetingInstruction
                    .TargetingSystemGameObjects);

                geometryTargetingSystemsContainer.RemoveTargetingProgram(targetingInstruction
                    .TargetingSystemEntities);
            }
        }
        
        /// <summary>
        /// Provides Needed default object targeting for the system in case there is none. Otherwise replaces one from the current users
        /// targeting instructions. 
        /// </summary>
        /// <param name="geoVision"></param>
        /// <param name="targetingSystem"></param>
        private void IfNoDefaultTargetingAddOne(GeometryVision geoVision, IGeoTargeting targetingSystem)
        {
            var targetingInstruction = geoVision.GetTargetingInstructionOfType(GeometryType.Objects);
            string defaulTag = "";
            if (targetingInstruction == null)
            {
                targetingInstruction = new TargetingInstruction(GeometryType.Objects, defaulTag,
                    (targetingSystem, null), true,
                    null);

                AssignDefaultTargetingSystem(targetingSystem);
                geoVision.TargetingInstructions.Add(targetingInstruction);
            }
            else
            {
                AssignDefaultTargetingSystem(targetingSystem);
            }

            void AssignDefaultTargetingSystem(IGeoTargeting geoTargeting)
            {
                var targetingSystemsContainer = geoVision.GetComponent<GeometryTargetingSystemsContainer>();
                if (geoTargeting.IsForEntities())
                {
                    var sys = targetingSystemsContainer.GetTargetingProgram<GeometryEntitiesObjectTargeting>();
                    if (sys != null)
                    {
                        targetingSystemsContainer.RemoveTargetingProgram(sys);
                    }
                   
                    targetingInstruction.TargetingSystemEntities = geoTargeting;
                    targetingSystemsContainer.AddTargetingProgram(targetingInstruction.TargetingSystemEntities);
                }
                else if (!geoTargeting.IsForEntities())
                {
                    var sys = targetingSystemsContainer.GetTargetingProgram<GeometryObjectTargeting>();
                    if (sys != null)
                    {
                        targetingSystemsContainer.RemoveTargetingProgram(sys);
                    }

                    targetingInstruction.TargetingSystemGameObjects = geoTargeting;
                    targetingSystemsContainer.AddTargetingProgram(targetingInstruction.TargetingSystemGameObjects);
                }
            }
        }

        private void CreateGeometryVisionRunner(GameObject geoVisionHead, GeometryVision geoVisionComponent)
        {
            if (geoVisionHead.GetComponent<GeometryVisionRunner>() == null)
            {
                geoVisionHead.AddComponent<GeometryVisionRunner>();
            }

            var runner = geoVisionHead.GetComponent<GeometryVisionRunner>();
            if (runner.GeoVisions == null)
            {
                runner.GeoVisions = new HashSet<GeometryVision>();
            }

            runner.GeoVisions.Add(geoVisionComponent);
            geoVisionComponent.Runner = runner;
        }
    }
}