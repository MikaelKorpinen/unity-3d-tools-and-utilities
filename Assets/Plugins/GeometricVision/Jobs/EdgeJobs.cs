using Plugins.GeometricVision;
using Plugins.GeometricVision.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Plugins.GeometricVision.GeometryDataModels.Boolean;

namespace GeometricVision.Jobs
{
    [BurstCompile]
    public struct ShowEdgesInsideFrustum : IJobParallelFor
    {
        public NativeArray<GeometryDataModels.Edge> Edges;
        private GeometryDataModels.Edge edge;
        [System.ComponentModel.ReadOnly(true)] public NativeArray<UnityEngine.Plane> planes;

        public ShowEdgesInsideFrustum(NativeArray<GeometryDataModels.Edge> edges, GeometryDataModels.Edge edge, UnityEngine.Plane[] planes)
        {
            this.Edges = edges;
            this.planes = JobUtilities.CopyDatafromArrayToNative(planes, 0, planes.Length, Allocator.TempJob);
            
            this.edge = edge;
        }
        
        public void Execute(int index)
        {
            if (MeshUtilities.IsInsideFrustum(Edges[index], planes))
            {
                edge = Edges[index];
                edge.isVisible = True;
                Edges[index] = edge;
            }
            else
            {
                edge = Edges[index];
                edge.isVisible = False;
                Edges[index] = edge;
            }
        }
    }
}