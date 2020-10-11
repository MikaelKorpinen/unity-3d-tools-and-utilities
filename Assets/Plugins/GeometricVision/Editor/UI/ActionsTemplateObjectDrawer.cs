using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.Editor.UI
{

    [CustomEditor(typeof(ActionsTemplateObject))]
    internal class ActionsTemplateObjectDrawer : UnityEditor.Editor
    {
        private Texture headerTexture;
        private float textureBottomSpaceExtension = 50f;
        private float ratioMultiplier;
        
        public override void OnInspectorGUI()
        {
            if (headerTexture == null)
            {
                headerTexture = LoadPNG(Application.dataPath+ GeometryVisionSettings.HeaderImagePath);

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

            void DrawTexture()
            {
                GUILayout.Label("Geometric vision actions template");
                ratioMultiplier = (float) headerTexture.height / (float) headerTexture.width;
                EditorGUI.DrawPreviewTexture(
                    new Rect(25, 60, EditorGUIUtility.currentViewWidth, EditorGUIUtility.currentViewWidth * ratioMultiplier),
                    headerTexture);
                GUILayout.Space(EditorGUIUtility.currentViewWidth * ratioMultiplier + textureBottomSpaceExtension);
            }
        }
    }
}