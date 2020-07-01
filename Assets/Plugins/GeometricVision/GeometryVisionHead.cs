using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using UnityEngine;

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
        private List<IGeoProcessor> processors;
        private List<IGeoEye> eyes = new List<IGeoEye>();
        
        void Reset()
        {
            processors = new List<IGeoProcessor>();
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

                }
            }
        }

        public void AddProcessor<T>(T processor)
        {
            if (processors == null)
            {
                processors  = new List<IGeoProcessor>();
            }
            if (InterfaceUtilities.ListContainsInterfaceOfType(processor.GetType(), processors) == false)
            {
                processors.Add((IGeoProcessor) processor);
            }
        }

        public void AddProcessor<T>() where T : new()
        {
            if (InterfaceUtilities.ListContainsInterfaceOfType(typeof(T), processors) == false)
            {
                IGeoProcessor newProcessor = new T() as IGeoProcessor;
                processors.Add(newProcessor);
            }
        }

        public T GetProcessor<T>()
        {
            return (T) InterfaceUtilities.GetInterfaceOfTypeFromList(typeof(T), processors);
        }

        public void RemoveProcessor<T>()
        {
            InterfaceUtilities.RemoveInterfacesOfTypeFromList(typeof(T), ref processors);
        }

        public List<IGeoEye> Eyes
        {
            get { return eyes; }
            set { eyes = value; }
        }

        public HashSet<GeometryVision> GeoVisions
        {
            get { return geoVisions; }
            set { geoVisions = value; }
        }
    }
}