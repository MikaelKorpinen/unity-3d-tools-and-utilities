using System.Collections.Generic;
using GeometricVision;
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
        void UpdateVisibility();
        NativeArray<GeometryDataModels.Edge> GetSeenEdges();
        
    }
}