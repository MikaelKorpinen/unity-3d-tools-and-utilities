using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
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
        private List<IGeoProcessor> processors;
        public GeometryVisionMemory GeoMemory { get; } = new GeometryVisionMemory();

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
                    geoVision.RegenerateVisionArea(geoVision.Camera1.fieldOfView);
                    foreach (var geoEye in geoVision.Eyes)
                    {
                        geoEye.UpdateVisibility(processor.GetAllObjects(), GeoMemory.GeoInfos);
                    }
                }
            }
        }

        public void AddProcessor<T>(T processor)
        {
            if (processors == null)
            {
                processors = new List<IGeoProcessor>();
            }

            if (InterfaceUtilities.ListContainsInterfaceOfType(processor.GetType(), processors) == false)
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
            return (T) InterfaceUtilities.GetInterfaceOfTypeFromList(typeof(T), processors);
        }

        public void RemoveProcessor<T>()
        {
            InterfaceUtilities.RemoveInterfacesOfTypeFromList(typeof(T), ref processors);
        }
        
        public HashSet<GeometryVision> GeoVisions
        {
            get { return geoVisions; }
            set { geoVisions = value; }
        }
    }
}