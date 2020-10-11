using System;
using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForGameObjects
{
    public class CreationalTests : MonoBehaviour
    {
        private const string version = TestSettings.Version;
        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processGameObjects = true,
            processEntities = false,
            defaultTargeting = true
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TestSceneGetsLoadedForGameObjects(
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
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator GeometryVisionGetsCreatedWithFactory(
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
            IGeoProcessor processor =
                geoVision.GetComponent<GeometryVision>().Runner.GetProcessor<GeometryVisionProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();
            yield return null;

            Assert.True(amountOfObjectsInScene > 0);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator GeometryVisionGetsCreatedWithoutFactory(
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

            geoVision.AddComponent<GeometryVision>();
            var geometryVisionComponent = geoVision.GetComponent<GeometryVision>();
            geometryVisionComponent.GameObjectBasedProcessing.Value = factorySettings.processGameObjects;
            yield return null;
            int amountOfObjectsInScene = 0;
            var processor = geometryVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();
            yield return null;

            Assert.True(amountOfObjectsInScene > 0);
        }
        
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator ToggleGameObjectBasedProcessingWorks(
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

            geoVision.AddComponent<GeometryVision>();
            var geometryVisionComponent = geoVision.GetComponent<GeometryVision>();
            geometryVisionComponent.GameObjectBasedProcessing.Value = factorySettings.processGameObjects;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 1);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>() != null);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionEntityProcessor>() == null);
            geometryVisionComponent.GameObjectBasedProcessing.Value = false;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 0);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionEntityProcessor>() == null);
            geometryVisionComponent.GameObjectBasedProcessing.Value = true;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 1);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>() != null);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionEntityProcessor>() == null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator HasBasicComponentsWhenCreatedWithoutFactory(
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
            yield return null;
            
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.TargetingInstructions.Count != 0);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityProcessing.Value == false);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() == null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() == null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() != null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator HasBasicComponentsWhenCreatedWithFactory(
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
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            yield return null;
            amountOfObjectsInScene =
                geoVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>().CountSceneObjects();
            yield return null;

            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.TargetingInstructions.Count != 0);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityProcessing.Value == false);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() == null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() == null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() != null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator SwitchingToEntitiesWorksWhenCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
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

            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value = true;
            yield return null;

            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.TargetingInstructions.Count != 0);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == true);
            Assert.True(geometryVision.EntityProcessing.Value == true);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() != null);
            factorySettings.processEntities = false;
        }

        [UnityTest, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingParentComponentGetsAddedAfterAddingEyeComponent(
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
            yield return null;
            yield return null;
            int AmountOfTargetingSystemsRegistered;
            int expectedObjectCount1 = 1;

            AmountOfTargetingSystemsRegistered = geoVision.GetComponents<GeometryTargetingSystemsContainer>().Length;
            
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
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
            yield return null;
            int amountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() =>
            {
                amountOfTargetingSystemsRegistered =
                    geoVision.GetComponents<GeometryTargetingSystemsContainer>().Length;
            }).Run();
            
            Assert.AreEqual(expectedObjectCount1, amountOfTargetingSystemsRegistered);
        }

        [UnityTest, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingSystemGetsAddedAfterAddingEyeComponent(
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
            yield return null;
            yield return null;
            int amountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            var targs = geoVision.GetComponent<GeometryTargetingSystemsContainer>().GetTargetingProgramsCount();
            amountOfTargetingSystemsRegistered = targs;
            
            Assert.AreEqual(expectedObjectCount1, amountOfTargetingSystemsRegistered);
        }

        [UnityTest, Version(TestSettings.Version)]
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
            int amountOfTargetingSystemsRegistered = 0;
            int expectedSystemCount1 = 1;

            amountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                    .GetTargetingProgramsCount();
            
            Assert.AreEqual(expectedSystemCount1, amountOfTargetingSystemsRegistered);
        }
    }
}