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
        /// <param name="targetTransform"></param>
        /// <param name="newPosition"></param>
        /// <param name="movementSpeed"></param>
        /// <param name="distanceToStop"></param>
        public static IEnumerator MoveTarget(Transform targetTransform, Vector3 newPosition, float movementSpeed,
            float distanceToStop)
        {
            float timeOut = 10f;

            while (targetTransform && Vector3.Distance(targetTransform.position, newPosition) > distanceToStop)
            {
                var animatedPoint = Vector3.MoveTowards(targetTransform.position, newPosition, movementSpeed);
                targetTransform.position = animatedPoint;
                if (timeOut < 0.05f)
                {
                    break;
                }

                timeOut -= Time.deltaTime;

                yield return null;
            }
        }

        public static bool TargetHasNotChanged(GeometryDataModels.Target newTarget, GeometryDataModels.Target currentTarget)
        {
            
            return newTarget.GeoInfoHashCode == currentTarget.GeoInfoHashCode
                   && newTarget.entity.Index == currentTarget.entity.Index
                   && newTarget.entity.Version == currentTarget.entity.Version;
        }
        
        //Usage: this.targets.Sort<GeometryDataModels.Target, DistanceComparer>(new DistanceComparer());
        public class DistanceComparer : IComparer<GeometryDataModels.Target>
        {
            public int Compare(GeometryDataModels.Target x, GeometryDataModels.Target y)
            {
                return Comparer<float>.Default.Compare(y.distanceToRay, x.distanceToRay);
            }
        }
    }
}