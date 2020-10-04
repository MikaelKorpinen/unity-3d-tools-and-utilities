using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
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
            geoVisionComponent.EntityBasedProcessing.Value = settings.processEntities;
            geoVisionComponent.GameObjectBasedProcessing.Value = settings.processGameObjects;
            geoVisionComponent.InitializeGeometricVision(geoTypes); //Runs the init again but parametrized
            geoVisionComponent.ApplyTagToTargetingInstructions(settings.defaultTag);
            geoVisionComponent.ApplyEntityComponentFilterToTargetingInstructions(settings.entityComponentQueryFilter);
            geoVisionComponent.ApplyActionsTemplateObject(settings.actionsTemplateObject);
            geoVisionComponent.Id = new Hash128();
            geoVisionComponent.DebugMode = debugModeEnabled;


            return geoVisionComponent;
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