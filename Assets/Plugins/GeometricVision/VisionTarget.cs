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
    
    /// <summary>
    /// Contains user defined targeting instructions for the GeometryVision object
    /// </summary>
    [Serializable]
    public class VisionTarget
    {
        public bool enabled = true;
        [SerializeField,  Tooltip("Choose what geometry to target or use.")] private GeometryType geometryType;
    
        [SerializeField] private BoolReactiveProperty target = new BoolReactiveProperty();
        //Cannot get Reactive value from serialized property, so this boolean variable handles its job on the inspector gui behind the scenes.
        //See UI/VisionTypeDrawer.cs
        //It is not visible on the inspector but removing serialization makes it un findable
        [SerializeField] private bool targetActionsTemplateSlotVisible;
        [SerializeField, Layer, Tooltip("Choose what layer from unity layers settings to use")] private int targetLayer = 31;
        public bool Subscribed { get; set; }
        public ActionsTemplateObject targetingActions;
        //GeometryVision plugin needs to be able to target both GameObjects and Entities at the same time
        private IGeoTargeting targetingSystemGameObjects = null;
        private IGeoTargeting targetingSystemEntities = null;
        
        /// <summary>
        /// Constructor for the GeometryVision target object
        /// </summary>

        /// <param name="geoType"></param>
        /// <param name="layerIndex"></param>
        /// <param name="targetingSystem"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public VisionTarget(GeometryType geoType, int layerIndex, IGeoTargeting targetingSystem, bool targetingEnabled)
        {
            GeometryType = geoType;
            targetLayer = layerIndex;
            
            if (null == targetingSystem)
            {
                throw new ArgumentNullException(nameof(targetingSystem));
            }

            target.Value = targetingEnabled;
            AssignTargetingSystem(targetingSystem);

            Target.Value = true;
            
            void AssignTargetingSystem(IGeoTargeting geoTargeting)
            {
                if (geoTargeting.IsForEntities())
                {
                    TargetingSystemEntities = geoTargeting;
                }
                else
                {
                    TargetingSystemGameObjects = geoTargeting;
                }
            }
        }
        
        public IGeoTargeting TargetingSystemGameObjects
        {
            get { return targetingSystemGameObjects; }
            set { targetingSystemGameObjects = value; }
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

        public bool TargetActionsTemplateSlotVisible
        {
            get { return targetActionsTemplateSlotVisible; }
            set { targetActionsTemplateSlotVisible = value; }
        }
    }
}