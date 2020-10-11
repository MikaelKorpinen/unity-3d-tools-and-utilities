using System.Collections.Generic;
using UniRx;
using Unity.Collections;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces
{
    /// <summary>
    /// Responsible for seeing objects and geometry inside the geoVision.
    /// It checks, if object is inside visibility zone and filters out unwanted objects and geometry.
    ///
    /// Usage: Automatic. Default implementation of this interface is added automatically after user adds GeometryVision component from the inspector UI.
    /// Implementation is also switched according to user decisions. See GeometricVision.cs
    /// </summary>
    public interface IGeoEye
    {
        string Id { get; set; }
        GeometryVisionRunner Runner { get; set; }
        GeometryVision GeoVision { get; set; }
        
        /// <summary>
        /// Updates targets source game object or entity visibilities.
        /// Only to be used in case you need to get updated information for target
        /// searches like GetTargets or Update targets from GeometryVision component.
        /// Normally this is done automatically and only needs to be manually invoked if waiting for a frame is not an option.
        /// </summary>
        /// <param name="useBounds"></param>
        void UpdateVisibility(bool useBounds);
        
        /// <summary>
        /// Currently unavailable
        /// </summary>
        /// <returns></returns>
        NativeArray<GeometryDataModels.Edge> GetSeenEdges();
        
    }
}