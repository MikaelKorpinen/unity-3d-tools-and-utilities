using System;
using GeometricVision;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.UI;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace Plugins.GeometricVision
{
    [Serializable]
    public class TargetingEvents : UnityEvent{ }
    [Serializable]
    public class VisionTarget
    {
        public bool enabled = true;
        [SerializeField,  Tooltip("Choose what geometry to target or use.")] private GeometryType geometryType;
    
        [SerializeField] private BoolReactiveProperty target = new BoolReactiveProperty();
        //Cannot get Reactive value from serialized property, so this boolean variable handles it job on the inspector gui under the hood.
        //It is not visible on the inspector but removing serialization makes it un findable
        [SerializeField] private bool targetHidden;
        [SerializeField] private Action actionToPerform;
        [SerializeField, Layer, Tooltip("Choose what layer from unity layers settings to use")] private int targetLayer = 31;
        public bool Subscribed { get; set; }
        public ActionsTemplateObject targetingActions;
        private IGeoTargeting targetingSystem = null;
        private IGeoTargeting targetingSystemEntities = null;

        public VisionTarget(GeometryType geoType, int layerIndex, IGeoTargeting targetingSystem)
        {
            GeometryType = geoType;
            targetLayer = layerIndex;
            this.TargetingSystem = targetingSystem;
            
            Target.Value = true;
        }
        public IGeoTargeting TargetingSystem
        {
            get { return targetingSystem; }
            set { targetingSystem = value; }
        }

        public IGeoTargeting TargetingSystemEntities
        {
            get { return targetingSystemEntities; }
            set { targetingSystemEntities = value; }
        }

        public int TargetLayer
        {
            get { return targetLayer; }
            set { targetLayer = value; }
        }

        public GeometryType GeometryType
        {
            get { return geometryType; }
            set { geometryType = value; }
        }
        
        /// <summary>
        /// Use the targeting system, if Target.Value set to true
        /// </summary>
        public BoolReactiveProperty Target
        {
            get { return target; }
            set { target = value; }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public bool TargetHidden
        {
            get { return targetHidden; }
            set { targetHidden = value; }
        }
    }

    public class ExposePropertyAttribute : PropertyAttribute {
        public string PropertyName;
        public ExposePropertyAttribute(string propName) { PropertyName = propName; }
    }
    
}