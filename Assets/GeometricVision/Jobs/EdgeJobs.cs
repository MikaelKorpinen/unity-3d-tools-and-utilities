using GeometricVision.Utilities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static GeometryDataModels.Boolean;

namespace GeometricVision.Jobs
{

        public struct ShowEdgesInsideFrustum : IJobParallelFor
        {
            public NativeArray<GeometryDataModels.Edge> edges;
            private GeometryDataModels.Edge edge;
            [System.ComponentModel.ReadOnly(true)] public NativeArray<UnityEngine.Plane> planes;

            public ShowEdgesInsideFrustum(GeometryDataModels.Edge[] edges, GeometryDataModels.Edge edge, UnityEngine.Plane[] planes)
            {
                this.edges = new NativeArray<GeometryDataModels.Edge>(edges, Allocator.TempJob);
                this.planes =  new NativeArray<UnityEngine.Plane>(planes, Allocator.TempJob);
                this.edge = edge;
            }

            public void Execute()
            {
                for (var index = 0; index < edges.Length; index++)
                {
                    if (MeshUtilities.IsInsideFrustum(edges[index], planes))
                    {
                        edge = edges[index];
                        edge.isVisible = True;
                        edges[index] = edge;
                    }
                    else
                    {
                        edge = edges[index];
                        edge.isVisible = False;
                        edges[index] = edge;
                    }
                }
            }

            public void Execute(int index)
            {
                if (MeshUtilities.IsInsideFrustum(edges[index], planes))
                    {
                        edge = edges[index];
                        edge.isVisible = True;
                        edges[index] = edge;
                    }
                    else
                    {
                        edge = edges[index];
                        edge.isVisible = False;
                        edges[index] = edge;
                    }
                }
            }
        }