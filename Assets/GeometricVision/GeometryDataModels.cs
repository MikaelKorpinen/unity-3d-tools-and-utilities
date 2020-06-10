using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;


public static class GeometryDataModels 
{
    public struct Edge
    {
        public int firstEdgePointIndex;
        public int secondEdgePointIndex;
        public int edgeIndex;
        public Vector3 firstVertex;
        public Vector3 secondVertex;
        public float lengthNonSquared;
        public Vector3 closestPoint;
        public Boolean isVisible;
    }
    public struct GeoInfo
    {
        public GameObject gameObject;
        public Renderer renderer;
        public Transform transform;
        public GeometryDataModels.Edge[] edges;
        public Mesh mesh;
        private Vector3[] BoundsCorners;
    }

    public struct NativeEdge
    {
        public int firstEdgePointIndex;
        public int secondEdgePointIndex;
        public int edgeIndex;
        public Vector3 firstVertex;
        public Vector3 secondVertex;
        public float lengthNonSquared;
        public Vector3 closestPoint;
        public byte isVisible;
    }
    
    public enum Plane : ushort
    {
        left = 0,
        right = 1,
        down = 2,
        up = 3,
        near = 4,
        far = 5,
    }
    /// <summary>
    /// Great and easy idea for Blittable type boolean from playerone-studio.com
    /// </summary>
    public enum Boolean : byte
    {
        False = 0,
        True = 1
    }
}
