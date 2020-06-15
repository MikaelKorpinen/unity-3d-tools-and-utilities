using System;
using System.Collections.Generic;
using Plugins.GeometricVision;
using UnityEngine;
using UnityEngine.Serialization;

namespace GeometricVision
{
    public class GeometryTargeting : MonoBehaviour
    {
        [SerializeField] private bool debugMode;
        private GeometryVisionEye eye;
        private HashSet<IGeoTargeting> targetingPrograms =new HashSet<IGeoTargeting>();
        [SerializeField] private int targetingSystemsCount = 0;

        public HashSet<IGeoTargeting> TargetingPrograms
        {
            get { return targetingPrograms; }
            set { targetingPrograms = value; }
        }

        private void Reset()
        {
            InitializeTargeting();
        }

        void Start()
        {
            InitializeTargeting();
        }

        /// <summary>
        /// Initializes the geometry vision eye, which handles setting up the geometry vision plugin.
        /// </summary>
        private void InitializeTargeting()
        {
            if (TargetingPrograms == null)
            {
                TargetingPrograms = new HashSet<IGeoTargeting>();
            }

            if (GetComponent<GeometryVisionEye>() == null)
            {
                gameObject.AddComponent<GeometryVisionEye>();
                eye = GetComponent<GeometryVisionEye>();
            }

            /*targetingPrograms = new IGeoTargeting[eye.GeometryTypes.Length];
             
              foreach (var targetTypes in eye.GeometryTypes)
              {
                  
              }*/
        }

        public void AddTarget(VisionTarget geometryContainer)
        {
            if (geometryContainer.type == GeometryType.Objects_)
            {
                IGeoTargeting targetingSystem = new GeometryObjectTargeting();
                geometryContainer.TargetingSystem = targetingSystem;
                TargetingPrograms.Add(geometryContainer.TargetingSystem);
            }

            if(TargetingPrograms != null){
                targetingSystemsCount = TargetingPrograms.Count;
            }
        }

        public void RemoveTarget(VisionTarget geometryType)
        {
            if (geometryType.TargetingSystem != null)
            {
                TargetingPrograms.Remove(geometryType.TargetingSystem);
                targetingSystemsCount = TargetingPrograms.Count;
            }
        }
    }
}