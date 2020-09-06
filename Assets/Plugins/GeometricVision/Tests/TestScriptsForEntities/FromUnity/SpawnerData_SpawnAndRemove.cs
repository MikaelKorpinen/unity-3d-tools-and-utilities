using Unity.Entities;

// ReSharper disable once InconsistentNaming
namespace Plugins.GeometricVision.Tests.TestScriptsForEntities.FromUnity
{
    public struct SpawnerData_SpawnAndRemove : IComponentData
    {
        public int CountX;
        public int CountY;
        public Entity Prefab;
        public float separationMultiplier;
    }
}
