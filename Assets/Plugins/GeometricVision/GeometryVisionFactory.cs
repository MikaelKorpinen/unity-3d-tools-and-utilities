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
        public  GeometryVisionFactory(GeometryDataModels.FactorySettings settings)
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
            CreateEye(geoVisionManagerGO, geoVisionComponent);
            geoVisionComponent.InitUnityCamera();
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
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView,
            List<GeometryType> geoTypes,  bool debugModeEnabled)
        {
            if (settings.edgesTargeted)
            {
                geoTypes.Add(GeometryType.Lines);
            }
            var geoVisionManagerGO = CreateGeometryVisionManagerGameObject();
            
            var geoVisionComponent = CreateGeoVisionComponent(new GameObject(), debugModeEnabled);
            CreateHead(geoVisionManagerGO, geoVisionComponent);
            CreateGeometryProcessor(geoVisionManagerGO);
            geoVisionComponent.InitUnityCamera();

            CreateEye(geoVisionManagerGO, geoVisionComponent);
            
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
            var systems = GetTargetingSystems();
            AssignTargetingSystems();
            geoVision.ValidateTargetingSystems(geoVision.TargetingInstructions);
            (IGeoTargeting, IGeoTargeting, IGeoTargeting) GetTargetingSystems()
            {
                if (settings.processEntities == true)
                {
                    return CreateTargetingSystems(true);
                }
                
                if (settings.processGameObjects == true )
                {
                    return CreateTargetingSystems(false);
                }

                return (null, null, null);
            }
            
            (IGeoTargeting, IGeoTargeting, IGeoTargeting) CreateTargetingSystems(bool forEntities)
            {
                if (forEntities)
                {
                    var oTargetinbg = World.DefaultGameObjectInjectionWorld.CreateSystem<GeometryEntitiesObjectTargeting>();
                    var lTargetinbg = World.DefaultGameObjectInjectionWorld.CreateSystem<GeometryEntitiesLineTargeting>();
                    return ( oTargetinbg,  lTargetinbg, null);
                }
                else
                {
                    return ( new GeometryObjectTargeting(),  new GeometryLineTargeting(),  new GeometryVertexTargeting() );
                }
            }
            
            void AssignTargetingSystems()
            {
                foreach (var geoType in geoTypes)
                {
                    if (geoType == GeometryType.Objects)
                    {
                        geoVision.TargetingInstructions.Add(new TargetingInstruction(GeometryType.Objects, settings.defaultTag,
                            systems.Item1, settings.defaultTargeting, settings.entityComponentQueryFilter));
                    }

                    if (geoType == GeometryType.Lines)
                    {
                        geoVision.TargetingInstructions.Add(new TargetingInstruction(GeometryType.Lines, settings.defaultTag, systems.Item2,
                            settings.defaultTargeting, settings.entityComponentQueryFilter));
                    }

                    if (geoType == GeometryType.Vertices)
                    {
                        geoVision.TargetingInstructions.Add(new TargetingInstruction(GeometryType.Vertices, settings.defaultTag,
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

        private void ConfigureGeometryVision(GeometryDataModels.FactorySettings factorySettings, GeometryVision geoVisionComponent)
        {
            geoVisionComponent.EntityBasedProcessing.Value = factorySettings.processEntities;
            geoVisionComponent.GameObjectBasedProcessing.Value = factorySettings.processGameObjects;
            geoVisionComponent.DefaultTag = settings.defaultTag;
            geoVisionComponent.InitializeSystems();
        }

        private GeometryVisionHead CreateHead(GameObject geoVisionHead, GeometryVision geoVisionComponent)
        {
            if (geoVisionHead.GetComponent<GeometryVisionHead>() == null)
            {
                geoVisionHead.AddComponent<GeometryVisionHead>();
            }

            var head = geoVisionHead.GetComponent<GeometryVisionHead>();
            if (head.GeoVisions == null)
            {
                head.GeoVisions = new HashSet<GeometryVision>();  
            }
            head.GeoVisions.Add(geoVisionComponent);
            geoVisionComponent.Head = head;
            return geoVisionHead.GetComponent<GeometryVisionHead>();
        }

        internal void CreateEye(GameObject geoVisionManager,  GeometryVision geoVisionComponent)
        {
            if (settings.processGameObjects)
            {
                var eye = geoVisionComponent.GetEye<GeometryVisionEye>();
                if (eye == null)
                {
                    if (geoVisionComponent.GetEye<GeometryVisionEye>() == null)
                    {
                        if (geoVisionComponent.gameObject.GetComponent<GeometryVisionEye>() == null)
                        {
                            geoVisionComponent.gameObject.AddComponent<GeometryVisionEye>();
                        }

                        geoVisionComponent.Eyes.Add(geoVisionComponent.gameObject.GetComponent<GeometryVisionEye>());
                        eye = geoVisionComponent.GetEye<GeometryVisionEye>();
                        eye.Head = geoVisionManager.GetComponent<GeometryVisionHead>();
                        eye.Id = new Hash128().ToString();
                        eye.GeoVision = geoVisionComponent;
                    }
                }
            }
            if (settings.processEntities)
            {
                var eye = geoVisionComponent.GetEye<GeometryVisionEntityEye>();
                if (eye == null)
                {
                    geoVisionComponent.AddEye<GeometryVisionEntityEye>();
                    eye = geoVisionComponent.GetEye<GeometryVisionEntityEye>();
                    eye.Head = geoVisionManager.GetComponent<GeometryVisionHead>();
                    eye.Id = new Hash128().ToString();
                    eye.GeoVision = geoVisionComponent;
                }
            }
        }

        private void CreateGeometryProcessor(GameObject geoVisionManager)
        {
            var head = geoVisionManager.GetComponent<GeometryVisionHead>();
            if (settings.processGameObjects)
            {
                if (head.GetProcessor<GeometryVisionProcessor>()== null)
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