using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using UnityEngine;

namespace Plugins.GeometricVision
{
    /// <summary>
    /// Made to contain targeting systems created by the user. Has logic for adding and removing targeting systems.
    /// Also handles things like initialization and cleaning up components if removed.
    /// Usage: Component is added and managed automatically by GeometricVision component
    /// </summary>
    [DisallowMultipleComponent]
    public class GeometryTargetingSystemsContainer : MonoBehaviour
    {
        [SerializeField] private bool debugMode;
        private GeometryVisionEye eye;
        [SerializeField]private HashSet<IGeoTargeting> targetingPrograms =new HashSet<IGeoTargeting>();

        public HashSet<IGeoTargeting> TargetingPrograms
        {
            set { targetingPrograms = value; }
        }

        private void Reset()
        {
            InitializeTargetingContainer();
        }

        void Awake()
        {
            InitializeTargetingContainer();
        }
        
        public IEnumerator RemoveAddedComponents()
        {
            if (this != null)
            {
                if (this.GetComponent<Camera>() != null)
                {
                    DestroyImmediate(this.GetComponent<Camera>());
                }
            
                if (this.GetComponent<GeometryVisionEye>() != null)
                {
                    DestroyImmediate(GetComponent<GeometryVisionEye>());
                }

                if (this.GetComponent<GeometryTargetingSystemsContainer>() != null)
                {
                    DestroyImmediate(GetComponent<GeometryTargetingSystemsContainer>());
                }
            }

            yield return null;
        }

        
        private void InitializeTargetingContainer()
        {
            TargetingPrograms = new HashSet<IGeoTargeting>();
        }
        
        public void AddTargetingProgram(IGeoTargeting targetingSystem)
        {
            targetingPrograms.Add(targetingSystem);
        }

        public T GetTargetingProgram<T>()
        {
            return (T) InterfaceUtilities.GetInterfaceImplementationOfTypeFromList(typeof(T), targetingPrograms);
        }
        
        public int GetTargetingProgramsCount()
        {
            return targetingPrograms.Count;
        }
        
        public void RemoveTargetingProgram(IGeoTargeting targetingSystem)
        {
            if (targetingSystem != null)
            {
                targetingPrograms.Remove(targetingSystem);
            }
        }
    }
}