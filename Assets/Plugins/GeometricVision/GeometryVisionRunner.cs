using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Utilities;
using Unity.Entities;
using UnityEngine;
using Object = System.Object;

namespace Plugins.GeometricVision
{
    /// <summary>
    /// Contains all the added GeometryVision components and Processors. Gives safe and easy access to processors
    /// </summary>
    /// <remarks>
    /// Class has custom accessors for the processors. Intention is to make sure that there is only one element of each
    /// processor implementation.
    /// </remarks>
    /// TODO: Convert or create implementation using entity system base. That way the manager would not need a gameObject
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
            foreach (var processor in Processors)
            {
                foreach (var geoVision in geoVisions) //processor.GeoVisions
                {
                    time += Time.deltaTime;
                    if (time > geoVision.CheckEnvironmentChangesTimeInterval)
                    {
                        time = 0f;
                        processor.CheckSceneChanges(geoVision);
                    }
                    
                    if (geoVision.DebugMode)
                    {
                        processor.Debug(geoVision);
                    }

                    geoVision.RegenerateVisionArea(geoVision.FieldOfView);
                    foreach (var geoEye in geoVision.Eyes)
                    {

                        geoEye.UpdateVisibility(geoVision.UseBounds);
                        

                        if (geoVision.DebugMode)
                        {
                            EyeDebugger.Debug(geoEye);
                        }
                    }

                    geoVision.UpdateClosestTargets();
                }
            }
        }
        /// <summary>
        /// Adds the processor of given type for the runner.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public void AddProcessor<T>()
        {
           
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
            GeometryVisionUtilities.HandleEntityImplementationAddition(GetProcessor<T>(), processors, world, InitProcessor);
            
            void InitProcessor()
            {
              //Initialisation variable setup here  
            }
        }
    }
}