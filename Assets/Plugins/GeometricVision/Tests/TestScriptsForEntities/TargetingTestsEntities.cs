using System.Collections;
using NUnit.Framework;
using Plugins.GeometricVision.ImplementationsEntities;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForEntities
{
    public class TargetingTestsEntities : MonoBehaviour
    {
        private GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView = 25f,
            processEntities = true,
            defaultTargeting = true,
            processGameObjects = false,
            entityComponentQueryFilter = null,
            edgesTargeted = false,
        };
        private string[] testObjectNames = {"GameObject", "Quad", "Plane", "Cylinder", "Sphere", "Cube"};
        
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
            factorySettings = new GeometryDataModels.FactorySettings
            {
                fielOfView = 25f,
                processEntities = true,
                defaultTargeting = true,
                processGameObjects = false,
                entityComponentQueryFilter = null,
                edgesTargeted = false,
            };
            TestUtilities.CleanUpEntities();
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator TargetingSystemGetsAddedIfTargetingEnabled(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;

            bool isAdded = geoVision.GetComponent<GeometryTargetingSystemsContainer>()
                .GetTargetingProgram<GeometryEntitiesObjectTargeting>() != null;

            Assert.True(isAdded);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator TargetingSystemGetsTarget(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;

            GeometryDataModels.Target target = new GeometryDataModels.Target();
            Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(); }).Run();
            
            Assert.True(target.isEntity == true);
            Assert.True(target.distanceToCastOrigin > 0);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator ClosestTargetListIsEmptyIfNothingIsSeen(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;


            Assert.True(geoVision.GetComponent<GeometryVision>().GetClosestTargets().Length > 0);
            //Move camera away so there is nothing to be seen
            geoVision.transform.position = new Vector3(34343f, 343434f, 3434343f);
            yield return null;
            Assert.True(geoVision.GetComponent<GeometryVision>().GetClosestTargets().Length == 0);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator TargetingSystemGetsClosestTarget(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            factorySettings.entityComponentQueryFilter = null;
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            yield return null;
            float offset = 0.1f;
            GeometryDataModels.Target target = new GeometryDataModels.Target();

            //Go through every test object
            for (var index = 0; index < testObjectNames.Length; index++)
            {
                geoVision.transform.position = new Vector3(index * -2f + offset, 0f, -6f);
                yield return null;
                Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(); })
                    .Run();
                Assert.True(Vector3.Distance(target.position, new Vector3(index * -2f, 0f, 10f)) < 0.1f+ offset);
            }

            Assert.True(target.isEntity);
            Assert.True(target.distanceToCastOrigin > 0);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntitiesTargeting))]
        public IEnumerator EntityFilteringByComponentWorks([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForEntities))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            string pathToEntityFilterScript = "Assets/Plugins/GeometricVision/EntityScripts/FromUnity/RotationSpeed_SpawnAndRemove.cs";
            UnityEngine.Object entityFilterScript = AssetDatabase.LoadAssetAtPath(pathToEntityFilterScript, typeof(UnityEngine.Object));
            factorySettings.entityComponentQueryFilter = entityFilterScript;
            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            factorySettings.entityComponentQueryFilter = null;
            float offset = 0.1f;
            GeometryDataModels.Target target = new GeometryDataModels.Target();
            int amountOfItemsFound = 0;
            int amountOfExpectedItemsToBeFound = 1;
            for (var index = 0; index < testObjectNames.Length; index++)
            {
                geoVision.transform.position = new Vector3(index * -2f + offset, 0f, -6f);
                yield return null;
                Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(); })
                    .Run();
                if (Vector3.Distance(target.position, new Vector3(index * -2f, 0f, 10f)) < 0.1f+ offset)
                {
                    amountOfItemsFound += 1;
                    Assert.True(target.isEntity == true);
                    Assert.True(target.distanceToCastOrigin > 0);
                }
            }
            
            Assert.True(amountOfItemsFound == amountOfExpectedItemsToBeFound);
   }
    }
}