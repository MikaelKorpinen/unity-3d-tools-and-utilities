using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests
{
    public class CreationalTestsGameObjectsAndEntitiesSimultaneously : MonoBehaviour
    {
        private const string version = TestSettings.Version;

        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processGameObjects = true,
            processEntities = true,
            defaultTargeting = true
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
            factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView = 25f,
                processGameObjects = true,
                processEntities = true,
                defaultTargeting = true
            };
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TestSceneGetsLoadedForGameObjectsAndEntities(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
           
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f),
                new GeometryVisionFactory(factorySettings));
            yield return null;
            
            Assert.True(scenePath.Contains(SceneManager.GetActiveScene().name));
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator GeometryVisionGetsCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))]
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

            int amountOfObjectsInScene = 0;
            IGeoProcessor processor =
                geoVision.GetComponent<GeometryVision>().Runner.GetProcessor<GeometryVisionProcessor>();
            IGeoProcessor processorEntities = geoVision.GetComponent<GeometryVision>().Runner
                .GetProcessor<GeometryVisionEntityProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();

            yield return null;
            Assert.True(amountOfObjectsInScene > 0);
            amountOfObjectsInScene = processorEntities.CountSceneObjects();
            Assert.True(amountOfObjectsInScene > 0);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator GeometryVisionGetsCreatedWithoutFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision = Instantiate(new GameObject("geoVision"));
            geoVision.transform.position = new Vector3(0f, 0f, -6f);

            geoVision.AddComponent<GeometryVision>();
            var geometryVisionComponent = geoVision.GetComponent<GeometryVision>();
            geometryVisionComponent.GameObjectBasedProcessing.Value = true;
            geometryVisionComponent.EntityProcessing.Value = true;
            yield return null;
            int amountOfObjectsInScene = 0;
            IGeoProcessor processor =
                geoVision.GetComponent<GeometryVision>().Runner.GetProcessor<GeometryVisionProcessor>();
            IGeoProcessor processorEntities = geoVision.GetComponent<GeometryVision>().Runner
                .GetProcessor<GeometryVisionEntityProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();
            yield return null;
            Assert.True(amountOfObjectsInScene > 0);
            amountOfObjectsInScene = processorEntities.CountSceneObjects();
            Assert.True(amountOfObjectsInScene > 0);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithoutFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision = Instantiate(new GameObject("geoVision"));
            geoVision.transform.position = new Vector3(0f, 0f, -6f);

            //TODO: Find out how to measure stuff happening in the start and awake from here to get the real estimate
            geoVision.AddComponent<GeometryVision>();
            var geometryVisionComponent = geoVision.GetComponent<GeometryVision>();
            geometryVisionComponent.GameObjectBasedProcessing.Value = factorySettings.processGameObjects;
            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value =
                factorySettings.processEntities;
            yield return null;

            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityProcessing.Value == true);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() != null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
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
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityProcessing.Value == true);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() != null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator GeometryVisionSwitchingToEntitiesWorksWhenCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
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
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityProcessing.Value == true);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() != null);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingParentComponentGetsAddedWithoutFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
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

            Assert.True(geoVision.GetComponent<GeometryTargetingSystemsContainer>() != null);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingParentComponentGetsAddedAfterCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
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


            Assert.True(geoVision.GetComponent<GeometryTargetingSystemsContainer>() != null);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingSystemGetsAddedWithoutFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            GameObject geoVision = new GameObject("geoTesting");
            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().GameObjectBasedProcessing.Value =
                factorySettings.processGameObjects;            
            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value =
                factorySettings.processEntities;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 2;

            Measure.Method(() =>
            {
                AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                    .GetTargetingProgramsCount();
            }).Run();
            
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingSystemGetsAddedAfterAfterCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))] string scenePath) {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;

            int amountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 2;//1 targeting program for game objects and 1 for entities

            amountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>().GetTargetingProgramsCount();


            Assert.AreEqual(expectedObjectCount1, amountOfTargetingSystemsRegistered);
        }
    }
}