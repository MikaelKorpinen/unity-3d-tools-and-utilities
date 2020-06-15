using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeometricVision;
using GeometricVision.Utilities;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static GeometricVision.GeometryDataModels.Boolean;
using Object = UnityEngine.Object;

namespace Tests
{
    [TestFixture]
    public class GeoUtilitiesTests
    {
        private const string version = TestSettings.Version;
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
        
        [TearDown]
        public void TearDown()
        {
            //Put original scenes back to build settings
            EditorBuildSettings.scenes = factoryAndOriginalScenes.Item2;
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator GetEdgesOnCubeReturns18Edges([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))]
            string scenePath)
        {
            int expectedEdgeCount = 12 + 6; //corner edges + triangulated edges
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25,
                GeometryType.Edges, 0);
            var geoEye = geoVision.GetComponent<GeometryVisionEye>();
            var cube = GameObject.Find("Cube");
            yield return null;
            //Since we are not moving the geoVisionObject we don't need to recalculate planes from camera component
            Assert.AreEqual(expectedEdgeCount,
                MeshUtilities.GetEdgesFromMesh(cube.GetComponent<Renderer>(), cube.GetComponent<MeshFilter>().mesh)
                    .Length);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator NoResultIfNoGeometryTargeted([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))]
            string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);

            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            var expectedEdgeCount = 0;
            var cube = GameObject.Find("Cube");
            yield return null;

            cube.transform.position = Vector3.zero;
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25,
                GeometryType.Objects_, 0);
            yield return null;
            GeometryDataModels.Edge[] edges;
            var geoEye = geoVision.GetComponent<GeometryVisionEye>();

            /////Put camera at position where it can only see text cube 3d model partially
            var position = new Vector3(-0.69f, 0.352f, -4.34f);
            //Need to wait till update loop finishes updating. Most likely issue with slow computers.
            yield return null;

            var visibleEdgeCount =
                GetTestResultsFromPositionMultiThreaded(geoVision, geoEye, cube, out edges, position);
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator GetEdgesOnPartiallyVisibleCubeReturnsRightAmountOfEdges(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))]
            string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);

            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            var expectedEdgeCount = testCubeCount * 3;
            var cube = GameObject.Find("Cube");
            yield return null;
            cube.transform.position = Vector3.zero;
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25,
                GeometryType.Edges, 0);
            yield return null;
            GeometryDataModels.Edge[] edges;
            var geoEye = geoVision.GetComponent<GeometryVisionEye>();

            /////Put camera at position where it can only see text cube 3d model partially
            var position = new Vector3(-0.69f, 0.352f, -4.34f);
            //Need to wait till update loop finishes for frustum to update. On windows machines not happen as fast as on Linux for some reason.
            yield return null;

            var visibleEdgeCount =
                GetTestResultsFromPositionMultiThreaded(geoVision, geoEye, cube, out edges, position);
            yield return null;
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator GetEdgesOnPartiallyVisibleCubeReturns3EdgesMultiThreaded(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))]
            string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);

            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            var expectedEdgeCount = testCubeCount * 3;
            var cube = GameObject.Find("Cube");
            yield return null;
            cube.transform.position = Vector3.zero;
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25,
                GeometryType.Edges, 0);
            yield return null;
            GeometryDataModels.Edge[] edges;
            var geoEye = geoVision.GetComponent<GeometryVisionEye>();

            /////Put camera at position where it can only see text cube 3d model partially(3 edges of side of the cube)
            var position = new Vector3(-0.69f, 0.352f, -4.34f);
            var visibleEdgeCount =
                GetTestResultsFromPositionMultiThreaded(geoVision, geoEye, cube, out edges, position);
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator CubeReturnsCorrectEdgesWhenMovingCameraBackAndForth(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))]
            string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);

            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var testCubes = Object.FindObjectsOfType<GameObject>();
            yield return null;
            var testCubeCount = testCubes.Where(tc => tc.name.Contains("Cube")).ToList().Count;
            var expectedEdgeCount = testCubeCount * 3;
            var cube = GameObject.Find("Cube");
            yield return null;

            yield return null;
            cube.transform.position = Vector3.zero;
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25,
                GeometryType.Edges, 0);
            yield return null;
            var geoEye = geoVision.GetComponent<GeometryVisionEye>();

            /////Put camera at starting position so it can see the 3d model.
            GeometryDataModels.Edge[] edges = new GeometryDataModels.Edge[0];
            var position = new Vector3(-0.69f, 0.352f, -4.34f);
            var visibleEdgeCount = GetTestResultsFromPosition(geoVision, geoEye, cube, out edges, position);
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);


            /////Move the eye so it sees the whole cube
            expectedEdgeCount = testCubeCount * (12 + 6); //corner edges + triangulated edges
            position = new Vector3(-0.06f, 0.352f, -12.0f);
            visibleEdgeCount = GetTestResultsFromPosition(geoVision, geoEye, cube, out edges, position);
            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);

            //Move the eye back so it only sees 3 edges
            expectedEdgeCount = testCubeCount * 3;
            position = new Vector3(-0.69f, 0.352f, -4.34f);
            visibleEdgeCount = GetTestResultsFromPosition(geoVision, geoEye, cube, out edges, position);

            Assert.AreEqual(expectedEdgeCount, visibleEdgeCount);
        }

        private static int GetTestResultsFromPosition(GameObject geoVision, GeometryVisionEye geoEye, GameObject cube,
            out GeometryDataModels.Edge[] edges, Vector3 position)
        {
            edges = new GeometryDataModels.Edge[0];
            var edgesT = edges;
            geoVision.transform.position = position;
            geoEye.RegenerateVisionArea(25);
            var renderer = cube.GetComponent<Renderer>();
            Measure.Method(() => { MeshUtilities.UpdateEdgesVisibility(geoEye.Planes, geoEye.SeenGeoInfos); }).Run();
            var visibleEdgeCount = 0;

            foreach (var geo in geoEye.SeenGeoInfos)
            {
                visibleEdgeCount += geo.edges.Where(edge => edge.isVisible == True).ToList().Count;
            }

            return visibleEdgeCount;
        }

        private static int GetTestResultsFromPositionMultiThreaded(GameObject geoVision, GeometryVisionEye geoEye,
            GameObject cube, out GeometryDataModels.Edge[] edges, Vector3 position)
        {
            edges = new GeometryDataModels.Edge[0];
            var edgesT = edges;
            geoVision.transform.position = position;
            geoEye.RegenerateVisionArea(25);
            var renderer = cube.GetComponent<Renderer>();

            Measure.Method(() =>
            {
                MeshUtilities.UpdateEdgesVisibilityParallel(geoEye.Planes, geoEye.SeenGeoInfos);
                ///TODO:check visibility for every object
            }).Run();

            return CalculateAmountOfVisibleEdges(geoEye);
        }

        private static int CalculateAmountOfVisibleEdges(GeometryVisionEye geoEye)
        {
            var visibleEdgeCount = 0;
            foreach (var geo in geoEye.SeenGeoInfos)
            {
                visibleEdgeCount += geo.edges.Where(edge => edge.isVisible == True).ToList().Count;
            }

            return visibleEdgeCount;
        }
    }
}