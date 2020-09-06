using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
namespace Plugins.GeometricVision.Tests.TestScriptsForEntities.FromUnity
{
    [Serializable]
    public struct RotationSpeed_SpawnAndRemove : IComponentData
    {
        public float RadiansPerSecond;
    }
}
