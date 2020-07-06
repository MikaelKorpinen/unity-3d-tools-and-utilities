﻿using System;
using System.Collections.Generic;
using GeometricVision;
using GeometricVision.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using static Plugins.GeometricVision.GeometryDataModels.Boolean;

namespace Plugins.GeometricVision.Utilities
{
    public class MeshUtilities
    {
        private static GeometryDataModels.Edge _edge = new GeometryDataModels.Edge();
        private static UnityEngine.Plane plane = new UnityEngine.Plane();
        
        public static GeometryDataModels.Edge[] GetEdgesFromMesh(Renderer renderer, Mesh mesh)
        {
            if (!renderer)
            {
                return new GeometryDataModels.Edge[0];
            }

            var localToWorldMatrix = renderer.transform.localToWorldMatrix;
            var vertices = mesh.vertices;
            var indices = mesh.triangles;

            List<GeometryDataModels.Edge> edges = BuildEdges(indices, vertices, localToWorldMatrix);

            return edges.ToArray();
        }
        
        internal static NativeArray<GeometryDataModels.Edge> BuildEdgesFromNativeArrays(Matrix4x4 localToWorldMatrix, DynamicBuffer<TrianglesBuffer> indices, DynamicBuffer<VerticesBuffer> vertices)
        {
            Vector3 v1 = Vector3.zero;
            int t1Index;
            Vector3 v2 = Vector3.zero;
            int t2Index;
            Vector3 v3 = Vector3.zero;
            int t3Index;
            NativeArray<GeometryDataModels.Edge> edges = new NativeArray<GeometryDataModels.Edge>(indices.Length*3, Allocator.TempJob);
            int triangleCount = indices.Length;

            for (int i = 0; i < triangleCount; i += 3)
            {
                v1 = vertices[indices[i]];
                t1Index = indices[i];
                v2 = vertices[indices[i + 1]];
                t2Index = indices[i + 1];
                v3 = vertices[indices[i + 2]];
                t3Index = indices[i + 2];


                var firstEdgePoint = _edge.firstVertex;
                var secondEdgePoint = _edge.secondVertex;

                var m = localToWorldMatrix;
                firstEdgePoint = m.MultiplyPoint3x4(v1);
                secondEdgePoint = m.MultiplyPoint3x4(v2);

                GeometryDataModels.Edge edge1 = new GeometryDataModels.Edge();
                
                edges = BuildEdge(m, v1, v2, t1Index, t2Index, edges, i);
                edges = BuildEdge(m, v2, v3, t2Index, t3Index, edges, i + 1);
                edges = BuildEdge(m, v3, v1, t3Index, t1Index, edges, i + 2);
            }

            return edges;
        }

        private static NativeArray<GeometryDataModels.Edge> BuildEdge(Matrix4x4 m, Vector3 v1, Vector3 v2, int t1Index, int t2Index, NativeArray<GeometryDataModels.Edge> edges,
            int i)
        {
            Vector3 firstEdgePoint;
            Vector3 secondEdgePoint;
            GeometryDataModels.Edge edge1;
            firstEdgePoint = m.MultiplyPoint3x4(v1);
            secondEdgePoint = m.MultiplyPoint3x4(v2);


            edge1 = AssignEdge(firstEdgePoint, t1Index, secondEdgePoint, t2Index);

            if (!checkIfExists(edge1, edges))
            {
                edges[i] = (edge1);
            }

            return edges;
        }

        private static GeometryDataModels.Edge AssignEdge(Vector3 firstEdgePoint, int t1Index, Vector3 secondEdgePoint, int t2Index)
        {
            GeometryDataModels.Edge edge1 = new GeometryDataModels.Edge();
            edge1.firstVertex = firstEdgePoint;
            edge1.firstEdgePointIndex = t1Index;
            edge1.secondVertex = secondEdgePoint;
            edge1.secondEdgePointIndex = t2Index;
            return edge1;
        }

