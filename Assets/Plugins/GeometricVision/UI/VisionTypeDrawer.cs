using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GeometricVision;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.UniRx.Scripts.UnityEngineBridge;
using UniRx;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Plugins.GeometricVision.UI
{
    public class LayerAttribute : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(VisionTarget))]
    public class VisionTypeDrawerUIE : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            var toggleField = new PropertyField(property.FindPropertyRelative("enabled"));
            var visionTypeField = new PropertyField(property.FindPropertyRelative("type"));
            var layerField = new PropertyField(property.FindPropertyRelative("targetLayer"));
            var targetField = new PropertyField(property.FindPropertyRelative("target"));
            var targetField2 = new PropertyField(property.FindPropertyRelative("targetHidden"));
            var actionsTemplate = new PropertyField(property.FindPropertyRelative("targetingActions"));

            container.Add(toggleField);
            container.Add(visionTypeField);
            container.Add(layerField);
            container.Add(targetField);
            container.Add(targetField2);
            container.Add(actionsTemplate);

            return container;
        }
    }

    [CustomEditor(typeof(GeometryVision))]
    public class GeometryVisionInspectorGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Create a new actions template for targeting."))
            {
                var newActionsTemplate = CreateInstance<ActionsTemplateObject>();
                newActionsTemplate.name = newActionsTemplate.name;


                AssetDatabase.CreateAsset(newActionsTemplate, "Assets/NewActionsAssetForTargeting.asset");
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();

                Selection.activeObject = newActionsTemplate;
            }
        }
    }


    [CustomEditor(typeof(GeometryVisionEye))]
    public class ClearComponents : Editor
    {
        public override void OnInspectorGUI()
        {
            var go = Selection.activeGameObject;
            if (go.GetComponent<GeometryVision>() == null)
            {
                EditorCoroutineUtility.StartCoroutine(RemoveAddedComponents(go), this);
            }
        }

        private IEnumerator RemoveAddedComponents(GameObject go)
        {
            if (go.GetComponent<Camera>() != null)
            {
                DestroyImmediate(go.GetComponent<Camera>());
            }

            if (go.GetComponent<GeometryVisionEye>() != null)
            {
                DestroyImmediate(go.GetComponent<GeometryVisionEye>());
            }

            if (go.GetComponent<GeometryTargeting>() != null)
            {
                DestroyImmediate(go.GetComponent<GeometryTargeting>());
            }


            yield return null;
        }
    }

    [CustomPropertyDrawer(typeof(VisionTarget))]
    public class VisionTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = "Seen target type: " + property.displayName;
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var intend = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var offset = position.height / 2 - 6;
            var labelRect = new Rect(position.x, position.y - offset, 70, position.height);
            var toggleRect = new Rect(position.x + 55, position.y - offset, 70, position.height);

            var seeLabelRect = new Rect(position.x + 75, position.y - offset, 111, position.height);
            var typeRect = new Rect(position.x + 105, position.y, 70, position.height);

            var labelLayerRect = new Rect(position.x + 180, position.y - offset, 111, position.height);
            var layerRect = new Rect(position.x + 220, position.y, 111, position.height);

            var labelRectTargeting = new Rect(position.x + 340, position.y - offset, 70, position.height);
            var toggleRectTargeting = new Rect(position.x + 400, position.y, 70, position.height);

            var labelRectOnTargetFound = new Rect(position.x + 420, position.y - offset, 120, position.height);
            var onSomethingHappenedEvent = new Rect(position.x + 520, position.y, 200, position.height);

            EditorGUI.LabelField(labelRect, "enabled:");
            EditorGUI.PropertyField(toggleRect, property.FindPropertyRelative("enabled"), GUIContent.none);

            EditorGUI.LabelField(seeLabelRect, "See:");
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("type"), GUIContent.none);
            GUIContent label2 = new GUIContent("");

            EditorGUI.LabelField(labelLayerRect, "Layer:");
            property.FindPropertyRelative("targetLayer").intValue = EditorGUI.LayerField(layerRect, label2,
                property.FindPropertyRelative("targetLayer").intValue);

            EditorGUI.LabelField(labelRectTargeting, "Targeting:");
            EditorGUI.PropertyField(toggleRectTargeting, property.FindPropertyRelative("target"), GUIContent.none);

            var istarget = property.FindPropertyRelative("targetHidden").boolValue;

            if (istarget)
            {
                EditorGUI.LabelField(labelRectOnTargetFound, "On target found:");
                EditorGUI.PropertyField(onSomethingHappenedEvent, property.FindPropertyRelative("targetingActions"),
                    GUIContent.none);
            }


            EditorGUI.indentLevel = intend;
            EditorGUI.EndProperty();
        }
    }
}