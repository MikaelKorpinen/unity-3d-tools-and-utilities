using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Plugins.GeometricVision
{
    public enum GeometryType
    {
        all,
        Vertices,
        Lines,
        Objects
    }


    public static class GeometryDataModels
    {
        public struct Edge
        {
            public int firstEdgePointIndex;
            public int secondEdgePointIndex;
            public Vector3 firstVertex;
            public Vector3 secondVertex;
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
            public Mesh colliderMesh;
        }
        
        public struct Target :IComponentData, IEnumerable
        {
            public Vector3 position;
            public Vector3 projectedTargetPosition;
            public float distanceToRay;
            public float distanceToCastOrigin;
            public IEnumerator GetEnumerator()
            {
                throw new System.NotImplementedException();
            }
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
}