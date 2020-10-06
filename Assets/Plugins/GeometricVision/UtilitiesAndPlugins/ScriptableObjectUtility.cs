using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.UtilitiesAndPlugins
{
    /// <summary>
    /// Script from Unity wiki. http://wiki.unity3d.com/index.php?title=CreateScriptableObjectAsset&_ga=2.4504122.86194272.1600153901-758874881.1585030311
    /// </summary>
    public static class ScriptableObjectUtility
    {
        #if UNITY_EDITOR
        /// <summary>
        ///	This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static T CreateAsset<T> () where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T> ();
 
            string path = AssetDatabase.GetAssetPath (Selection.activeObject);
            if (path == "") 
            {
                path = "Assets";
            } 
            else if (Path.GetExtension (path) != "") 
            {
                path = path.Replace (Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
            }
 
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New " + typeof(T).ToString() + ".asset");
 
            AssetDatabase.CreateAsset (asset, assetPathAndName);
 
            AssetDatabase.SaveAssets ();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow ();
            Selection.activeObject = asset;
            return asset;
        }
        /// <summary>
        ///	This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static T CreateAssetAtPath<T> (string path) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T> ();
            
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New " + typeof(T).ToString() + ".asset");
 
            AssetDatabase.CreateAsset (asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow ();
            Selection.activeObject = asset;
            return asset;
        }
        #endif
    }
}