#if UNITY_EDITOR
using NavigatorNS;
using UnityEditor;

[CustomEditor(typeof(UINavigator))]
public class UINavigatorEditor : Editor
{
    private SerializedProperty navigateModeProp;
    private SerializedProperty appendPositionProp;
    private SerializedProperty appendPercentProp;

    private void OnEnable()
    {
        navigateModeProp = serializedObject.FindProperty("NavigateMode");
        appendPositionProp = serializedObject.FindProperty("AppendPosition");
        appendPercentProp = serializedObject.FindProperty("AppendPercent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. 模式选择
        EditorGUILayout.PropertyField(navigateModeProp);
        EditorGUILayout.Space();

        // 2. 根据模式显示不同字段
        var component = target as UINavigator;

        switch (component.NavigateMode)
        {
            case UINavigator.NavigateModeType.Percent:
                EditorGUILayout.PropertyField(appendPercentProp);
                break;

            case UINavigator.NavigateModeType.Axis:
                EditorGUILayout.PropertyField(appendPositionProp);
                break;
        }

        EditorGUILayout.Space();

        // 3. 绘制其余所有属性（排除已经手动绘制的属性）
        DrawPropertiesExcluding(
            serializedObject,
            "NavigateMode",
            "AppendPosition",
            "AppendPercent",
            "m_Script",
            "m_Enabled" // 通常也会排除脚本引用
        );

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
