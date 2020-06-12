using System;
using GeometricVision;
using UniRx;
using UnityEngine;

namespace Plugins.GeometricVision
{
    [Serializable]
    public class VisionTarget
    {
        public bool onOff = true;
        public GeometryType type;
        [ExposeProperty("Targeting"), SerializeField] public ReactiveProperty<bool> targeting = new ReactiveProperty<bool>();
        [SerializeField] public BoolReactiveProperty target;
        [SerializeField, Layer] private int targetLayer = 31;

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

        public bool Targeting 
        {
            get {
                return targeting.Value;
            } 
            set {
                targeting.Value = value;
            }
        }

        public VisionTarget(GeometryType geoType, int layerIndex)
        {
            GeometryType = geoType;
            targetLayer = layerIndex;
        }
    }

    public class ExposePropertyAttribute : PropertyAttribute {
        public string PropertyName;
        public ExposePropertyAttribute(string propName) { PropertyName = propName; }
    }
    
}