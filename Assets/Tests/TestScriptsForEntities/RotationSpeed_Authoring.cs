using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
[ConverterVersion("Mikael", 1)]
public class RotationSpeed_Authoring : MonoBehaviour, IConvertGameObjectToEntity
{

    public float RadiansPerSecond;


    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var rotationData = new RotationSpeed_SpawnAndRemove()
        {
            // The referenced prefab will be converted due to DeclareReferencedPrefabs.
            // So here we simply map the game object to an entity reference to that prefab.
            RadiansPerSecond = RadiansPerSecond,
        };
        dstManager.AddComponentData(entity, rotationData);
    }
}