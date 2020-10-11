using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Unity.Entities;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForEntities
{
    public class CreationalTestsEntities : MonoBehaviour
    {
        private const string version = TestSettings.Version;

        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processGameObjects = false,
            processEntities = true,
            defaultTargeting = true,
            edgesTargeted =false
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
            TestUtilities.CleanUpEntities();
            factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView = 25f,
                processGameObjects = false,
                processEntities = true,
                defaultTargeting = true,
                edgesTargeted =false
            };
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
            IGeoProcessor processor = geoVision.GetComponent<GeometryVision>().Runner
                .GetProcessor<GeometryVisionEntityProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();
            yield return null;

            Assert.True(amountOfObjectsInScene > 0);
        }


        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator TestSceneGetsLoadedForEntities(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))]
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
        public IEnumerator NoEntitiesProcessorIfEntitiesDisabledWithFactory(
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
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            geoVisionComponent.EntityProcessing.Value = false;
            yield return null;
            
            var processor =geoVisionComponent.Runner.GetProcessor<GeometryVisionEntityProcessor>();
            yield return null;

            Assert.True(processor == null);
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
            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value = true;
            yield return null;
            int amountOfObjectsInScene = 0;

            amountOfObjectsInScene = geoVision.GetComponent<GeometryVision>().Runner
                .GetProcessor<GeometryVisionEntityProcessor>().CountSceneObjects();
            yield return null;
            Assert.True(amountOfObjectsInScene > 0);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator NoEntityProcessorIfDisabledWithoutFactory(
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
            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value = false;
            yield return null;

            var processor = geoVision.GetComponent<GeometryVision>().Runner
                .GetProcessor<GeometryVisionEntityProcessor>();
            yield return null;

            Assert.True(processor == null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithoutFactory(
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

            //TODO: Find out how to measure stuff happening in the start and awake from here to get the real estimate
            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value = true;
            yield return null;
            int amountOfObjectsInScene = 0;

            amountOfObjectsInScene = geoVision.GetComponent<GeometryVision>().Runner
                .GetProcessor<GeometryVisionEntityProcessor>().CountSceneObjects();
            yield return null;
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() == null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVision.Runner.Processors.Count == 1);
            Assert.True(geometryVision.Eyes.Count == 1);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == false);
            Assert.True(geometryVision.EntityProcessing.Value == true);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithFactory(
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

            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() == null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == false);
            Assert.True(geometryVision.EntityProcessing.Value == true);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator GeometryVisionSwitchingToEntitiesWorksWhenCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))]
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

            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value = true;
            yield return null;
            var geometryVision = geoVision.GetComponent<GeometryVision>();
            Assert.True(geometryVision != null);
            Assert.True(geometryVision.Id != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVision.GetEye<GeometryVisionEye>() == null);
            Assert.True(geometryVision.HiddenUnityCamera != null);
            Assert.True(geometryVision.TargetingInstructions != null);
            Assert.True(geometryVision.Runner != null);
            Assert.True(geometryVision.Runner.GeoVisions != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVision.Runner.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVision.GameObjectBasedProcessing.Value == false);
            Assert.True(geometryVision.EntityProcessing.Value == true);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator ToggleGameObjectBasedProcessingWorks(
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
            geometryVisionComponent.GameObjectBasedProcessing.Value = false;
            geometryVisionComponent.EntityProcessing.Value = factorySettings.processEntities;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 1);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
            Assert.True(geometryVisionComponent.GetEye<GeometryVisionEntityEye>() != null);
            Assert.True(geometryVisionComponent.GetEye<GeometryVisionEye>() == null);
            geometryVisionComponent.EntityProcessing.Value = false;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 0);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionEntityProcessor>() == null);
            geometryVisionComponent.EntityProcessing.Value = true;
            yield return null;
            Assert.True(geometryVisionComponent.Eyes.Count == 1);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>() == null);
            Assert.True(geometryVisionComponent.Runner.GetProcessor<GeometryVisionEntityProcessor>() != null);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator TargetingParentComponentGetsAddedAfterAddingEyeComponent(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            GameObject geoVision = new GameObject("geoTesting");
            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value = true;
            yield return null;

            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() =>
            {
                AmountOfTargetingSystemsRegistered =
                    geoVision.GetComponents<GeometryTargetingSystemsContainer>().Length;
            }).Run();

            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator TargetingParentComponentGetsAddedAfterCreatedWithFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))]
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

            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator TargetingSystemGetsAddedOnStart(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            GameObject geoVision = new GameObject("geoTesting");
            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().EntityProcessing.Value = true;
            yield return null;

            int AmountOfTargetingSystemsRegistered = 0;
            int expectedSystemsCount = 1;
            AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                .GetTargetingProgramsCount();

            Assert.AreEqual(expectedSystemsCount, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator TargetingSystemGetsAddedAfterAfterCreatedWithFactory(
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
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() =>
            {
                AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                    .GetTargetingProgramsCount();
            }).Run();

            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }
    }
}