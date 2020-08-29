using System.Collections;
using UnityEngine;
using System;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class CreationalTestsGameObjectsAndEntitiesSimultaneously : MonoBehaviour
    {
        private const string version = TestSettings.Version;
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;

        private readonly GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processGameObjects = true,
            processEntities = true,
            processGameObjectsEdges = false,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
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

            var geoVision = TestUtilities.SetupGeoVision2(new Vector3(0f, 0f, -6f),
                new GeometryVisionFactory(factorySettings), false);
            yield return null;

            Debug.Log("Scenepath: " + scenePath);
            Debug.Log("Active scene: " + SceneManager.GetActiveScene().name);
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
                geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionProcessor>();
            IGeoProcessor processorEntities = geoVision.GetComponent<GeometryVision>().Head
                .GetProcessor<GeometryVisionEntityProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();

            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
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
            geometryVisionComponent.GameObjectBasedProcessing.Value = factorySettings.processGameObjects;
            geometryVisionComponent.EntityBasedProcessing.Value = factorySettings.processEntities;
            yield return null;
            int amountOfObjectsInScene = 0;
            IGeoProcessor processor =
                geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionProcessor>();
            IGeoProcessor processorEntities = geoVision.GetComponent<GeometryVision>().Head
                .GetProcessor<GeometryVisionEntityProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
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
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value =
                factorySettings.processEntities;
            yield return null;

            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != "");
            Assert.True(geometryVision.Camera1 != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Head != null);
            Assert.True(geometryVision.Head.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityBasedProcessing.Value == true);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionProcessor>() != null);
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
            Assert.True(geometryVision.Id != "");
            Assert.True(geometryVision.Camera1 != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Head != null);
            Assert.True(geometryVision.Head.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityBasedProcessing.Value == true);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionProcessor>() != null);
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
            int amountOfObjectsInScene = 0;
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != "");
            Assert.True(geometryVision.Camera1 != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Head != null);
            Assert.True(geometryVision.Head.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityBasedProcessing.Value == true);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionProcessor>() != null);
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
        public IEnumerator TargetingSystemGetsAddedOnlyWithoutFactory(
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
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value =
                factorySettings.processEntities;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 2;

            Measure.Method(() =>
            {
                AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                    .GetTargetingProgramsCount();
            }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingSystemGetsAddedAfterAfterCreatedWithFactory(
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
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 2;//1 targeting program for gameobjects and 1 for entities
            Measure.Method(() =>
            {
                AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                    .GetTargetingProgramsCount();
            }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }
    }
}