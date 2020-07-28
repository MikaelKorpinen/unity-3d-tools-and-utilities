using System.Collections;
using UnityEngine;
using System;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class CreationalTestsEntities : MonoBehaviour
    {
        private const string version = TestSettings.Version;

        private readonly GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processGameObjects = false,
            processGameObjectsEdges = false,
            processEntities = true,
            defaultTargeting = true
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator GeometryVisionGetsCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            IGeoProcessor processor = geoVision.GetComponent<GeometryVision>().Head
                .GetProcessor<GeometryVisionEntityProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();
            yield return null;

            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.True(amountOfObjectsInScene > 0);
        }


        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator TestSceneGetsLoadedForEntities(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
        [Timeout(TestSettings.defaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator NoEntitiesProcessorIfEntitiesDisabledWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = false;
            yield return null;

            var processor = geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionEntityProcessor>();
            yield return null;

            Assert.True(processor == null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator GeometryVisionGetsCreatedWithoutFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = true;
            yield return null;
            int amountOfObjectsInScene = 0;

            amountOfObjectsInScene = geoVision.GetComponent<GeometryVision>().Head
                .GetProcessor<GeometryVisionEntityProcessor>().CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.True(amountOfObjectsInScene > 0);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator NoEntityProcessorIfDisabledWithoutFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = false;
            yield return null;

            var processor = geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionEntityProcessor>();
            yield return null;

            Assert.True(processor == null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithoutFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = true;
            yield return null;
            int amountOfObjectsInScene = 0;

            amountOfObjectsInScene = geoVision.GetComponent<GeometryVision>().Head
                .GetProcessor<GeometryVisionEntityProcessor>().CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != "");
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() == null);
            Assert.True(geometryVision.Camera1 != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Head != null);
            Assert.True(geometryVision.Head.GeoVisions != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == false);
            Assert.True(geometryVision.EntityBasedProcessing.Value == true);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            Assert.True(geometryVision.Id != "");
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() == null);
            Assert.True(geometryVision.Camera1 != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Head != null);
            Assert.True(geometryVision.Head.GeoVisions != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == false);
            Assert.True(geometryVision.EntityBasedProcessing.Value == true);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator GeometryVisionSwitchingToEntitiesWorksWhenCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
            string scenePath)
        {
            TestUtilities.SetupBuildSettings(scenePath);
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            int amountOfObjectsInScene = 0;

            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = true;
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != "");
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() == null);
            Assert.True(geometryVision.Camera1 != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Head != null);
            Assert.True(geometryVision.Head.GeoVisions != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Head.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == false);
            Assert.True(geometryVision.EntityBasedProcessing.Value == true);
        }
        
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator ToggleGameObjectBasedProcessingWorks(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            geometryVisionComponent.GameObjectBasedProcessing.Value = false;
            geometryVisionComponent.EntityBasedProcessing.Value = factorySettings.processEntities;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 1);
            Assert.True(geometryVisionComponent.Head.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVisionComponent.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVisionComponent.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVisionComponent.GetEye<GeometryVisionEye>() == null);
            geometryVisionComponent.EntityBasedProcessing.Value = false;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 0);
            Assert.True(geometryVisionComponent.Head.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVisionComponent.Head.GetProcessor<GeometryVisionEntityProcessor>() == null);
            geometryVisionComponent.EntityBasedProcessing.Value = true;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 1);
            Assert.True(geometryVisionComponent.Head.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVisionComponent.Head.GetProcessor<GeometryVisionEntityProcessor>() != null);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator TargetingParentComponentGetsAddedAfterAddingEyeComponent(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            GameObject geoVision = new GameObject("geoTesting");
            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = true;
            yield return null;

            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() =>
            {
                AmountOfTargetingSystemsRegistered =
                    geoVision.GetComponents<GeometryTargetingSystemsContainer>().Length;
            }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator TargetingParentComponentGetsAddedAfterCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
            string scenePath)
        {
            TestUtilities.SetupBuildSettings(scenePath);
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() =>
            {
                AmountOfTargetingSystemsRegistered =
                    geoVision.GetComponents<GeometryTargetingSystemsContainer>().Length;
            }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator TargetingSystemGetsAddedOnStart(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            GameObject geoVision = new GameObject("geoTesting");
            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = true;
            yield return null;

            int AmountOfTargetingSystemsRegistered = 0;
            int expectedSystemsCount = 1;
            AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>().GetTargetingProgramsCount();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedSystemsCount, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator TargetingSystemGetsAddedAfterAfterCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))]
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
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
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