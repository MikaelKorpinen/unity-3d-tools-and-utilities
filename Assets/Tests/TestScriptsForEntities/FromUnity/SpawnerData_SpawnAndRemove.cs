using Unity.Entities;

// ReSharper disable once InconsistentNaming
public struct SpawnerData_SpawnAndRemove : IComponentData
{
    public int CountX;
    public int CountY;
    public Entity Prefab;
    public float separationMultiplier;
}
