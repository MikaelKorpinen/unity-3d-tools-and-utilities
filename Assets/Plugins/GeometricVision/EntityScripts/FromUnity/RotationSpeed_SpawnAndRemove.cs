﻿using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
namespace Plugins.GeometricVision.EntityScripts.FromUnity
{
    [Serializable]
    public struct RotationSpeed_SpawnAndRemove : IComponentData
    {
        public float RadiansPerSecond;
    }
}
