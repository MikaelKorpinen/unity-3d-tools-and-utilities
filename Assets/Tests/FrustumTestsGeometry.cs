﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces.Implementations;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    [TestFixture]
    public class FrustumTestsGeometry
    {
        private const string version = TestSettings.Version;
 
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsGameObjects))]
        public IEnumerator ObjectWithRendererIsPickedIfInsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForGameObjectsFromPath))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            int expectedObjectCount = GameObject.FindObjectsOfType<Renderer>().Length;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(), false);

            yield return null;
            yield return null;
            
            Assert.AreEqual(expectedObjectCount, geoVision.GetComponent<GeometryVisionEye>().SeenGeoInfos.Count);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsGameObjects))]
        public IEnumerator ObjectWithRendererIsRemovedAndAddedBackIfOutsideFrustum(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForGameObjectsFromPath))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            int expectedObjectCount1 = GameObject.FindObjectsOfType<Renderer>().Length;

            int expectedObjectCount2 = 0;
            int expectedObjectCount3 = expectedObjectCount1;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(), false);
            
            yield return null;
            yield return null;
            Assert.AreEqual(expectedObjectCount1, geoVision.GetComponent<GeometryVisionEye>().SeenGeoInfos.Count);

            geoVision.transform.position = new Vector3(10f, 10f, 10); //Move Object outside the cube
            yield return null;
            yield return null;
            Assert.AreEqual(expectedObjectCount2, geoVision.GetComponent<GeometryVisionEye>().SeenGeoInfos.Count);

            geoVision.transform.position = new Vector3(0f, 0f, -6f);
            yield return null;
            yield return null;
            Assert.AreEqual(expectedObjectCount3, geoVision.GetComponent<GeometryVisionEye>().SeenGeoInfos.Count);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsGameObjects))]
        public IEnumerator ObjectWithRendererIsRemovedIfOutsideFrustum(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForGameObjectsFromPath))] string scenePath)
        {
            int expectedObjectCount = 0;
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(), false);
            yield return null;
            geoVision.transform.position = new Vector3(10f, 10f, 10f);
            yield return null;
            
            Assert.AreEqual(expectedObjectCount, geoVision.GetComponent<GeometryVisionEye>().SeenGeoInfos.Count);
        }
    }
}