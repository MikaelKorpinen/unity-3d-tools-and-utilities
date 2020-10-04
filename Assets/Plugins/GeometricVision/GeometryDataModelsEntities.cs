using Unity.Entities;
using UnityEngine;

namespace Plugins.GeometricVision
{
    /// <summary>
    /// Class to store data model definitions
    /// </summary>
    public static class GeometryDataModelsEntities
    {
        public struct EdgesBuffer : IBufferElementData
        {
            // These implicit conversions are optional, but can help reduce typing.
            public static implicit operator GeometryDataModels.Edge(EdgesBuffer e)
            {
                return e.edge;
            }

            public static implicit operator EdgesBuffer(GeometryDataModels.Edge e)
            {
                return new EdgesBuffer {edge = e};
            }

            public GeometryDataModels.Edge edge;
        }
              
        
        public struct LocalToWorldMatrix : IComponentData
        {
            public Matrix4x4 Matrix;
        }
    
        public struct Visible : IComponentData
        {
            public bool IsVisible;
        }
    
        public struct VerticesBuffer : IBufferElementData
        {
            // These implicit conversions are optional, but can help reduce typing.
            public static implicit operator Vector3(VerticesBuffer e)
            {
                return e.VertexData;
            }

            public static implicit operator VerticesBuffer(Vector3 e)
            {
                return new VerticesBuffer {VertexData = e};
            }

            public Vector3 VertexData;
        }

        public struct TrianglesBuffer : IBufferElementData
        {
            // These implicit conversions are optional, but can help reduce typing.
            public static implicit operator int(TrianglesBuffer e)
            {
                return e.triangle;
            }

            public static implicit operator TrianglesBuffer(int e)
            {
                return new TrianglesBuffer {triangle = e};
            }

            public int triangle;
        }
        
        public struct GeoInfoEntityComponent : IComponentData
        {
            public DynamicBuffer<GeometryDataModels.Edge> Edges;
            public DynamicBuffer<Vector3> Vertices;
            public DynamicBuffer<int> Triangles;
            public Matrix4x4 Matrix;
        }
    }
}