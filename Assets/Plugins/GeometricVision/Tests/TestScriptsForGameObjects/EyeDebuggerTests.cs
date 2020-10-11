using System.Collections;
using System.Linq;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Utilities;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision.Tests.TestScriptsForGameObjects
{
    /// <summary>
    /// Class tests debugger that is used for showing edges and else.
    /// If the tests fail it could be because of updating unity or something else that affect the view frustum calculations of the unity camera 
    /// </summary>
    [TestFixture]
    public class EyeDebuggerTests
    {
        //TODO:Uncomment this for version 2.0
        //Tests pass with vr camera. The frustum is different so need to implement separate way to tests edges
        /*
        private const string version = TestSettings.Version;
        
        private readonly GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = true,
            processGameObjectsEdges = false,
            edgesTargeted = true
        };
        
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }
        
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator EyeDebuggerReceivesEdges(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);

            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            var expectedEdgeCount = testCubeCount * (12 + 6);
            var cube = GameObject.Find("Cube");
            yield return null;
            cube.transform.position = Vector3.zero;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(-2.33f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            GeometryDataModels.Edge[] edges;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            var geoEye = geoVisionComponent.GetEye<GeometryVisionEye>();
            /////Put camera at position where it can only see text cube 3d model partially
            var position = new Vector3(-0.06f, 0.352f, -12.0f);
            //Need to wait till update loop finishes for frustum to update. On windows machines not happen as fast as on Linux for some reason.
            yield return null;

            var visibleEdgeCount =
                GetTestResultsFromPositionMultiThreaded(geoVision, geoEye, cube, out edges, position);
            yield return null;
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator GetEdgesOnPartiallyVisibleCubeReturns3EdgesMultiThreaded(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);

            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            var expectedEdgeCount = testCubeCount * 5;
            var cube = GameObject.Find("Cube");
            yield return null;
            cube.transform.position = Vector3.zero;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(-2.33f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            GeometryDataModels.Edge[] edges;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            var geoEye = geoVisionComponent.GetEye<GeometryVisionEye>();
            /////Put camera at position where it can only see text cube 3d model partially(3 edges of side of the cube)
            var position = new Vector3(-2.33f, 0.352f, -6f);
            var visibleEdgeCount =
                GetTestResultsFromPositionMultiThreaded(geoVision, geoEye, cube, out edges, position);
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator CubeReturnsCorrectEdgesWhenMovingCameraBackAndForth(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))]
            string scenePath)
        {

            TestUtilities.SetupScene(scenePath);

            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            var expectedEdgeCount = testCubeCount * 5;
            var cube = GameObject.Find("Cube");
            yield return null;

            yield return null;
            cube.transform.position = Vector3.zero;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(-2.33f, 0.352f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            var geoEye = geoVisionComponent.GetEye<GeometryVisionEye>();
            /////Put camera at starting position so it can see the 3d model.
            GeometryDataModels.Edge[] edges = new GeometryDataModels.Edge[0];
            var position = new Vector3(-2.33f, 0.352f, -6f);
            yield return null;
            var visibleEdgeCount = GetTestResultsFromPosition(geoVision, geoEye, out edges, position);
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
            
            /////Move the eye so it sees the whole cube
            expectedEdgeCount = testCubeCount * (12 + 6); //corner edges + triangulated edges
            position = new Vector3(-0.06f, 0.352f, -12.0f);
            yield return null;
            visibleEdgeCount = GetTestResultsFromPosition(geoVision, geoEye, out edges, position);
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);

            //Move the eye back so it only sees 3 edges
            expectedEdgeCount = testCubeCount * 5;
            position = new Vector3(-2.33f, 0.352f, -6f);
            yield return null;
            visibleEdgeCount = GetTestResultsFromPosition(geoVision, geoEye, out edges, position);

            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
        }

        private int GetTestResultsFromPosition(GameObject geoVision, GeometryVisionEye geoEye,
            out GeometryDataModels.Edge[] edges, Vector3 position)
        {
            edges = new GeometryDataModels.Edge[0];
            var edgesT = edges;
            geoVision.transform.position = position;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            geoVisionComponent.RegenerateVisionArea(25);
            geoVisionComponent.Runner.GetProcessor<GeometryVisionProcessor>().CheckSceneChanges(geoEye.GeoVision);
            MeshUtilities.UpdateEdgesVisibility(geoVision.GetComponent<GeometryVision>().Planes, geoEye.SeenGeoInfos);
            var visibleEdgeCount = 0;
            Measure.Method(() =>
            {
                geoVisionComponent.Runner.EyeDebugger.Debug(geoEye);
                visibleEdgeCount = geoVisionComponent.Runner.EyeDebugger.AmountOfSeenEdges;
                geoVisionComponent.Runner.EyeDebugger.AmountOfSeenEdges = 0;
            }).Run();


            return visibleEdgeCount;
        }

        private static int GetTestResultsFromPositionMultiThreaded(GameObject geoVision, GeometryVisionEye geoEye,
            GameObject cube, out GeometryDataModels.Edge[] edges, Vector3 position)
        {
            edges = new GeometryDataModels.Edge[0];
            var edgesT = edges;
            geoVision.transform.position = position;
            var geoVis = geoVision.GetComponent<GeometryVision>();
            geoVis.RegenerateVisionArea(25);
            var renderer = cube.GetComponent<Renderer>();
            MeshUtilities.UpdateEdgesVisibilityParallel(geoVis.Planes, geoEye.SeenGeoInfos);
            geoEye.DebugMode = true;
            var visibleEdgeCount = 0;
            
            Measure.Method(() =>
            {
                geoVis.Runner.EyeDebugger.Debug(geoEye);
                visibleEdgeCount = geoVis.Runner.EyeDebugger.AmountOfSeenEdges;
                geoVis.Runner.EyeDebugger.AmountOfSeenEdges = 0;
            }).Run();

            return visibleEdgeCount;
        }
        */
    }
}