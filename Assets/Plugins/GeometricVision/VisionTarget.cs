using System;
using GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.UI;

using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using UniRx;
using UnityEditor;
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
    
        [SerializeField] private BoolReactiveProperty isTargetingEnabled = new BoolReactiveProperty();
        //Cannot get Reactive value from serialized property, so this boolean variable handles its job on the inspector gui behind the scenes.
        //See UI/VisionTypeDrawer.cs
        //It is not visible on the inspector but removing serialization makes it un findable
        [SerializeField] private bool isTargetActionsTemplateSlotVisible;
        [SerializeField,  Tooltip("Choose what tag from unity tags settings to use")] private string targetTag;
        public bool Subscribed { get; set; }
        public ActionsTemplateObject targetingActions;
        //GeometryVision plugin needs to be able to target both GameObjects and Entities at the same time
        private IGeoTargeting targetingSystemGameObjects = null; //TODO:consider: remove these
        private IGeoTargeting targetingSystemEntities = null; //TODO:same
        private Type entityQueryFilter;
        /// <summary>
        /// Constructor for the GeometryVision target object
        /// </summary>

        /// <param name="geoType"></param>
        /// <param name="tagName"></param>
        /// <param name="targetingSystem"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public VisionTarget(GeometryType geoType, string tagName, IGeoTargeting targetingSystem, bool targetingEnabled, Type entityQueryFilter)
        {
            GeometryType = geoType;
            targetTag = tagName;
            this.entityQueryFilter = entityQueryFilter;

            isTargetingEnabled.Value = targetingEnabled;
            AssignTargetingSystem(targetingSystem);
            
            isTargetActionsTemplateSlotVisible = targetingEnabled;
            void AssignTargetingSystem(IGeoTargeting geoTargeting)
            {
                if (targetingSystem != null &&geoTargeting.IsForEntities())
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

        public string TargetTag
        {
            get { return targetTag; }
            set { targetTag = value; }
        }

        public GeometryType GeometryType
        {
            get { return geometryType; }
            set { geometryType = value; }
        }
        
        /// <summary>
        /// Use the targeting system, if Target.Value set to true
        /// </summary>
        public BoolReactiveProperty IsTargetingEnabled
        {
            get { return isTargetingEnabled; }
            set { isTargetingEnabled = value; }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public bool IsTargetActionsTemplateSlotVisible
        {
            get { return isTargetActionsTemplateSlotVisible; }
            set { isTargetActionsTemplateSlotVisible = value; }
        }
    }
}