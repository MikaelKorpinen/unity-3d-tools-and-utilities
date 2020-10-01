using System;
using System.Reflection;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using Plugins.GeometricVision.UtilitiesAndPlugins;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision
{
    /// <summary>
    /// Contains user defined targeting instructions for the GeometryVision object
    /// </summary>
    [Serializable]
    public class TargetingInstruction
    {
        private bool enabled = true;

        [SerializeField, Tooltip("Choose what geometry to target or use.")]
        private GeometryType geometryType;

        [SerializeField] private BoolReactiveProperty isTargetingEnabled = new BoolReactiveProperty();

        //Cannot get Reactive value from serialized property, so this boolean variable handles its job on the inspector gui behind the scenes.
        //See UI/VisionTypeDrawer.cs
        //It is not visible on the inspector but removing serialization makes it un findable
        [SerializeField] private bool isTargetActionsTemplateSlotVisible;

        [SerializeField, Tooltip("Choose what tag from unity tags settings to use")]
        private string targetTag;

        public bool Subscribed { get; set; }

        //GeometryVision plugin needs to be able to target both GameObjects and Entities at the same time
        private IGeoTargeting targetingSystemGameObjects = null; //TODO:consider: remove these for 2.0
        private IGeoTargeting targetingSystemEntities = null; //TODO:same
        [SerializeField] private Object entityQueryFilter;
        [SerializeField] private string entityQueryFilterName;
        [SerializeField] private string entityQueryFilterNameSpace;
        [SerializeField] private Type entityFilterComponent;
        [SerializeField] private ActionsTemplateObject targetingActions;

        /// <summary>
        /// Constructor for the GeometryVision targeting instructions object
        /// </summary>
        /// <param name="geoType"></param>
        /// <param name="tagName"></param>
        /// <param name="targetingSystem"></param>
        /// <param name="targetingEnabled"></param>
        /// <param name="entityQueryFilter"></param>
        public TargetingInstruction(GeometryType geoType, string tagName, IGeoTargeting targetingSystem,
            bool targetingEnabled, Object entityQueryFilter)
        {
            GeometryType = geoType;
            targetTag = tagName;
            this.EntityQueryFilter = entityQueryFilter;
#if UNITY_EDITOR
            if (this.EntityQueryFilter)
            {
                var nameSpace = GetNameSpace(this.EntityQueryFilter.ToString());
                this.EntityQueryFilterNameSpace = nameSpace;
                this.EntityQueryFilterName = EntityQueryFilter.name;
            }
#endif


            isTargetingEnabled.Value = targetingEnabled;
            AssignTargetingSystem(targetingSystem);

            isTargetActionsTemplateSlotVisible = targetingEnabled;

            void AssignTargetingSystem(IGeoTargeting geoTargeting)
            {
                if (targetingSystem != null && geoTargeting.IsForEntities())
                {
                    TargetingSystemEntities = geoTargeting;
                }
                else
                {
                    TargetingSystemGameObjects = geoTargeting;
                }
            }
        }

        /// <summary>
        /// Constructor overload for the GeometryVision targeting instruction object.
        /// Accepts factory settings as parameter.
        /// Easier to pass multiple parameters.
        /// </summary>
        /// <param name="geoType"></param>
        /// <param name="targetingSystem"></param>
        /// <param name="settings"></param>
        public TargetingInstruction(GeometryType geoType, IGeoTargeting targetingSystem,
            GeometryDataModels.FactorySettings settings)
        {
            GeometryType = geoType;
            targetTag = settings.defaultTag;
            this.entityQueryFilter = settings.entityComponentQueryFilter;
            this.entityFilterComponent = GetCurrentEntityFilterType();
            isTargetingEnabled.Value = settings.defaultTargeting;
            AssignTargetingSystem(targetingSystem);
            TargetingActions = settings.actionsTemplateObject;
            isTargetActionsTemplateSlotVisible = settings.defaultTargeting;

            void AssignTargetingSystem(IGeoTargeting geoTargeting)
            {
                if (targetingSystem != null && geoTargeting.IsForEntities())
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

        public Object EntityQueryFilter
        {
            get { return entityQueryFilter; }
            set { entityQueryFilter = value; }
        }

        public ActionsTemplateObject TargetingActions
        {
            get { return targetingActions; }
            set { targetingActions = value; }
        }

        public Type EntityFilterComponent
        {
            get { return entityFilterComponent; }
        }

        public string EntityQueryFilterName
        {
            get { return entityQueryFilterName; }
            set { entityQueryFilterName = value; }
        }

        public string EntityQueryFilterNameSpace
        {
            get { return entityQueryFilterNameSpace; }
            set { entityQueryFilterNameSpace = value; }
        }


        public void SetCurrentEntityFilterType(UnityEngine.Object entityFilterObject)
        {
            if (entityFilterObject)
            {
                var nameSpace = GetNameSpace(this.EntityQueryFilter.ToString());
                this.EntityQueryFilterNameSpace = nameSpace;
                this.EntityQueryFilterName = EntityQueryFilter.name;
                this.entityFilterComponent = Type.GetType(string.Concat(nameSpace, ".", entityFilterObject.name));
            }
        }

        public Type GetCurrentEntityFilterType()
        {
#if UNITY_EDITOR
            if (this.entityQueryFilter)
            {
                var nameSpace = GetNameSpace(this.EntityQueryFilter.ToString());
                this.EntityQueryFilterNameSpace = nameSpace;
                this.EntityQueryFilterName = EntityQueryFilter.name;
            }
#endif
            Type entityFilterType = Type.GetType(string.Concat(EntityQueryFilterNameSpace, ".", EntityQueryFilterName));
            return entityFilterType;
        }

        /// <summary>
        /// Finds the correct type for a script. 
        /// Script from unity forums combined with suggestion.
        /// </summary>
        /// <param name="TypeName"></param>
        /// <returns></returns>
        public static Type GetType(string TypeName)
        {
            // Try Type.GetType() first. This will work with types defined
            // by the Mono runtime, in the same assembly as the caller, etc.
            var type = Type.GetType(TypeName);

            // If it worked, then we're done here
            if (type != null)
                return type;

            // If the TypeName is a full name, then we can try loading the defining assembly directly
            if (TypeName.Contains("."))
            {
                // Get the name of the assembly (Assumption is that we are using 
                // fully-qualified type names)
                var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));

                // Attempt to load the indicated Assembly
                var assembly = Assembly.Load(assemblyName);
                if (assembly == null)
                    return null;

                // Ask that assembly to return the proper Type
                type = assembly.GetType(TypeName);
                if (type != null)
                    return type;
            }

            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var A in assemblies)
            {
                if (A.FullName.Contains("GeometricVision"))
                {
                    // Attempt to load the indicated Assembly
                    var assembly = Assembly.Load(A.FullName);
                    if (assembly == null)
                        return null;

                    // Ask that assembly to return the proper Type

                    return assembly.GetType(TypeName);
                }
            }

            // The type just couldn't be found...
            return null;
        }
        
        /// <summary>
        /// Get namespace for getting a type with class name
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Trimmed namespace</returns>
        private string GetNameSpace(string text)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            string toReturn = "";
            int elementFollowingNamespaceDeclaration = 1;
            foreach (var line in lines)
            {
                if (line.Contains("namespace"))
                {
                    toReturn = line.Split(' ')[elementFollowingNamespaceDeclaration].Trim();
                }
            }

            return toReturn;
        }
    }
}