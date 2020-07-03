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
        void UpdateVisibility(List<Transform> objectsToUpdate, List<GeometryDataModels.GeoInfo> geoInfos);
        NativeArray<GeometryDataModels.Edge> GetSeenEdges();
        GeometryVision GeoVision { get; }
        
        
    }
}
