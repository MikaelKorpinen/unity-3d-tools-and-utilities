using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using UnityEngine;

namespace Plugins.GeometricVision.Utilities
{
    public static class GeometryVisionUtilities
    {
        /// <summary>
        /// Creates and prepares settings for factory
        /// </summary>
        /// <param name="geoVision"></param>
        /// <param name="targetingInstructions"></param>
        /// <param name="settings"></param>
        internal static void SetupGeometryVision( GeometryVision geoVision, List<TargetingInstruction> targetingInstructions, GeometryDataModels.FactorySettings settings)
        {
            var factory = new GeometryVisionFactory(settings);
            var geoTypesToTarget = new List<GeometryType>();
            
            foreach (var targetType in targetingInstructions)
            {
                geoTypesToTarget.Add(targetType.GeometryType);
            }
            

            factory.CreateGeometryVision(new Vector3(0f, 0f, 0f), Quaternion.identity, 25, geoVision,
                geoTypesToTarget, 0);
        }
        
        /// <summary>
        /// Moves transform. GameObject version of the move target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="newPosition"></param>
        /// <param name="speedMultiplier"></param>
        /// <returns></returns>
        public static IEnumerator MoveTarget(Transform target, Vector3 newPosition, float speedMultiplier)
        {
            float timeOut = 10f;
            float stopMovingTreshold = 0.05f;
            while (Vector3.Distance(target.position, newPosition) > stopMovingTreshold)
            {
                var animatedPoint = Vector3.MoveTowards(target.position, newPosition, speedMultiplier);
                target.position = animatedPoint;
                if (timeOut < 0.1f)
                {
                    break;
                }

                timeOut -= 0.1f;

                yield return new WaitForSeconds(Time.deltaTime * 0.1f);
            }
        }
        
    }
}