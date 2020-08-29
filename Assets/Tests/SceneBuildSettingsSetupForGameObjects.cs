using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Plugins.GeometricVision;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    #if UNITY_EDITOR
    public class SceneBuildSettingsSetupForGameObjects : IPrebuildSetup
    {
        public void Setup()
        {
            TestSessionVariables.BuildScenes = TestUtilities.SetupBuildSettings(GetScenesFromPathGameObjects());
        }
        
        List<string> GetScenesFromPathGameObjects()
        {
            var scenePaths = TestUtilities.CreateScenePathFromRelativeAddress(TestSettings.GameObjectsSimpleTestsPath);

            return scenePaths;
        }
    }
    public class SceneBuildSettingsSetupForGameObjectsEmptyScene : IPrebuildSetup
    {
        public void Setup()
        {
            TestSessionVariables.BuildScenes = TestUtilities.SetupBuildSettings(GetScenesFromPathGameObjects());
        }
        
        List<string> GetScenesFromPathGameObjects()
        {
            var scenePaths = TestUtilities.CreateScenePathFromRelativeAddress(TestSettings.EmptyScenePath);

            return scenePaths;
        }
    }

    public class SceneBuildSettingsSetupForEntities : IPrebuildSetup
    {
        public void Setup()
        {
            TestSessionVariables.BuildScenes = TestUtilities.SetupBuildSettings(GetScenesForEntitiesFromPath());
        }
        
        List<string>  GetScenesForEntitiesFromPath()
        {
            var scenePaths = TestUtilities.CreateScenePathFromRelativeAddress(TestSettings.EntitiesSimpleTestsPath);

            return scenePaths;
        }
    }
    
    public class SceneBuildSettingsSetupForEntitiesTargeting : IPrebuildSetup
    {
        public void Setup()
        {
            TestSessionVariables.BuildScenes = TestUtilities.SetupBuildSettings(GetScenesForEntitiesFromPath());
        }
        
        List<string>  GetScenesForEntitiesFromPath()
        {
            var scenePaths = TestUtilities.CreateScenePathFromRelativeAddress(TestSettings.EntitiesTargetingTests);

            return scenePaths;
        }
    }
    
        
    public class SceneBuildSettingsSetupForGameObjectsTargeting : IPrebuildSetup
    {
        public void Setup()
        {
            TestSessionVariables.BuildScenes = TestUtilities.SetupBuildSettings(GetScenesForGameObjectsFromPath());
        }
        
        List<string>  GetScenesForGameObjectsFromPath()
        {
            var scenePaths = TestUtilities.CreateScenePathFromRelativeAddress(TestSettings.GameObjectsTargetingTests);

            return scenePaths;
        }
    }
    internal static class TestSessionVariables
    {
        internal static EditorBuildSettingsScene[] BuildScenes;
    }

#endif
}
