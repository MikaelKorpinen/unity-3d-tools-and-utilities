using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision
{
    public enum GeometryType
    {
        all,
        Vertices,
        Lines,
        Objects
    }

    /// <summary>
    /// Contains most of the public data blueprints for the GeometryVision plugin
    /// </summary>
    public static class GeometryDataModels
    {
        public struct Edge:IComponentData
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
           // private Vector3[] BoundsCorners;
            public Mesh colliderMesh;
        }
        
        public struct Target :IComponentData
        {
            public Vector3 position;
            public Vector3 projectedTargetPosition;
            public float distanceToRay;
            public float distanceToCastOrigin;
            public bool isEntity;
            public int GeoInfoHashCode;
            public Entity entity;
            public bool isSeen;
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

        public struct FactorySettings
        {
            public bool processGameObjects;
            public bool processEntities;
            public bool processGameObjectsEdges;
            public float fielOfView;
            public bool edgesTargeted;
            public bool defaultTargeting;
            public string defaultTag;
            public Object entityComponentQueryFilter;
            public ActionsTemplateObject actionsTemplateObject;
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