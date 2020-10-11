using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.StressTests
{
    public class StressPerformanceTests
    {
        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processEntities = false,
            defaultTargeting = true,
            processGameObjects = false,
            entityComponentQueryFilter = null,
            edgesTargeted = false,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
            factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView = 25f,
                processEntities = false,
                defaultTargeting = true,
                processGameObjects = false,
                entityComponentQueryFilter = null,
                edgesTargeted = false,
            };
            TestUtilities.CleanUpEntities();
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesStressTests))]
        public IEnumerator PerformanceOfGetClosestTargetForEntities(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetStressTestsScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            factorySettings.processEntities = true;
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var target = new GeometryDataModels.Target();

            Measure.Method(() => { geoVision.GetComponent<GeometryVision>().UpdateClosestTargets(true, true); }).Run();
            target = geoVision.GetComponent<GeometryVision>().GetClosestTarget();
            Assert.True(target.distanceToCastOrigin != 0f);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsStressTests))]
        public IEnumerator PerformanceOfGetClosestTargetForGameObjects(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetStressTestsScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            factorySettings.processGameObjects = true;
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var target = new GeometryDataModels.Target();

            Measure.Method(() => { geoVision.GetComponent<GeometryVision>().UpdateClosestTargets(true,true); }).Run();
            target = geoVision.GetComponent<GeometryVision>().GetClosestTarget();
            Assert.True(target.distanceToCastOrigin != 0f);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsStressTests))]
        public IEnumerator PerformanceOfUpdateVisibilityForGameObjects(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetStressTestsScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            factorySettings.processGameObjects = true;
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var target = new GeometryDataModels.Target();

            Measure.Method(() =>
            {
                geoVision.GetComponent<GeometryVision>().GetEye<GeometryVisionEye>().UpdateVisibility(false);
            }).Run();
            target = geoVision.GetComponent<GeometryVision>().GetClosestTarget();
            Assert.True(target.distanceToCastOrigin != 0f);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsStressTests))]
        public IEnumerator PerformanceOfUpdateVisibilityForGameObjectsWithTagEnabled(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetStressTestsScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            factorySettings.processGameObjects = true;
            factorySettings.defaultTag = "Player";
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var target = new GeometryDataModels.Target();
            factorySettings.defaultTag = "";
            Measure.Method(() =>
            {
                geoVision.GetComponent<GeometryVision>().GetEye<GeometryVisionEye>().UpdateVisibility(false);
            }).Run();
            target = geoVision.GetComponent<GeometryVision>().GetClosestTarget();
            Assert.True(target.distanceToCastOrigin != 0f);
        }
    }
}