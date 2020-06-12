using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Plugins.GeometricVision;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class LayerAttribute : PropertyAttribute
{
}

[CustomPropertyDrawer(typeof(VisionTarget))]
public class LayerAttributeEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.intValue = EditorGUI.LayerField(position, label, property.intValue);
    }
}

[CustomPropertyDrawer(typeof(VisionTarget))]
public class VisionTypeDrawerUIE : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();

        var toggleField = new PropertyField(property.FindPropertyRelative("onOff"));
        var visionTypeField = new PropertyField(property.FindPropertyRelative("type"));
        var layerField = new PropertyField(property.FindPropertyRelative("targetLayer"));
       
     
        container.Add(toggleField);
        container.Add(visionTypeField);
        container.Add(layerField);


        return container;
    }
}

[CustomPropertyDrawer(typeof(VisionTarget))]
public class VisionTypeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label.text = "Seen target type:";
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var intend = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        
        var labelRect = new Rect(position.x , position.y, 70, position.height);
        var toggleRect = new Rect(position.x + 55, position.y, 70, position.height);
        
        var seeLabelRect = new Rect(position.x +80, position.y, 111, position.height);
        var typeRect = new Rect(position.x + 110, position.y, 70, position.height);
        
        var labelLayerRect = new Rect(position.x +190, position.y, 111, position.height);
        var layerRect = new Rect(position.x +230, position.y, 111, position.height);

        var labelRectTargeting = new Rect(position.x +360, position.y, 70, position.height);
        var toggleRectTargeting = new Rect(position.x + 420, position.y, 70, position.height);
        
        EditorGUI.LabelField(labelRect, "enabled:");
        EditorGUI.PropertyField(toggleRect, property.FindPropertyRelative("onOff"), GUIContent.none);

        EditorGUI.LabelField(seeLabelRect, "See:");
        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("type"), GUIContent.none);
        GUIContent label2 = new GUIContent("");
        
        EditorGUI.LabelField(labelLayerRect, "Layer:");
        property.FindPropertyRelative("targetLayer").intValue = EditorGUI.LayerField(layerRect, label2, property.FindPropertyRelative("targetLayer").intValue);
      
        EditorGUI.LabelField(labelRectTargeting, "Targeting:");
        EditorGUI.PropertyField(toggleRectTargeting, property.FindPropertyRelative("target"), GUIContent.none);

        EditorGUI.indentLevel = intend;
        EditorGUI.EndProperty();
        
    }
}