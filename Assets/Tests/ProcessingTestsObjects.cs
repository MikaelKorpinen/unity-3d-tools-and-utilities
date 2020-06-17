using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class ProcessingTestsObjects
    {
        private const string version = TestSettings.Version;
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
        
        [TearDown]
        public void TearDown()
        {
            //Put original scenes back to build settings
            EditorBuildSettings.scenes = factoryAndOriginalScenes.Item2;
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator SceneObjectCountMatchesTheCountedValue([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoTypesToTarget = new List<GeometryType>();
            geoTypesToTarget.Add(GeometryType.Objects_);
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25, geoTypesToTarget, 0);
            yield return null;
            yield return null;
            int amountOfObjectsInScene = 0;
            int expectedObjectCount1 = TestUtilities.GetObjectCountFromScene();
            Measure.Method(() => { amountOfObjectsInScene = geoVision.GetComponent<GeometryVisionEye>().ControllerBrain.CountSceneObjects(); }).Run();

            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator SceneObjectCountMatchesTheCountedValueWithUnityFindObjects([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            int expectedObjectCount1 = GameObject.FindObjectsOfType<GameObject>().Length; 
            int amountOfObjectsInScene123 = 0;
            Measure.Method(() => { amountOfObjectsInScene123 = GameObject.FindObjectsOfType<GameObject>().Length; })
                .Run();

            Debug.Log("total objects: " + amountOfObjectsInScene123);
            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene123);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator SceneObjectCountMatchesTheCountedValueWithUnityFindTransformsInChildren(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
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
            
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator GetTransformReturnsCorrectAmountOfObjects([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            int expectedObjectCount1 = GameObject.FindObjectsOfType<GameObject>().Length;
            var geoTypesToTarget = new List<GeometryType>();
            geoTypesToTarget.Add(GeometryType.Objects_);
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25, geoTypesToTarget, 0);
            yield return null;
            List<GameObject> rootGameObjects = new List<GameObject>();
            HashSet<Transform> result = new HashSet<Transform>();
            SceneManager.GetActiveScene().GetRootGameObjects(rootGameObjects);

            Measure.Method(() =>
            {
                result = geoVision.GetComponent<GeometryVisionEye>().ControllerBrain.GetTransforms(rootGameObjects);
            }).Run();
            
            Debug.Log("total objects: " + result.Count);
            Assert.AreEqual(expectedObjectCount1, result.Count);
        }
    }
}