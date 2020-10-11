using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForGameObjects
{
    [TestFixture]
    public class FrustumTestsGeometry
    {
        private const string version = TestSettings.Version;
         
        private readonly GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = true,
            edgesTargeted = false,
        };

        
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }
        
        /// <summary>
        /// Test for the renderer based search works
        /// </summary>
        /// <param name="scenePath"></param>
        /// <returns></returns>
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator ObjectWithRendererIsPickedIfInsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            int expectedObjectCount = GameObject.FindObjectsOfType<Renderer>().Length;
            
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            geoVision.GetComponent<GeometryVision>().UseBounds = true;
            yield return null;
            yield return null;
            
            Assert.AreEqual(expectedObjectCount, geoVision.GetComponent<GeometryVisionEye>().SeenGeoInfos.Count);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator ObjectWithRendererIsRemovedAndAddedBackIfOutsideFrustum(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            int expectedObjectCount1 = GameObject.FindObjectsOfType<Renderer>().Length;

            int expectedObjectCount2 = 0;
            int expectedObjectCount3 = expectedObjectCount1;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            geoVision.GetComponent<GeometryVision>().UseBounds = true;
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
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator ObjectWithRendererIsRemovedIfOutsideFrustum(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            int expectedObjectCount = 0;
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            geoVision.GetComponent<GeometryVision>().UseBounds = true;
            yield return null;
            geoVision.transform.position = new Vector3(10f, 10f, 10f);
            yield return null;
            
            Assert.AreEqual(expectedObjectCount, geoVision.GetComponent<GeometryVisionEye>().SeenGeoInfos.Count);
        }
    }
}