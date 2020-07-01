using System.Collections.Generic;
using GeometricVision;
using UniRx;
using Unity.Collections;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces
{
    public interface IGeoEye
    {
        string Id { get; }
        void UpdateVisibility();
        NativeArray<GeometryDataModels.Edge> GetSeenEdges();
        GeometryVision GeoVision { get; }
        
    }
}
