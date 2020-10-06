using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision
{
    public enum GeometryType
    {
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
            public float3 firstVertex;
            public float3 secondVertex;
            public bool isVisible;
        }
        
        /// <summary>
        /// Cache object for GameObject related data
        /// </summary>
        public struct GeoInfo
        {
            public GameObject gameObject;
            public Renderer renderer;
            public Transform transform;
            public GeometryDataModels.Edge[] edges;
            public Mesh mesh;
            public Mesh colliderMesh;
        }
        
        /// <summary>
        /// Multi threading friendly target object containing info how to find entity or gameObject and info about
        /// distances.
        /// </summary>
        public struct Target :IComponentData, IComparable<Target>
        {
            public float3 position;
            public float3 projectedTargetPosition;
            public float distanceToRay;
            public float distanceToCastOrigin;
            public bool isEntity;
            public int GeoInfoHashCode;
            public Entity entity;
            public bool isSeen;

            public int CompareTo(Target other)
            {
                var distanceToRayComparison = distanceToRay.CompareTo(other.distanceToRay);
                if (distanceToRayComparison != 0) return distanceToRayComparison;
                var distanceToCastOriginComparison = distanceToCastOrigin.CompareTo(other.distanceToCastOrigin);
                if (distanceToCastOriginComparison != 0) return distanceToCastOriginComparison;
       
                return isSeen.CompareTo(other.isSeen);
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

        public struct FactorySettings
        {
            public bool processGameObjects;
            public bool processEntities;
            public float fielOfView;
            public bool edgesTargeted;
            public bool defaultTargeting;
            public string defaultTag;
            public Object entityComponentQueryFilter;
            public ActionsTemplateObject actionsTemplateObject;
        }
    }
}