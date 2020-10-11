using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.Editor.UI
{        [CustomEditor(typeof(GeometryVision))]

        internal class GeometryVisionHeaderImageDrawer : UnityEditor.Editor
        {
            private Texture headerTexture;
            private float textureBottomSpaceExtension = 5f;
            private float ratioMultiplier;
        
            public override void OnInspectorGUI()
            {
                if (headerTexture == null)
                {
                    headerTexture = LoadPNG(Application.dataPath + GeometryVisionSettings.HeaderImagePath);

                    Texture2D LoadPNG(string filePath) {
 
                        Texture2D texture2D = null;
                        byte[] fileData;
 
                        if (File.Exists(filePath))     {
                            fileData = File.ReadAllBytes(filePath);
                            texture2D = new Texture2D(2, 2);

                            texture2D.LoadImage(fileData); 


                        }
                        return texture2D;
                    }
                }
            
                DrawTexture();
                DrawDefaultInspector ();

                if (GUILayout.Button("Create a new actions template for targeting."))
                {
                    var newActionsTemplate = CreateInstance<ActionsTemplateObject>();
                    newActionsTemplate.name = newActionsTemplate.name;


                    AssetDatabase.CreateAsset(newActionsTemplate, GeometryVisionSettings.NewActionsAssetForTargetingPath);
                    AssetDatabase.SaveAssets();

                    EditorUtility.FocusProjectWindow();

                    Selection.activeObject = newActionsTemplate;
                }
                var go = Selection.activeGameObject;


                if(go!= null && go.GetComponent<GeometryVision>() !=null)
                {
                    var container = go.GetComponent<GeometryTargetingSystemsContainer>();
                    go.GetComponent<Camera>().hideFlags = HideFlags.HideInInspector;

                        container.hideFlags = HideFlags.HideInInspector;
                    
                }
                
                void DrawTexture()
                {
                    ratioMultiplier = (float) headerTexture.height / (float) headerTexture.width;
                    EditorGUI.DrawPreviewTexture(
                        new Rect(0, 0, EditorGUIUtility.currentViewWidth, EditorGUIUtility.currentViewWidth * ratioMultiplier),
                        headerTexture);
                    GUILayout.Space(EditorGUIUtility.currentViewWidth * ratioMultiplier + textureBottomSpaceExtension);
                }
            }
        }
}
