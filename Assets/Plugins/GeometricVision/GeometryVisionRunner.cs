using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Debugging;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Utilities;
using Unity.Entities;
using UnityEngine;
using Object = System.Object;

namespace Plugins.GeometricVision
{
    /// <summary>
    /// Contains all the added GeometryVision components and Processors.
    /// Code is run every frame updating all the required sub systems that makes getting targets possible.
    /// </summary>
    [DisallowMultipleComponent]
    public class GeometryVisionRunner : MonoBehaviour
    {
        private HashSet<GeometryVision> geoVisions;
        private HashSet<IGeoProcessor> processors = new HashSet<IGeoProcessor>();
        internal GeometryVisionMemory GeoMemory { get; } = new GeometryVisionMemory();

        private EyeDebugger EyeDebugger { get; } = new EyeDebugger();
        private float time = 0f;

        private void Awake()
        {
            processors = new HashSet<IGeoProcessor>();
        }

        void Reset()
        {
            processors = new HashSet<IGeoProcessor>();
        }

        private void Update()
        {
            time += Time.deltaTime;

            foreach (var geoVision in geoVisions)
            {
                if (time <= geoVision.CheckEnvironmentChangesTimeInterval)
                {
                    return;
                }
                time = 0f;
                
                ProcessorCheckSceneChanges(geoVision);
                geoVision.RegenerateVisionArea(geoVision.FieldOfView);
                UpdateEntityOrGameObjectVisibilities(geoVision);
                geoVision.UpdateClosestTargets(true, true);
            }
        }

        void ProcessorCheckSceneChanges(GeometryVision geoVision)
        {
            foreach (var processor in Processors)
            {
                processor.CheckSceneChanges(geoVision);
            }
        }

        void UpdateEntityOrGameObjectVisibilities(GeometryVision geoVision)
        {
            foreach (var geoEye in geoVision.Eyes)
            {
                geoEye.UpdateVisibility(geoVision.UseBounds);

                if (geoVision.DebugMode)
                {
                    EyeDebugger.Debug(geoEye);
                }
            }
        }


        /// <summary>
        /// Gets the processor of given type from the runner.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetProcessor<T>()
        {
            return (T) InterfaceUtilities.GetInterfaceImplementationOfTypeFromList(typeof(T), Processors);
        }

        public void RemoveGameObjectProcessor<T>()
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList(typeof(T), ref processors);
            if (typeof(T) == typeof(GeometryVisionProcessor))
            {
                var processor = GetComponent<GeometryVisionProcessor>();
                //also remove the mono behaviour from gameObject, if it is one. TODO: get the if implements monobehaviour
                //Currently there is only 2 types. Other one is MonoBehaviour and the other one not
                if (Application.isPlaying && processor != null)
                {
                    Destroy(processor);
                }
                else if (Application.isPlaying == false && processor != null)
                {
                    DestroyImmediate(processor);
                }
            }
        }

        public void RemoveEntityProcessor<T>() where T : ComponentSystemBase
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList(typeof(T), ref processors);
            foreach (GeometryVision geoVision in geoVisions)
            {
                var system = geoVision.EntityWorld.GetExistingSystem<T>();

                if (system != null)
                {
                    geoVision.EntityWorld.DestroySystem(system);
                }
            }
        }

        public HashSet<GeometryVision> GeoVisions
        {
            get { return geoVisions; }

            set { geoVisions = value; }
        }

        public HashSet<IGeoProcessor> Processors
        {
            get { return processors; }
        }

        public void AddEntityProcessor<T>(World world) where T : ComponentSystemBase, IGeoProcessor, new()
        {
            GeometryVisionUtilities.HandleEntityImplementationAddition(GetProcessor<T>(), processors, world,
                InitProcessor);

            void InitProcessor()
            {
                //Initialisation variable setup here  in case needed
            }
        }
    }
}