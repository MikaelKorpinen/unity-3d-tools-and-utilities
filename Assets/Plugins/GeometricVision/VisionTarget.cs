using System;
using GeometricVision;
using UniRx;
using UnityEngine;

namespace Plugins.GeometricVision
{
    [Serializable]
    public class VisionTarget
    {
        public bool enabled = true;
        public GeometryType type;
    
        [SerializeField] private BoolReactiveProperty target = new BoolReactiveProperty();
        [SerializeField, Layer] private int targetLayer = 31;
        public IGeoTargeting TargetingSystem { get; set; }
        public bool Subscribed { get; set; }

        public int TargetLayer
        {
            get { return targetLayer; }
            set { targetLayer = value; }
        }

        public GeometryType GeometryType
        {
            get { return type; }
            set { type = value; }
        }

        public BoolReactiveProperty Target
        {
            get { return target; }
            set { target = value; }
        }

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }


        public VisionTarget(GeometryType geoType, int layerIndex, IGeoTargeting targetingSystem)
        {
            GeometryType = geoType;
            targetLayer = layerIndex;
            this.TargetingSystem = targetingSystem;
            
            Target.Value = true;
        }
    }

    public class ExposePropertyAttribute : PropertyAttribute {
        public string PropertyName;
        public ExposePropertyAttribute(string propName) { PropertyName = propName; }
    }
    
}