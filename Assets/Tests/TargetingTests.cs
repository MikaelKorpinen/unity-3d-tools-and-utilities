using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class TargetingTests 
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
        public IEnumerator TargetingSystemGetsAddedIfTargetingEnabled([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes, false);
            yield return null;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() => { AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>().TargetingPrograms.Count; }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }
    }
}
