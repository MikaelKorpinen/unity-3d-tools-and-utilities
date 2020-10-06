using Unity.Collections;
using Unity.Entities;

namespace Plugins.GeometricVision.EntityScripts.Components
{
    public struct Name : IComponentData {
        public FixedString64 Value;
    }
}