        private static List<GeometryDataModels.Edge> BuildEdges(int[] indices, Vector3[] vertices,
            Matrix4x4 localToWorldMatrix)
        {
            Vector3 v1 = Vector3.zero;
            int t1Index;
            Vector3 v2 = Vector3.zero;
            int t2Index;
            Vector3 v3 = Vector3.zero;
            int t3Index;
            List<GeometryDataModels.Edge> edges = new List<GeometryDataModels.Edge>();
            int triangleCount = indices.Length;

            for (int i = 0; i < triangleCount; i += 3)
            {
                v1 = vertices[indices[i]];
                t1Index = indices[i];
                v2 = vertices[indices[i + 1]];
                t2Index = indices[i + 1];
                v3 = vertices[indices[i + 2]];
                t3Index = indices[i + 2];


                var firstEdgePoint = _edge.firstVertex;
                var secondEdgePoint = _edge.secondVertex;

                var m = localToWorldMatrix;
                firstEdgePoint = m.MultiplyPoint3x4(v1);
                secondEdgePoint = m.MultiplyPoint3x4(v2);

                GeometryDataModels.Edge edge1 = new GeometryDataModels.Edge();

                edge1.firstVertex = firstEdgePoint;
                edge1.firstEdgePointIndex = t1Index;
                edge1.secondVertex = secondEdgePoint;
                edge1.secondEdgePointIndex = t2Index;

                if (!checkIfExists(edge1, edges))
                {
                    edges.Add(edge1);
                }

                firstEdgePoint = m.MultiplyPoint3x4(v2);
                secondEdgePoint = m.MultiplyPoint3x4(v3);

                GeometryDataModels.Edge edge2 = new GeometryDataModels.Edge();

                edge2.firstVertex = firstEdgePoint;
                edge2.firstEdgePointIndex = t2Index;
                edge2.secondVertex = secondEdgePoint;
                edge2.secondEdgePointIndex = t3Index;

                if (!checkIfExists(edge2, edges))
                {
                    edges.Add(edge2);
                }

                firstEdgePoint = m.MultiplyPoint3x4(v3);
                secondEdgePoint = m.MultiplyPoint3x4(v1);
                GeometryDataModels.Edge edge3 = new GeometryDataModels.Edge();

                edge3.firstVertex = firstEdgePoint;
                edge3.firstEdgePointIndex = t3Index;
                edge3.secondVertex = secondEdgePoint;
                edge3.secondEdgePointIndex = t1Index;

                if (!checkIfExists(edge3, edges))
                {
                    edges.Add(edge3);
                }
            }

            return edges;
        }


        public static void UpdateEdgesVisibility(UnityEngine.Plane[] planes, List<GeometryDataModels.GeoInfo> seenGeometry)
        {
            for (var index = 0; index < seenGeometry.Count; index++)
            {
                var geoInfo = seenGeometry[index];

                geoInfo.edges = ShowEdgesInsideFrustum(geoInfo.edges, planes);

                seenGeometry[index] = geoInfo;

            }
        }
        
        public static GeometryDataModels.Edge[] ShowEdgesInsideFrustum(GeometryDataModels.Edge[] edges, UnityEngine.Plane[] planes)
        {
            GeometryDataModels.Edge  edge = new GeometryDataModels.Edge();
            for (var index = 0; index < edges.Length; index++)
            {
                edge = edges[index];
                if (IsInsideFrustum(edges[index], planes))
                {
                    edge.isVisible = True;
                }
                else
                {
                    edge.isVisible = False;
                }

                edges[index] = edge;
            }

            return edges;
        }

        public static void UpdateEdgesVisibilityParallel(UnityEngine.Plane[] planes, List<GeometryDataModels.GeoInfo> geometries)
        {
            var edge = new GeometryDataModels.Edge();
            for (var index = 0; index < geometries.Count; index++)
            {
                var geoInfo = geometries[index];
                
                ShowEdgesInsideFrustum job = new ShowEdgesInsideFrustum(new NativeArray<GeometryDataModels.Edge>(geoInfo.edges.Length, Allocator.TempJob), edge, planes);
                job.Edges.CopyFrom(geoInfo.edges);
               
                var handle = job.Schedule(geoInfo.edges.Length, 64);
                
                handle.Complete();
                job.Edges.CopyTo(geoInfo.edges);
                geometries[index] = geoInfo;
                job.Edges.Dispose();
                job.planes.Dispose();
            }
        }
        
        private static void RemoveAt(ref GeometryDataModels.Edge[] edges, int indexToRemove)
        {
            for (int i = indexToRemove; i < edges.Length - 1; i++)
            {
                edges[i] = edges[i + 1];
            }

            Array.Resize(ref edges, edges.Length - 1);
        }

        public static bool IsInsideFrustum(GeometryDataModels.Edge edge, UnityEngine.Plane[] planes)
        {
            for (var index = 0; index < planes.Length; index++)
            {
                plane = planes[index];
                if (plane.GetDistanceToPoint(edge.firstVertex) < 0 || plane.GetDistanceToPoint(edge.secondVertex) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInsideFrustum(GeometryDataModels.Edge edge, NativeArray<UnityEngine.Plane> planes)
        {
            for (var i = 0; i < planes.Length; i++)
            {
                if (planes[i].GetDistanceToPoint(edge.firstVertex) < 0 || planes[i].GetDistanceToPoint(edge.secondVertex) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInsideFrustum(Vector3 point, UnityEngine.Plane[] planes)
        {
            foreach (var plane in planes)
            {
                if (plane.GetDistanceToPoint(point) < 0)
                {
                    return false;
                }
            }

            return true;
        }
        static bool checkIfExists(GeometryDataModels.Edge edge, List<GeometryDataModels.Edge> edges)
        {
            bool found = false;
            foreach (var edge1 in edges)
            {
                if (Equals(edge1, edge))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }
        static bool checkIfExists(GeometryDataModels.Edge edge, NativeArray<GeometryDataModels.Edge> edges)
        {
            bool found = false;
            for (var index = 0; index < edges.Length; index++)
            {
                var edge1 = edges[index];
                if (Equals(edge1, edge))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }
        static bool Equals(GeometryDataModels.Edge x, GeometryDataModels.Edge y)
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


    }
}