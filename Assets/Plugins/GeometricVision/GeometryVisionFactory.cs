using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
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
        }

        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView,
            GeometryVision geoVisionComponent, List<GeometryType> geoTypes, int layerIndex)
        {
            var geoVisionManagerGO = CreateGeometryVisionManagerGameObject();

            CreateHead(geoVisionManagerGO, geoVisionComponent);
            CreateGeometryProcessor(geoVisionManagerGO);
            HandleEyes(geoVisionManagerGO, geoVisionComponent);
            geoVisionComponent.InitUnityCamera(false);

            geoVisionManagerGO.transform.position = startingPosition;
            geoVisionManagerGO.transform.rotation = rotation;
            return geoVisionComponent.gameObject;
        }

        /// <summary>
        /// Factory method for building up Geometric vision plugin on a scene via code
        /// </summary>
        /// <remarks>By default GeometryType to use is objects since plugin needs that in order to work</remarks>
        /// <param name="startingPosition"></param>
        /// <param name="rotation"></param>
        /// <param name="fieldOfView"></param>
        /// <param name="geoTypes"></param>
        /// <param name="layerIndex"></param>
        /// <param name="debugModeEnabled"></param>
        /// <returns></returns>
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, 
            List<GeometryType> geoTypes, bool debugModeEnabled)
        {
            if (settings.edgesTargeted)
            {
                geoTypes.Add(GeometryType.Lines);
            }

            var geoVisionManagerGO = CreateGeometryVisionManagerGameObject();

            var geoVisionComponent = CreateGeoVisionComponent(new GameObject(), debugModeEnabled);
            CreateHead(geoVisionManagerGO, geoVisionComponent);
            CreateGeometryProcessor(geoVisionManagerGO);
            geoVisionComponent.InitUnityCamera(false);

            HandleEyes(geoVisionManagerGO, geoVisionComponent);

            var geometryVision = geoVisionComponent.gameObject;
            var transform = geometryVision.transform;
            transform.position = startingPosition;
            transform.rotation = rotation;

            AddAdditionalTargets(geoVisionComponent, geoTypes);

            geoVisionComponent.InitEntitySwitch();
            geoVisionComponent.InitGameObjectSwitch();
            geoVisionComponent.UpdateTargetingSystemsContainer();

            return geometryVision;
        }

        private void AddAdditionalTargets(GeometryVision geoVision, List<GeometryType> geoTypes)
        {
            geoVision.TargetingInstructions.Clear();
            var systems = MakeTargetingSystems();
            AssignTargetingSystems();
            geoVision.ValidateTargetingSystems(geoVision.TargetingInstructions);

            (IGeoTargeting, IGeoTargeting, IGeoTargeting) MakeTargetingSystems()
            {
                if (settings.processEntities == true)
                {
                    return CreateTargetingSystems(true);
                }

                if (settings.processGameObjects == true)
                {
                    return CreateTargetingSystems(false);
                }

                return (null, null, null);
            }

            (IGeoTargeting, IGeoTargeting, IGeoTargeting) CreateTargetingSystems(bool forEntities)
            {
                if (forEntities)
                {
                    var oTargetin =
                        World.DefaultGameObjectInjectionWorld.CreateSystem<GeometryEntitiesObjectTargeting>();
                    var lTargetin = World.DefaultGameObjectInjectionWorld.CreateSystem<GeometryEntitiesLineTargeting>();
                    return (oTargetin, lTargetin, null);
                }
                else
                {
                    return (new GeometryObjectTargeting(), new GeometryLineTargeting(), new GeometryVertexTargeting());
                }
            }

            void AssignTargetingSystems()
            {
                foreach (var geoType in geoTypes)
                {
                    if (geoType == GeometryType.Objects)
                    {
                        geoVision.TargetingInstructions.Add(new TargetingInstruction(GeometryType.Objects,
                            settings.defaultTag,
                            systems.Item1, settings.defaultTargeting, settings.entityComponentQueryFilter));
                    }

                    if (geoType == GeometryType.Lines)
                    {
                        geoVision.TargetingInstructions.Add(new TargetingInstruction(GeometryType.Lines,
                            settings.defaultTag, systems.Item2,
                            settings.defaultTargeting, settings.entityComponentQueryFilter));
                    }

                    if (geoType == GeometryType.Vertices)
                    {
                        geoVision.TargetingInstructions.Add(new TargetingInstruction(GeometryType.Vertices,
                            settings.defaultTag,
                            systems.Item3, settings.defaultTargeting, settings.entityComponentQueryFilter));
                    }
                }
            }
        }

        private static GameObject CreateGeometryVisionManagerGameObject()
        {
            GameObject geoVision = GameObject.Find("geoVision");
            if (geoVision == null)
            {
                geoVision = new GameObject("geoVision");
            }

            return geoVision;
        }

        private GeometryVision CreateGeoVisionComponent(GameObject geoVision, bool debugModeEnabled)
        {
            geoVision.AddComponent<GeometryVision>();
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            geoVisionComponent.ApplyDefaultTagToTargetingInstructions();
            geoVisionComponent.Id = new Hash128().ToString();
            geoVisionComponent.DebugMode = debugModeEnabled;
            ConfigureGeometryVision(this.settings, geoVisionComponent);
            return geoVisionComponent;
        }

        private void ConfigureGeometryVision(GeometryDataModels.FactorySettings factorySettings,
            GeometryVision geoVisionComponent)
        {
            geoVisionComponent.EntityBasedProcessing.Value = factorySettings.processEntities;
            geoVisionComponent.GameObjectBasedProcessing.Value = factorySettings.processGameObjects;
            geoVisionComponent.DefaultTag = settings.defaultTag;
            geoVisionComponent.InitializeGeometricVision();
        }

        private GeometryVisionRunner CreateHead(GameObject geoVisionHead, GeometryVision geoVisionComponent)
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
            return geoVisionHead.GetComponent<GeometryVisionRunner>();
        }

        internal void HandleEyes(GameObject geoVisionManager, GeometryVision geoVisionComponent)
        {
            CreateGameObjectGeoEye();
            CreateEntityGeoEye();

            void CreateGameObjectGeoEye()
            {
                if (settings.processGameObjects)
                {
                    geoVisionComponent.AddEye<GeometryVisionEye>();
                }
            }

            void CreateEntityGeoEye()
            {
                if (settings.processEntities)
                {
                    geoVisionComponent.AddEye<GeometryVisionEntityEye>();
                }
            }
        }

        private void CreateGeometryProcessor(GameObject geoVisionManager)
        {
            var head = geoVisionManager.GetComponent<GeometryVisionRunner>();
            if (settings.processGameObjects)
            {
                if (head.GetProcessor<GeometryVisionProcessor>() == null)
                {
                    if (geoVisionManager.GetComponent<GeometryVisionProcessor>() == null)
                    {
                        geoVisionManager.AddComponent<GeometryVisionProcessor>();
                    }

                    head.AddProcessor(geoVisionManager.GetComponent<GeometryVisionProcessor>());
                }
            }

            if (settings.processEntities)
            {
                if (head.GetProcessor<GeometryVisionEntityProcessor>() == null)
                {
                    var world = World.DefaultGameObjectInjectionWorld;
                    var newProcessor = world.GetOrCreateSystem<GeometryVisionEntityProcessor>();
                    head.AddProcessor(newProcessor);
                }
            }
        }
    }
}