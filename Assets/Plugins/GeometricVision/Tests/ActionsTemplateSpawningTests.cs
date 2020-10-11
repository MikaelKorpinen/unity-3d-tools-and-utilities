using System;
using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Interfaces;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests
{
    public class ActionsTemplateSpawningTests : MonoBehaviour
    {
        private const string version = TestSettings.Version;
        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processGameObjects = true,
            processEntities = true,
            defaultTargeting = true,
            defaultTag = "",
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator TargetingActionsTemplateIsAssignedThroughFactory(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
            newActions.name += "actionsTemplateFromTestScript_" + 0;
            factorySettings.actionsTemplateObject = newActions;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f),
                new GeometryVisionFactory(factorySettings));
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            Assert.True(geoVisionComponent.TargetingInstructions.Count == 1);
            Assert.True(geoVisionComponent.TargetingInstructions[0].TargetingActions.name ==
                        newActions.name);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator NothingIsSpawnedIfNothingIsSet(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath, 1f);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
            newActions.name += "actionsTemplateFromTestScript_" + 0;
            factorySettings.actionsTemplateObject = newActions;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f),
                new GeometryVisionFactory(factorySettings));
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            geoVisionComponent.TriggerTargetingActions();
            //Check if there is a spawn
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfStartingEffect) == null);
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfMainEffect) == null);
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfEndEffect) == null);
            //Wait frame and check again, if there is a spawn
            yield return null;
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfStartingEffect) == null);
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfMainEffect) == null);
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfEndEffect) == null);
            //Wait few more frames just in case the coroutines haven't finished and check again, if there is a spawn
            yield return null;
            yield return null;
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfStartingEffect.ToString()) == null);
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfMainEffect.ToString()) == null);
            Assert.True(GameObject.Find(GeometryVisionSettings.NameOfEndEffect.ToString()) == null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator ThreeObjectsAreSpawnedAndDeSpawnedSimultaneously(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath, 1f);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
            newActions.name += "actionsTemplateFromTestScript_" + 0;
            float durationOfSpawn = 1f;
            ConfigureActionsTemplateObjectForSpawn(newActions, durationOfSpawn, 0f);
            yield return null;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f),
                new GeometryVisionFactory(factorySettings));
            yield return null;


            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            geoVisionComponent.TriggerTargetingActions();
            yield return null;
            var startObject = GameObject.Find(GeometryVisionSettings.NameOfStartingEffect);
            var mainObject = GameObject.Find(GeometryVisionSettings.NameOfMainEffect);
            var endObject = GameObject.Find(GeometryVisionSettings.NameOfEndEffect);

            //Wait few more frames just in case the coroutines haven't finished and check again, if there is a spawn
            yield return null;
            Assert.True(startObject != null);
            Assert.True(mainObject != null);
            Assert.True(endObject != null);
            float totalTime = 0;
            //Check objects are spawned
            do
            {
                startObject = GameObject.Find(GeometryVisionSettings.NameOfStartingEffect);
                mainObject = GameObject.Find(GeometryVisionSettings.NameOfMainEffect);
                endObject = GameObject.Find(GeometryVisionSettings.NameOfEndEffect);

                yield return null;
                Assert.True(startObject != null);
                Assert.True(mainObject != null);
                Assert.True(endObject != null);
                totalTime += Time.deltaTime;
            } while (totalTime < durationOfSpawn-0.1f);

            //Wait and check if objects are de spawned
            totalTime = 0;
            do
            {
                yield return null;
                totalTime += Time.deltaTime;
            } while (totalTime < 0.3f);

            startObject = GameObject.Find(GeometryVisionSettings.NameOfStartingEffect);
            mainObject = GameObject.Find(GeometryVisionSettings.NameOfMainEffect);
            endObject = GameObject.Find(GeometryVisionSettings.NameOfEndEffect);
            yield return null;
            Assert.True(startObject == null);
            Assert.True(mainObject == null);
            Assert.True(endObject == null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsEmptyScene))]
        public IEnumerator SpawnDelayWorks(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetEmptyScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath, 1f);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var newActions = ScriptableObject.CreateInstance<ActionsTemplateObject>();
            newActions.name += "actionsTemplateFromTestScript_" + 0;
            float durationOfSpawn = 1f;
            float delay = 1f;
            ConfigureActionsTemplateObjectForSpawn(newActions, durationOfSpawn, delay);
            yield return null;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f),
                new GeometryVisionFactory(factorySettings));
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            
            geoVisionComponent.TriggerTargetingActions();
            //Wait for frame just in case the coroutines haven't finished and check again, if there is a spawn
            yield return null;

            var startObject = GameObject.Find(GeometryVisionSettings.NameOfStartingEffect);
            var mainObject = GameObject.Find(GeometryVisionSettings.NameOfMainEffect);
            var endObject = GameObject.Find(GeometryVisionSettings.NameOfEndEffect);

            float totalTime = 0;
            do
            {
                startObject = GameObject.Find(GeometryVisionSettings.NameOfStartingEffect);
                mainObject = GameObject.Find(GeometryVisionSettings.NameOfMainEffect);
                endObject = GameObject.Find(GeometryVisionSettings.NameOfEndEffect);
                yield return null;
                Assert.True(startObject == null);
                Assert.True(mainObject == null);
                Assert.True(endObject == null);
                totalTime += Time.deltaTime;
            } while (totalTime < delay-0.1f);
            
            totalTime = 0;
            do
            {
                //This should get the counter over the delay of 1 second. I think this way of measuring is not accurate
                yield return null;
                totalTime += Time.deltaTime;
            } while (totalTime < 0.1f);

                

            //The delay seems to work on play mode as expected so this is just used to check that there is a spawn delay. 
            totalTime = 0;
            do
            {
                startObject = GameObject.Find(GeometryVisionSettings.NameOfStartingEffect);
                mainObject = GameObject.Find(GeometryVisionSettings.NameOfMainEffect);
                endObject = GameObject.Find(GeometryVisionSettings.NameOfEndEffect);
                yield return null;
                Assert.True(startObject != null);
                Assert.True(mainObject != null);
                Assert.True(endObject != null);
                totalTime += Time.deltaTime;
            } while (totalTime < durationOfSpawn -0.1f);
        }

        private void ConfigureActionsTemplateObjectForSpawn(ActionsTemplateObject newActions, float durationOfSpawn,
            float delay)
        {
            factorySettings.actionsTemplateObject = newActions;
            factorySettings.actionsTemplateObject.StartActionObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            factorySettings.actionsTemplateObject.StartActionEnabled = true;
            factorySettings.actionsTemplateObject.StartDelay = delay;
            factorySettings.actionsTemplateObject.StartDuration = durationOfSpawn;
            factorySettings.actionsTemplateObject.MainActionObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            factorySettings.actionsTemplateObject.MainActionEnabled = true;
            factorySettings.actionsTemplateObject.MainActionDelay = delay;
            factorySettings.actionsTemplateObject.MainActionDuration = durationOfSpawn;
            factorySettings.actionsTemplateObject.EndActionObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            factorySettings.actionsTemplateObject.EndActionEnabled = true;
            factorySettings.actionsTemplateObject.EndDelay = delay;
            factorySettings.actionsTemplateObject.EndDuration = durationOfSpawn;
        }
    }
}