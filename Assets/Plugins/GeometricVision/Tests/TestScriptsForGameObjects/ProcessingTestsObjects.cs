using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForGameObjects
{
    public class ProcessingTestsObjects
    {
        private const string version = TestSettings.Version;
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
        
        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = true,
 
        };
        
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator SceneObjectCountMatchesTheCountedValue([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;
            int amountOfObjectsInScene = 0;
            int expectedObjectCount1 = TestUtilities.GetObjectCountFromScene();
            Measure.Method(() => { amountOfObjectsInScene = geoVision.GetComponent<GeometryVision>().Runner.GetProcessor<GeometryVisionProcessor>().CountSceneObjects(); }).Run();
            
            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator SceneObjectCountMatchesTheCountedValueWithUnityFindObjects([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            int expectedObjectCount1 = GameObject.FindObjectsOfType<GameObject>().Length; 
            int amountOfObjectsInScene123 = 0;
            Measure.Method(() => { amountOfObjectsInScene123 = GameObject.FindObjectsOfType<GameObject>().Length; })
                .Run();

            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene123);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator SceneObjectCountMatchesTheCountedValueWithUnityFindTransformsInChildren(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupBuildSettings(scenePath);
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            int expectedObjectCount1 = TestUtilities.GetObjectCountFromScene();
            int amountOfObjectsInScene = 0;
            List<GameObject> rootObjects = new List<GameObject>();
            SceneManager.GetActiveScene().GetRootGameObjects(rootObjects);
            Measure.Method(() =>
            {
                amountOfObjectsInScene = 0;
                foreach (var root in rootObjects)
                {
                    amountOfObjectsInScene += root.GetComponentsInChildren<Transform>().Length;
                }
            }).Run();
            
            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator GetTransformReturnsCorrectAmountOfObjects([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

           
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;

            List<GameObject> rootGameObjects = new List<GameObject>();
            HashSet<Transform> result = new HashSet<Transform>();
            SceneManager.GetActiveScene().GetRootGameObjects(rootGameObjects);
            int expectedObjectCount1 = TestUtilities.GetObjectCountFromScene();
            Measure.Method(() =>
            {
                result= geoVision.GetComponent<GeometryVision>().Runner.GetProcessor<GeometryVisionProcessor>().GetTransformsFromRootObjects(rootGameObjects, result);
            }).Run();
            
            Assert.AreEqual(expectedObjectCount1, result.Count);
        }
    }
}