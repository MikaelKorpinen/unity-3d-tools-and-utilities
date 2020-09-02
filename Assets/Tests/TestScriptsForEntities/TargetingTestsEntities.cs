using System;
using System.Collections;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.TestScriptsForEntities
{
    public class TargetingTestsEntities : MonoBehaviour
    {
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;

        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processEntities = true,
            defaultTargeting = true,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator TargetingSystemGetsAddedIfTargetingEnabled(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;

            bool isAdded = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                .GetTargetingProgram<GeometryEntitiesObjectTargeting>() != null;

            Assert.True(isAdded);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator TargetingSystemGetsTarget(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;

            GeometryDataModels.Target target = new GeometryDataModels.Target();
            Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(false); }).Run();

            Debug.Log("found targeting system: " + target);

            Assert.True(target.isEntity == true);
            Assert.True(target.distanceToCastOrigin > 0);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator TargetingSystemGetsClosestTarget(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;
            string[] testObjectNames = {"GameObject", "Quad", "Plane", "Cylinder", "Sphere", "Cube"};
            float
                offset = 0.1f; //TODO: Add a way to test boundaries of the geoVision view area. After that is implemented for entities
            GeometryDataModels.Target target = new GeometryDataModels.Target();

            for (var index = 0; index < testObjectNames.Length; index++)
            {
                var testObjectName = testObjectNames[index];
                geoVision.transform.position = new Vector3(index * -2f + offset, 0f, -6f);
                yield return null;
                Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(false); })
                    .Run();
                Assert.True(Vector3.Distance(target.position, new Vector3(index * -2f, 0f, 10f)) < 0.1f+ offset);
            }

            Debug.Log("found targeting system: " + target);

            Assert.True(target.isEntity == true);
            Assert.True(target.distanceToCastOrigin > 0);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator EntityFilteringByComponentWorks([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            factorySettings.entityComponentQueryFilter = typeof(RotationSpeed_SpawnAndRemove);
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;
            string[] testObjectNames = {"GameObject", "Quad", "Plane", "Cylinder", "Sphere", "Cube"};
            float
                offset = 0.1f; //TODO: Add a way to test boundaries of the geoVision view area. After that is implemented for entities
            GeometryDataModels.Target target = new GeometryDataModels.Target();
            int amountOfItemsFound = 0;
            int amountOfExpectedItemsToBeFound = 1;
            for (var index = 0; index < testObjectNames.Length; index++)
            {
                var testObjectName = testObjectNames[index];
                geoVision.transform.position = new Vector3(index * -2f + offset, 0f, -6f);
                yield return null;
                Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(false); })
                    .Run();
                if (Vector3.Distance(target.position, new Vector3(index * -2f, 0f, 10f)) < 0.1f+ offset)
                {
                    amountOfItemsFound += 1;
                    Assert.True(target.isEntity == true);
                    Assert.True(target.distanceToCastOrigin > 0);
                }
            }
            Assert.True(amountOfItemsFound == amountOfExpectedItemsToBeFound);
            Debug.Log("found targeting system: " + target);


        }
    }
}