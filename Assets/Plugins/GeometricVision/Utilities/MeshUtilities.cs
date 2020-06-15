﻿using System;
using System.Collections.Generic;
using GeometricVision.Jobs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static GeometricVision.GeometryDataModels.Boolean;

namespace GeometricVision.Utilities
{
    public class MeshUtilities
    {
        private static GeometryDataModels.Edge _edge = new GeometryDataModels.Edge();
        private static UnityEngine.Plane plane = new UnityEngine.Plane();
        
        public static GeometryDataModels.Edge[] GetEdgesFromMesh(Renderer renderer, Mesh mesh)
        {
            if (!renderer)
            {
                return null;
            }

            var localToWorldMatrix = renderer.transform.localToWorldMatrix;
            var vertices = mesh.vertices;
            var indices = mesh.triangles;

            var edges = BuildEdges(indices, vertices, localToWorldMatrix);

            return edges.ToArray();
        }

        private static List<GeometryDataModels.Edge> BuildEdgesOld(int[] indices, Vector3[] vertices,
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
                var x = v1.x;
                var y = v1.y;
                var z = v1.z;
                var rx = x * m[0] + y * m[4] + z * m[8] + m[12];
                var ry = x * m[1] + y * m[5] + z * m[9] + m[13];
                var rz = x * m[2] + y * m[6] + z * m[10] + m[14];
                var rw = 1 / (x * m[3] + y * m[7] + z * m[11] + m[15]);
                firstEdgePoint.x = rx * rw;
                firstEdgePoint.y = ry * rw;
                firstEdgePoint.z = rz * rw;

                x = v2.x;
                y = v2.y;
                z = v2.z;
                rx = x * m[0] + y * m[4] + z * m[8] + m[12];
                ry = x * m[1] + y * m[5] + z * m[9] + m[13];
                rz = x * m[2] + y * m[6] + z * m[10] + m[14];
                rw = 1 / (x * m[3] + y * m[7] + z * m[11] + m[15]);
                secondEdgePoint.x = rx * rw;
                secondEdgePoint.y = ry * rw;
                secondEdgePoint.z = rz * rw;
                GeometryDataModels.Edge edge1 = new GeometryDataModels.Edge();

                edge1.firstVertex = firstEdgePoint;
                edge1.firstEdgePointIndex = t1Index;
                edge1.secondVertex = secondEdgePoint;
                edge1.secondEdgePointIndex = t2Index;

                if (!checkIfExists(edge1, edges))
                {
                    edges.Add(edge1);
                }

                x = v2.x;
                y = v2.y;
                z = v2.z;
                rx = x * m[0] + y * m[4] + z * m[8] + m[12];
                ry = x * m[1] + y * m[5] + z * m[9] + m[13];
                rz = x * m[2] + y * m[6] + z * m[10] + m[14];
                rw = 1 / (x * m[3] + y * m[7] + z * m[11] + m[15]);
                firstEdgePoint.x = rx * rw;
                firstEdgePoint.y = ry * rw;
                firstEdgePoint.z = rz * rw;

                x = v3.x;
                y = v3.y;
                z = v3.z;
                rx = x * m[0] + y * m[4] + z * m[8] + m[12];
                ry = x * m[1] + y * m[5] + z * m[9] + m[13];
                rz = x * m[2] + y * m[6] + z * m[10] + m[14];
                rw = 1 / (x * m[3] + y * m[7] + z * m[11] + m[15]);
                secondEdgePoint.x = rx * rw;
                secondEdgePoint.y = ry * rw;
                secondEdgePoint.z = rz * rw;

                GeometryDataModels.Edge edge2 = new GeometryDataModels.Edge();

                edge2.firstVertex = firstEdgePoint;
                edge2.firstEdgePointIndex = t2Index;
                edge2.secondVertex = secondEdgePoint;
                edge2.secondEdgePointIndex = t3Index;

                if (!checkIfExists(edge2, edges))
                {
                    edges.Add(edge2);
                }

                x = v3.x;
                y = v3.y;
                z = v3.z;
                rx = x * m[0] + y * m[4] + z * m[8] + m[12];
                ry = x * m[1] + y * m[5] + z * m[9] + m[13];
                rz = x * m[2] + y * m[6] + z * m[10] + m[14];
                rw = 1 / (x * m[3] + y * m[7] + z * m[11] + m[15]);
                firstEdgePoint.x = rx * rw;
                firstEdgePoint.y = ry * rw;
                firstEdgePoint.z = rz * rw;

                x = v1.x;
                y = v1.y;
                z = v1.z;
                rx = x * m[0] + y * m[4] + z * m[8] + m[12];
                ry = x * m[1] + y * m[5] + z * m[9] + m[13];
                rz = x * m[2] + y * m[6] + z * m[10] + m[14];
                rw = 1 / (x * m[3] + y * m[7] + z * m[11] + m[15]);
                secondEdgePoint.x = rx * rw;
                secondEdgePoint.y = ry * rw;
                secondEdgePoint.z = rz * rw;
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
            var edge = new GeometryDataModels.Edge();
            for (var index = 0; index < seenGeometry.Count; index++)
            {
                var geoInfo = seenGeometry[index];

                geoInfo.edges = ShowEdgesInsideFrustum(geoInfo.edges, planes);

                seenGeometry[index] = geoInfo;

            }
        }
        
        public static GeometryDataModels.Edge[] ShowEdgesInsideFrustum(GeometryDataModels.Edge[] edges, UnityEngine.Plane[] planes)
        {
            for (var index = 0; index < edges.Length; index++)
            {
                if (IsInsideFrustum(edges[index], planes))
                {
                    edges[index].isVisible = True;
                }
                else
                {
                    edges[index].isVisible = False;
                }
            }

            return edges;
        }

        public static void UpdateEdgesVisibilityParallel(UnityEngine.Plane[] planes, List<GeometryDataModels.GeoInfo> geometries)
        {
            var edge = new GeometryDataModels.Edge();
            for (var index = 0; index < geometries.Count; index++)
            {
                var geoInfo = geometries[index];
                ShowEdgesInsideFrustum job = new ShowEdgesInsideFrustum(geometries[index].edges, edge, planes);
                var handle = job.Schedule(geoInfo.edges.Length, 64);
                handle.Complete();
                geoInfo.edges = job.edges.ToArray();
                geometries[index] = geoInfo;
                job.edges.Dispose();
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