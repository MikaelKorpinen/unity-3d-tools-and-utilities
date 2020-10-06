using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.EntityScripts;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Plugins.GeometricVision.Utilities
{
    public static class GeometryVisionUtilities
    {
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

        internal static IEnumerator MoveEntityTarget(Vector3 newPosition,
            float speedMultiplier, GeometryDataModels.Target target, float distanceToStop,
            TransformEntitySystem transformEntitySystem, World entityWorld,
            NativeSlice<GeometryDataModels.Target> closestTargets)
        {
            if (transformEntitySystem == null)
            {
                transformEntitySystem = entityWorld.CreateSystem<TransformEntitySystem>();
            }

            float timeOut = 2f;
            while (Vector3.Distance(target.position, newPosition) > distanceToStop)
            {
                var animatedPoint =
                    transformEntitySystem.MoveEntityToPosition(newPosition, target, speedMultiplier);

                target.position = animatedPoint;
                if (closestTargets.Length != 0)
                {
                    closestTargets[0] = target;
                }

                if (timeOut < 0.1f)
                {
                    break;
                }

                timeOut -= Time.deltaTime;

                yield return null;
            }
        }

        public static bool TargetHasNotChanged(GeometryDataModels.Target newTarget,
            GeometryDataModels.Target currentTarget)
        {
            return newTarget.GeoInfoHashCode == currentTarget.GeoInfoHashCode
                   && newTarget.entity.Index == currentTarget.entity.Index
                   && newTarget.entity.Version == currentTarget.entity.Version;
        }

        //Usage: this.targets.Sort<GeometryDataModels.Target, DistanceComparer>(new DistanceComparer());
        public struct DistanceComparer : IComparer<GeometryDataModels.Target>
        {
            public int Compare(GeometryDataModels.Target x, GeometryDataModels.Target y)
            {
                if (x.distanceToCastOrigin == 0f && y.distanceToCastOrigin != 0f)
                {
                    return 1;
                }

                if (x.distanceToCastOrigin != 0f && y.distanceToCastOrigin == 0f)
                {
                    return -1;
                }

                if (x.distanceToRay < y.distanceToRay)
                {
                    return -1;
                }

                else if (x.distanceToRay > y.distanceToRay)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        internal static bool TransformIsEffect(string nameOfTransform)
        {
            if (nameOfTransform.Length < GeometryVisionSettings.NameOfEndEffect.Length
                && nameOfTransform.Length < GeometryVisionSettings.NameOfMainEffect.Length
                && nameOfTransform.Length < GeometryVisionSettings.NameOfStartingEffect.Length)
            {
                return false;
            }

            return nameOfTransform.Contains(GeometryVisionSettings.NameOfStartingEffect) ||
                   nameOfTransform.Contains(GeometryVisionSettings.NameOfMainEffect) ||
                   nameOfTransform.Contains(GeometryVisionSettings.NameOfEndEffect);
        }


        public static void HandleEntityImplementationAddition<TImplementation, TCollection>(
            TImplementation entitySystemToAdd, HashSet<TCollection> listOfInterfaces, World eWorld, Action action)
            where TImplementation : ComponentSystemBase, TCollection, new()
        {
            if (entitySystemToAdd == null)
            {
                if (eWorld == null)
                {
                    eWorld = World.DefaultGameObjectInjectionWorld;
                }

                var eye = eWorld.CreateSystem<TImplementation>();
                InterfaceUtilities.AddImplementation(eye, listOfInterfaces);
                action();
            }
        }

        /// <summary>
        /// Clears up current targeting programs and creates a new, then proceeds to add all the available targeting systems
        /// to the targeting systems container
        /// </summary>
        internal static void UpdateTargetingSystemsContainer(List<TargetingInstruction> targetingInstructions,
            GeometryTargetingSystemsContainer targetingSystemsContainer)
        {
            targetingSystemsContainer.TargetingPrograms = new HashSet<IGeoTargeting>();

            foreach (var targetingInstruction in targetingInstructions)
            {
                if (targetingInstruction.TargetingSystemGameObjects != null)
                {
                    targetingSystemsContainer.AddTargetingProgram(targetingInstruction.TargetingSystemGameObjects);
                }

                if (targetingInstruction.TargetingSystemEntities != null)
                {
                    targetingSystemsContainer.AddTargetingProgram(targetingInstruction.TargetingSystemEntities);
                }
            }
        }

        /// <summary>
        /// In case the user plays around with the settings on the inspector and changes thins this needs to be run.
        /// It checks that the targeting system implementations are correct.
        /// </summary>
        /// <param name="targetingInstructionsIn"></param>
        /// <param name="gameObjectProcessing"></param>
        /// <param name="entityBasedProcessing"></param>
        /// <param name="entityWorld"></param>
        public static List<TargetingInstruction> ValidateTargetingSystems(
            List<TargetingInstruction> targetingInstructionsIn, bool gameObjectProcessing, bool entityBasedProcessing,
            World entityWorld)
        {
            ValidatePresentTargetingInstructions();

            void ValidatePresentTargetingInstructions()
            {
                foreach (var targetingInstruction in targetingInstructionsIn)
                {
                    if (gameObjectProcessing == true)
                    {
                        targetingInstruction.TargetingSystemGameObjects =
                            AssignNewTargetingSystemToTargetingInstruction(targetingInstruction,
                                new GeometryObjectTargeting(), new GeometryLineTargeting());
                    }

                    if (entityBasedProcessing == true && Application.isPlaying)
                    {
                        if (entityWorld == null)
                        {
                            entityWorld = World.DefaultGameObjectInjectionWorld;
                        }

                        var newObjectTargeting = entityWorld.CreateSystem<GeometryEntitiesObjectTargeting>();
                        var newLineTargeting = entityWorld.CreateSystem<GeometryEntitiesLineTargeting>();

                        targetingInstruction.TargetingSystemEntities =
                            AssignNewTargetingSystemToTargetingInstruction(targetingInstruction, newObjectTargeting,
                                newLineTargeting);
                    }
                }
            }

            return targetingInstructionsIn;

            // Local functions

            IGeoTargeting AssignNewTargetingSystemToTargetingInstruction(TargetingInstruction targetingInstruction,
                IGeoTargeting newObjectTargeting,
                IGeoTargeting newLineTargeting)
            {
                IGeoTargeting targetingToReturn = null;

                if (targetingInstruction.GeometryType == GeometryType.Objects)
                {
                    targetingToReturn = newObjectTargeting;
                }

                if (targetingInstruction.GeometryType == GeometryType.Lines)
                {
                    targetingToReturn = newLineTargeting;
                }

                return targetingToReturn;
            }
        }
    }
}