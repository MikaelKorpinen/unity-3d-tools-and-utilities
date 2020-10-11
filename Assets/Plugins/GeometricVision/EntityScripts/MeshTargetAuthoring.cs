// NOTE: This is a per project ifdfef,
//       the samples in this project are run in both modes for testing purposes.
//       In a normal game project this ifdef is not required.

using Plugins.GeometricVision;
using Plugins.GeometricVision.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.EntityScripts
{
    class MeshTargetAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public bool UseColliderMeshInsteadOfRendererMesh = false;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
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
            dstManager.AddBuffer<GeometryDataModelsEntities.EdgesBuffer>(entity);
            dstManager.AddBuffer<GeometryDataModelsEntities.VerticesBuffer>(entity);
            dstManager.AddBuffer<GeometryDataModelsEntities.TrianglesBuffer>(entity);
            DynamicBuffer<GeometryDataModelsEntities.EdgesBuffer> eBuffer = dstManager.GetBuffer<GeometryDataModelsEntities.EdgesBuffer>(entity);
            DynamicBuffer<GeometryDataModelsEntities.VerticesBuffer> vBuffer = dstManager.GetBuffer<GeometryDataModelsEntities.VerticesBuffer>(entity);
            DynamicBuffer<GeometryDataModelsEntities.TrianglesBuffer> tBuffer = dstManager.GetBuffer<GeometryDataModelsEntities.TrianglesBuffer>(entity);
            //Reinterpret to plain int buffer
            DynamicBuffer<GeometryDataModels.Edge> edgesBuffer = eBuffer.Reinterpret<GeometryDataModels.Edge>();
            DynamicBuffer<Vector3> vector3Buffer = vBuffer.Reinterpret<Vector3>();
            DynamicBuffer<int> triangleBuffer = tBuffer.Reinterpret<int>();

            //populate the dynamic buffer
            for (int j = 0; j < mesh.vertexCount; j++)
            {
                vector3Buffer.Add(mesh.vertices[j]);
            }

            for (int j = 0; j < mesh.triangles.Length; j++)
            {
                triangleBuffer.Add(mesh.triangles[j]);
            }

            dstManager.AddComponentData(entity, new GeometryDataModelsEntities.LocalToWorldMatrix
            {
                Matrix = renderer.localToWorldMatrix,
            });
            
            dstManager.AddComponentData(entity, new GeometryDataModelsEntities.Visible
            {
                IsVisible = true,
            });
            
            var geoInfoData = new GeometryDataModelsEntities.GeoInfoEntityComponent
            {
                Edges = edgesBuffer,
                Vertices = vector3Buffer,
                Triangles = triangleBuffer,
                Matrix = renderer.localToWorldMatrix,
            };
            
            dstManager.AddComponentData(entity, geoInfoData);
            
            var targetData = new GeometryDataModels.Target
            {
                position = transform.position,
                projectedTargetPosition = Vector3.zero,
                distanceToRay = 0,
                distanceToCastOrigin = 0,
                entity = entity
            };
            dstManager.AddComponentData(entity, targetData);

        }
    }
}