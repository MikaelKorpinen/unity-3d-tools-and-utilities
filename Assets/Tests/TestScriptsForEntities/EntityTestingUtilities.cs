using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision;
using Plugins.GeometricVision.ImplementationsEntities;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[AlwaysUpdateSystem]
public class EntityTestingUtilities :  SystemBase
{
    private BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    [System.ComponentModel.ReadOnly(true)] public EntityCommandBuffer.Concurrent ConcurrentCommands;
    private int currentObjectCount;



    protected override void OnCreate()
    {
        // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        
        

    }

    protected override void OnUpdate()
    {
        var entityQuery = GetEntityQuery(typeof(Translation),typeof(GeometryDataModels.Target) );
        currentObjectCount = entityQuery.CalculateEntityCount();
    }

    public int CountEntities()
    {
        Update();
        return currentObjectCount;
    }
}
