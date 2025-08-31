using StoryNS;
using UnityEditor;
using UnityEngine;
using Json = JsonLoader.JsonManager;

public class StoryEditorWindow : EditorWindow
{
    private static StoryContainer _storyContainer;

    private static StoryEditorWindow WindowObject;

    private int NewObjectLayer = 0;

    private string NewObjectName = "Default";

    private bool DisplayAddNewObject = true;

    private bool DisplayTestObject = true;

    private bool DisplayNewObjectSettings = false;

    private bool DisplayTreeStruct = true;

    private Color GUIBackgroundColor;

    public StoryContainer StoryContainer => _storyContainer;

    [MenuItem("Tools/Story Editor")]
    public static void ShowWindow()
    {
        WindowObject = GetWindow<StoryEditorWindow>("�򵥴���");

        WindowObject.minSize = new Vector2(1080, 640);

        WindowObject.maxSize = new Vector2(1920, 1080);

        WindowObject.position = new Rect(150, 150, 160, 160);
    }

    private void OnEnable()
    {
        Json.TryLoadJsonFromZip("GameStory.story", out _storyContainer, default);
    }

    private void TestAllNodeActions()
    {
        StoryContainer.ActionTest();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        GUIBackgroundColor = GUI.backgroundColor;

        DrawTreePanel();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawNode(StoryNode Node) { }

    private void DrawTreePanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(400));

        EditorGUILayout.LabelField("����㼶�� : ", EditorStyles.boldLabel);

        DisplayAddNewObject = EditorGUILayout.Foldout(DisplayAddNewObject, "��� : ", true);

        if (DisplayAddNewObject)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15f);
            if (GUILayout.Button("������½�")) { }
            if (GUILayout.Button("����½ڵ�")) { }
            if (GUILayout.Button("�������")) { }
            EditorGUILayout.EndHorizontal();
        }

        DisplayTestObject = EditorGUILayout.Foldout(DisplayTestObject, "���� : ", true);

        if (DisplayAddNewObject)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15f);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("�������нڵ㶯��"))
            {
                TestAllNodeActions();
            }
            GUI.backgroundColor = GUIBackgroundColor;
            if (GUILayout.Button("���Ե�ǰ�ڵ㶯��"))
            {
                TestAllNodeActions();
            }
            EditorGUILayout.EndHorizontal();
        }

        DisplayNewObjectSettings = EditorGUILayout.Foldout(
            DisplayNewObjectSettings,
            "�½ڵ�Ĭ������ : ",
            true
        );

        if (DisplayNewObjectSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Ĭ������ : ", GUILayout.MaxWidth(90));

            NewObjectName = EditorGUILayout.TextField(NewObjectName);

            EditorGUILayout.LabelField("�½ڵ�Ĭ�ϲ㼶 : ", GUILayout.Width(120));

            NewObjectLayer = EditorGUILayout.IntSlider(NewObjectLayer, 1, 3);

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        DisplayTreeStruct = EditorGUILayout.Foldout(DisplayTreeStruct, "�������ṹ : ", true);

        if (DisplayTreeStruct)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginScrollView(new Vector2(0f, 0f), GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndVertical();
    }
}
