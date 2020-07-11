// NOTE: This is a per project ifdfef,
//       the samples in this project are run in both modes for testing purposes.
//       In a normal game project this ifdef is not required.

using GeometricVision;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.EntityScripts
{
    class MeshRenderingAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Color Color = Color.white;
        public bool UseColliderMeshInsteadOfRendererMesh = false;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Assets in subscenes can either be created during conversion and embedded in the scene
            var material = new Material(Shader.Find("Standard"));
            material.color = Color;
            var renderer = GetComponent<Renderer>();
            Mesh mesh;

            if (UseColliderMeshInsteadOfRendererMesh)
            {
                mesh = GetComponent<MeshCollider>().sharedMesh;
            }
            else
            {
                mesh = GetComponent<MeshFilter>().mesh;
            }

            GeometryDataModels.Edge[] edges = new GeometryDataModels.Edge[1];
            // ... Or be an asset that is being referenced.
            dstManager.AddBuffer<EdgesBuffer>(entity);
            dstManager.AddBuffer<VerticesBuffer>(entity);
            dstManager.AddBuffer<TrianglesBuffer>(entity);
            DynamicBuffer<EdgesBuffer> eBuffer = dstManager.GetBuffer<EdgesBuffer>(entity);
            DynamicBuffer<VerticesBuffer> vBuffer = dstManager.GetBuffer<VerticesBuffer>(entity);
            DynamicBuffer<TrianglesBuffer> tBuffer = dstManager.GetBuffer<TrianglesBuffer>(entity);
            //Reinterpret to plain int buffer
            DynamicBuffer<GeometryDataModels.Edge> edgesBuffer = eBuffer.Reinterpret<GeometryDataModels.Edge>();
            DynamicBuffer<Vector3> vector3Buffer = vBuffer.Reinterpret<Vector3>();
            DynamicBuffer<int> triangleBuffer = tBuffer.Reinterpret<int>();

            //Optionally, populate the dynamic buffer
            for (int j = 0; j < mesh.vertexCount; j++)
            {
                vector3Buffer.Add(mesh.vertices[j]);
            }

            for (int j = 0; j < mesh.triangles.Length; j++)
            {
                triangleBuffer.Add(mesh.triangles[j]);
            }



            dstManager.AddComponentData(entity, new LocalToWorldMatrix
            {
                Matrix = renderer.localToWorldMatrix,
            });
            
            dstManager.AddComponentData(entity, new Visible
            {
                IsVisible = GeometryDataModels.Boolean.True,
            });
            
            var geoInfoData = new GeoInfoEntityComponent
            {
                Edges = edgesBuffer,
                Vertices = vector3Buffer,
                Triangles = triangleBuffer,
                Matrix = renderer.localToWorldMatrix,
            };
            dstManager.AddComponentData(entity, geoInfoData);
        }
    }

    public struct GeoInfoEntityComponent : IComponentData
    {
        //  public NativeArray<int> Triangles;
        //   public NativeArray<Vector3> Vertices;
        // public Material Material;
        public DynamicBuffer<GeometryDataModels.Edge> Edges;
        public DynamicBuffer<Vector3> Vertices;
        public DynamicBuffer<int> Triangles;
        public Matrix4x4 Matrix;
    }

    public struct LocalToWorldMatrix : IComponentData
    {
        public Matrix4x4 Matrix;
    }
    
    public struct Visible : IComponentData
    {
        public GeometryDataModels.Boolean IsVisible;
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
}