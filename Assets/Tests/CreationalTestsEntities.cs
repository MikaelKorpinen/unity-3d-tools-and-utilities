﻿using System.Collections;
using UnityEngine;
using System;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine.TestTools;

namespace Tests
{
    public class CreationalTestsEntities : MonoBehaviour
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
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GeometryVisionGetsCreatedWithFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes,false);
            yield return null;
            
            int amountOfObjectsInScene = 0;
            IGeoProcessor processor = geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionEntityProcessor>();
            amountOfObjectsInScene = processor.CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.True( amountOfObjectsInScene>0);
        }
        
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GeometryVisionGetsCreatedWithoutFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision = Instantiate(new GameObject("geoVision"));
            geoVision.transform.position = new Vector3(0f,0f,-6f);
            
            //TODO: Find out how to measure stuff happening in the start and awake from here to get the real estimate

            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value=true;
            yield return null;
            int amountOfObjectsInScene = 0;

            amountOfObjectsInScene = geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionEntityProcessor>().CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.True( amountOfObjectsInScene>0);
        }
        
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithoutFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = Instantiate(new GameObject("geoVision"));
            geoVision.transform.position = new Vector3(0f,0f,-6f);
            
            //TODO: Find out how to measure stuff happening in the start and awake from here to get the real estimate
            geoVision.AddComponent<GeometryVision>();
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value=true;
            yield return null;
            int amountOfObjectsInScene = 0;

            amountOfObjectsInScene = geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionEntityProcessor>().CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.True( geoVision.GetComponent<GeometryVision>() != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Id != "");
            Assert.True( geoVision.GetComponent<GeometryVision>().Camera1 != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().TargetedGeometries != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head.GeoVisions != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head.gameObject.GetComponent<GeometryVisionProcessor>() != null);
            var proc = geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionProcessor>();
            Assert.True( geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionProcessor>() != null);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GeometryVisionHasBasicComponentsWhenCreatedWithFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes,false);
            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value=true;
            yield return null;
            int amountOfObjectsInScene = 0;
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            yield return null;
            amountOfObjectsInScene = geoVisionComponent.Head.GetProcessor<GeometryVisionProcessor>().CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.True( geoVision.GetComponent<GeometryVision>() != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Id != "");
            Assert.True( geoVision.GetComponent<GeometryVision>().Camera1 != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().TargetedGeometries != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head.GeoVisions != null);
        }
        
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GeometryVisionSwitchingToEntitiesWorksWhenCreatedWithFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes,false);
            yield return null;
            int amountOfObjectsInScene = 0;

            geoVision.GetComponent<GeometryVision>().EntityBasedProcessing.Value = true;
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.True( geoVision.GetComponent<GeometryVision>() != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Id != "");
            Assert.True( geoVision.GetComponent<GeometryVision>().Camera1 != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().TargetedGeometries != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head.GeoVisions != null);
            Assert.True( geoVision.GetComponent<GeometryVision>().Head.GetProcessor<GeometryVisionEntityProcessor>() != null);

        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator TargetingParentComponentGetsAddedAfterAddingEyeComponent([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
                
            }
            GameObject geoVision = new GameObject("geoTesting");
            geoVision.AddComponent<GeometryVision>();
            yield return null;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() => { AmountOfTargetingSystemsRegistered = geoVision.GetComponents<GeometryTargeting>().Length; }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator TargetingParentComponentGetsAddedAfterCreatedWithFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
                
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes,false);
            yield return null;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() => { AmountOfTargetingSystemsRegistered = geoVision.GetComponents<GeometryTargeting>().Length; }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator TargetingSystemGetsAddedAfterAddingEyeComponent([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            GameObject geoVision = new GameObject("geoTesting");
            geoVision.AddComponent<GeometryVision>();
            yield return null;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            var targs = geoVision.GetComponent<GeometryTargeting>().TargetingPrograms;
            Measure.Method(() => { AmountOfTargetingSystemsRegistered = targs.Count; }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        public IEnumerator TargetingSystemGetsAddedAfterAfterCreatedWithFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes,false);
            yield return null;
            yield return null;
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            Measure.Method(() => { AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargeting>().TargetingPrograms.Count; }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }
    }
}