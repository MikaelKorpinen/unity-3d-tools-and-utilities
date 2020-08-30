﻿using System;
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

    [CustomPropertyDrawer(typeof(TargetingInstruction))]
    public class VisionTypeDrawerUIE : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            var toggleField = new PropertyField(property.FindPropertyRelative("enabled"));
            var visionTypeField = new PropertyField(property.FindPropertyRelative("geometryType"));
            var layerField = new PropertyField(property.FindPropertyRelative("targetTag"));
            var targetField = new PropertyField(property.FindPropertyRelative("isTargetingEnabled"));
            var targetField2 = new PropertyField(property.FindPropertyRelative("isTargetActionsTemplateSlotVisible"));
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
    /// <summary>
    /// Handle InspectorGUI part of the GeometricVision component. Also hides components that should not be visible to the user.
    /// </summary>
    [CustomEditor(typeof(GeometryVision))]
    public class GeometryVisionInspectorGui : UnityEditor.Editor
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
            var go = Selection.activeGameObject;
            var container = go.GetComponent<GeometryTargetingSystemsContainer>();

            if(go.GetComponent<GeometryVision>() != null)
            {
                go.GetComponent<Camera>().hideFlags = HideFlags.HideInInspector;
                container.hideFlags = HideFlags.HideInInspector;
            }
        }
    }
    
    /// <summary>
    /// Checks if user removes GeometricVision component and if it does cleans up all the dependencies
    /// </summary>
    [CustomEditor(typeof(GeometryTargetingSystemsContainer))]
    public class ClearComponents2 : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var go = Selection.activeGameObject;
            var container = go.GetComponent<GeometryTargetingSystemsContainer>();
            if (go.GetComponent<GeometryVision>() == null && container != null)
            {
                EditorCoroutineUtility.StartCoroutine(container.RemoveAddedComponents(), this);
            }
        }
    }

    /// <summary>
    /// Checks if user removes GeometricVision component and if it does cleans up all the dependencies
    /// </summary>
    [CustomEditor(typeof(GeometryVisionEye))]
    public class ClearComponents : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var go = Selection.activeGameObject;
            var container = go.GetComponent<GeometryTargetingSystemsContainer>();
            if (go.GetComponent<GeometryVision>() == null && container != null)
            {
                EditorCoroutineUtility.StartCoroutine(container.RemoveAddedComponents(), this);
            }
        }
    }
    
    /// <summary>
    /// Checks if user removes GeometricVision component and if it does cleans up all the dependencies
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetingInstruction))]
    public class VisionTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = "Seen target type: " + property.displayName;
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var intend = EditorGUI.indentLevel;
            float offsetX = -70;
            float positionX = position.x + offsetX;
            EditorGUI.indentLevel = 0;
            var offset = position.height / 2 - 6;
            var labelRect = new Rect(positionX, position.y - offset, 70, position.height);
            var toggleRect = new Rect(positionX + 55, position.y - offset, 70, position.height);

            var seeLabelRect = new Rect(positionX + 75, position.y - offset, 111, position.height);
            var typeRect = new Rect(positionX + 105, position.y, 70, position.height);

            var labelLayerRect = new Rect(positionX + 180, position.y - offset, 111, position.height);
            var layerRect = new Rect(positionX + 220, position.y, 111, position.height);

            var labelRectTargeting = new Rect(positionX + 340, position.y - offset, 70, position.height);
            var toggleRectTargeting = new Rect(positionX + 400, position.y, 70, position.height);

            var labelRectOnTargetFound = new Rect(positionX + 420, position.y - offset, 120, position.height);
            var onSomethingHappenedEvent = new Rect(positionX + 520, position.y, 200, position.height);

       //     EditorGUI.LabelField(labelRect, "enabled:");
        //    EditorGUI.PropertyField(toggleRect, property.FindPropertyRelative("enabled"), GUIContent.none);

            EditorGUI.LabelField(seeLabelRect, "See:");
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("geometryType"), GUIContent.none);
            GUIContent label2 = new GUIContent("");

            EditorGUI.LabelField(labelLayerRect, "Tag:");
            property.FindPropertyRelative("targetTag").stringValue = EditorGUI.TagField(layerRect, label2,
                property.FindPropertyRelative("targetTag").stringValue);

            EditorGUI.LabelField(labelRectTargeting, "Targeting:");
            EditorGUI.PropertyField(toggleRectTargeting, property.FindPropertyRelative("isTargetingEnabled"), GUIContent.none);

            var istarget = property.FindPropertyRelative("isTargetActionsTemplateSlotVisible").boolValue;

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