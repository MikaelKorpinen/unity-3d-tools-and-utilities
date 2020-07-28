using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
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
    [DisallowMultipleComponent]
    public class GeometryVisionHead : MonoBehaviour
    {
        private HashSet<GeometryVision> geoVisions;
        private HashSet<IGeoProcessor> processors = new HashSet<IGeoProcessor>();
        public GeometryVisionMemory GeoMemory { get; } = new GeometryVisionMemory();
        public EyeDebugger EyeDebugger { get; } = new EyeDebugger();

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
            foreach (var processor in processors)
            {
                foreach (var geoVision in geoVisions)
                {
                    processor.CheckSceneChanges(geoVision);
                    if (geoVision.DebugMode)
                    {
                        processor.Debug(geoVision);
                    }
                    geoVision.RegenerateVisionArea(geoVision.FieldOfView);
                    foreach (var geoEye in geoVision.Eyes)
                    {
                        geoEye.UpdateVisibility();
                        if (geoVision.DebugMode)
                        {
                            EyeDebugger.Debug(geoEye);
                        }
                    }
                    geoVision.GetClosestTargets(GeoMemory.GeoInfos);
                }
            }
        }

        public void AddProcessor<T>(T processor)
        {
            if (processors == null)
            {
                processors = new HashSet<IGeoProcessor>();
            }

            if (InterfaceUtilities.ListContainsInterfaceImplementationOfType(processor.GetType(), processors) == false)
            {
                var dT = (IGeoProcessor) default(T);
                if (Object.Equals(processor, dT) == false)
                {
                    processors.Add((IGeoProcessor) processor);
                }
            }
        }

        public T GetProcessor<T>()
        {
            return (T) InterfaceUtilities.GetInterfaceImplementationOfTypeFromList(typeof(T), processors);
        }
        
        public void RemoveProcessor<T>()
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
        
        public void RemoveProcessors<T>()
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList(typeof(T), ref processors);
            if (typeof(T) == typeof(GeometryVisionProcessor))
            {
                var processor = GetComponent<GeometryVisionProcessor>();
                //also remove the mono behaviour from gameObject, if it is one. TODO: get the if implements monobehaviour
                //Currently there is only 2 types
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
        public HashSet<GeometryVision> GeoVisions
        {
            get { return geoVisions; }
            set { geoVisions = value; }
        }
    }
}