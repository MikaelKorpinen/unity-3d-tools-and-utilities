using Plugins.GeometricVision.ImplementationsGameObjects;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.Editor.UI
{
    /// <summary>
    /// Checks if user removes GeometricVision component and if it does cleans up all the dependencies
    /// </summary>
    [CustomEditor(typeof(GeometryTargetingSystemsContainer))]
    public class ClearComponents2 : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var go = Selection.activeGameObject;
            if (go != null && go.GetComponent<GeometryVision>() == null )
            {
                var container = go.GetComponent<GeometryTargetingSystemsContainer>();
                if (container != null)
                {
                    EditorCoroutineUtility.StartCoroutine(container.RemoveAddedComponents(), this);
                }
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
            if (go != null)
            {
                var container = go.GetComponent<GeometryTargetingSystemsContainer>();
                if (go.GetComponent<GeometryVision>() == null && container != null)
                {
                    EditorCoroutineUtility.StartCoroutine(container.RemoveAddedComponents(), this);
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if user removes GeometricVision component and if it does cleans up all the dependencies
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetingInstruction))]
    public class TargetingInstructionsTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = "Targeting instruction: " + property.displayName;
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var intend = EditorGUI.indentLevel;
            float offsetX = -70;
            float positionX = position.x + offsetX;
            EditorGUI.indentLevel = 0;
            var offset = position.height / 2 - 6;
            var labelRect = new Rect(positionX +70, position.y - offset, 70, position.height);
            var entityFilterRect = new Rect(positionX + 145, position.y - offset, 70, position.height);
            
            var tagTextRect = new Rect(positionX + 230, position.y - offset, 111, position.height);
            var tagRect = new Rect(positionX + 270, position.y, 111, position.height);

            var labelRectTargeting = new Rect(positionX + 390, position.y - offset, 70, position.height);
            var toggleRectTargeting = new Rect(positionX + 450, position.y, 70, position.height);

            var labelRectOnTargetFound = new Rect(positionX + 470, position.y - offset, 120, position.height);
            var onSomethingHappenedEvent = new Rect(positionX + 570, position.y, 200, position.height);

            EditorGUI.LabelField(labelRect, "Entity filter:");
            var prop = property.FindPropertyRelative("entityQueryFilter");
            EditorGUI.PropertyField(entityFilterRect, prop, GUIContent.none);

            GUIContent label2 = new GUIContent("");

            EditorGUI.LabelField(tagTextRect, "Tag:");
            property.FindPropertyRelative("targetTag").stringValue = EditorGUI.TagField(tagRect, label2,
                property.FindPropertyRelative("targetTag").stringValue);

            EditorGUI.LabelField(labelRectTargeting, "Targeting:");
            EditorGUI.PropertyField(toggleRectTargeting, property.FindPropertyRelative("isTargetingEnabled"), GUIContent.none);
            
            EditorGUI.LabelField(labelRectOnTargetFound, "Trigger actions:");
            EditorGUI.PropertyField(onSomethingHappenedEvent, property.FindPropertyRelative("targetingActions"),
                GUIContent.none);
            


            EditorGUI.indentLevel = intend;
            EditorGUI.EndProperty();
        }
    }
}