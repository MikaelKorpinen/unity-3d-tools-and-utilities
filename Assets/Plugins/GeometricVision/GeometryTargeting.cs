using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GeometricVision
{
    public class GeometryTargeting : MonoBehaviour
    {
        [SerializeField] private bool debugMode;
        private GeometryVisionEye eye;
        private IGeoTargeting[] targetingPrograms;
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
            if (GetComponent<GeometryVisionEye>() == null)
            {
                gameObject.AddComponent<GeometryVisionEye>();
                eye = GetComponent<GeometryVisionEye>();
            }
          /*  targetingPrograms = new IGeoTargeting[eye.GeometryTypes.Length];
            foreach (var targetTypes in eye.GeometryTypes)
            {
                
            }*/
        }
    }
}
