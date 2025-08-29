using UnityEditor;
using UnityEngine;

/// <summary>
/// �������Ժͻ�������ֻ�ڱ༭ģʽ��Ч
/// </summary>
namespace PropertyExtentions
{
    [CustomPropertyDrawer(typeof(Ext.ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            GUI.enabled = false;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = true;

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(Ext.ReadOnlyInGameAttribute))]
    public class ReadOnlyInGameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (!Application.isPlaying)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }

            EditorGUI.EndProperty();
        }
    }
}
