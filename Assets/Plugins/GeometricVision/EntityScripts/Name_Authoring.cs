using Plugins.GeometricVision.EntityScripts.Components;
using Unity.Entities;
using UnityEngine;

namespace Plugins.GeometricVision.EntityScripts
{
    [RequiresEntityConversion]
    [ConverterVersion("Mikael", 1)]
    public class Name_Authoring : MonoBehaviour, IConvertGameObjectToEntity
    {

        [SerializeField, Tooltip("Give name for the entity, if empty use gameObjects name instead")]private string overrideName ="";

        // Lets you convert the editor data representation to the entity optimal runtime representation
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            Name nameData;
            if (overrideName == "")
            {
                 nameData = new Name()
                {
                    Value = gameObject.name
                };
            }
            else
            {
                 nameData = new Name()
                {
                    Value = overrideName
                };
            }

            dstManager.AddComponentData(entity, nameData);
        }
    }
}