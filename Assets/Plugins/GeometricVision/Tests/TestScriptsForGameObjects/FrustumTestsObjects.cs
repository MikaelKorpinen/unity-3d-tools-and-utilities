using System;
using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForGameObjects
{
    public class FrustumTestsObjects
    {
        private const string version = TestSettings.Version;

        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;

        GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processGameObjects = true,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        /// <summary>
        /// Test transform culling. Transform that may or may not have renderer/mesh/bounding box in it
        /// </summary>
        /// <param name="scenePath"></param>
        /// <returns></returns>
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator GameObjectIsPickedIfInsideFrustum(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            yield return null;
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;

            var geoEye = geoVision.GetComponent<GeometryVision>().GetEye<GeometryVisionEye>();
            //+1 from geovision manager/runner that is added later
            Assert.True(geoEye.seenTransforms.Count == expectedObjectCount + 1);
        }

        /// <summary>
        /// Test transform culling. Transform that may or may not have renderer/mesh/bounding box in it
        /// </summary>
        /// <param name="scenePath"></param>
        /// <returns></returns>
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator GameObjectIsRemovedAndAddedBackIfOutsideFrustum(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 25; i++)
            {
                yield return null;
            }

            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            int expectedObjectCount2 = 0;
            int expectedObjectCount3 = expectedObjectCount;

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            var geoEye = geoVisionComponent.GetEye<GeometryVisionEye>();
            Assert.True(geoEye.seenTransforms.Count >= expectedObjectCount &&
                        geoEye.seenTransforms.Count <= expectedObjectCount + 1);

            geoVision.transform.position = new Vector3(10f, 10f, 10); //Move Object outside the cube
            yield return null;

            Assert.True(geoEye.seenTransforms.Count >= expectedObjectCount2 &&
                        geoEye.seenTransforms.Count <= expectedObjectCount2 + 1);

            geoVision.transform.position = new Vector3(0f, 0f, -6f); //Move Object back to the cube
            yield return null;
            Assert.True(geoEye.seenTransforms.Count >= expectedObjectCount3 &&
                        geoEye.seenTransforms.Count <= expectedObjectCount3 + 1);
        }
    }
}