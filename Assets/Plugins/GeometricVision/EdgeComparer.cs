using System.Collections.Generic;
using UnityEngine;

namespace Plugins.GeometricVision
{
    public class EdgeComparer : IEqualityComparer<GeometryDataModels.Edge>
    {
        public bool Equals(GeometryDataModels.Edge x, GeometryDataModels.Edge y)
        {
            var distance1 = Vector3.Distance(x.firstVertex, y.firstVertex);
            var distance2 = Vector3.Distance(x.secondVertex, y.secondVertex);
        
            if (distance1 < 0.1f && distance2 < 0.1f)
            {
                return true;
            }
            var distance3 = Vector3.Distance(x.firstVertex, y.secondVertex);
            var distance4 = Vector3.Distance(x.secondVertex, y.firstVertex);
        
            if (distance3 < 0.1f && distance4 < 0.1f)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(GeometryDataModels.Edge edge)
        {
            int firstIndexHashCode = edge.firstEdgePointIndex.GetHashCode();
            int secondIndexHashCode = edge.secondEdgePointIndex.GetHashCode();
            return firstIndexHashCode ^ secondIndexHashCode;
        }
    }
}