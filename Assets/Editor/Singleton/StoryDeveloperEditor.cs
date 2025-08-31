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
        WindowObject = GetWindow<StoryEditorWindow>("简单窗口");

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

        EditorGUILayout.LabelField("剧情层级树 : ", EditorStyles.boldLabel);

        DisplayAddNewObject = EditorGUILayout.Foldout(DisplayAddNewObject, "添加 : ", true);

        if (DisplayAddNewObject)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15f);
            if (GUILayout.Button("添加新章节")) { }
            if (GUILayout.Button("添加新节点")) { }
            if (GUILayout.Button("添加新行")) { }
            EditorGUILayout.EndHorizontal();
        }

        DisplayTestObject = EditorGUILayout.Foldout(DisplayTestObject, "测试 : ", true);

        if (DisplayAddNewObject)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15f);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("测试所有节点动作"))
            {
                TestAllNodeActions();
            }
            GUI.backgroundColor = GUIBackgroundColor;
            if (GUILayout.Button("测试当前节点动作"))
            {
                TestAllNodeActions();
            }
            EditorGUILayout.EndHorizontal();
        }

        DisplayNewObjectSettings = EditorGUILayout.Foldout(
            DisplayNewObjectSettings,
            "新节点默认设置 : ",
            true
        );

        if (DisplayNewObjectSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("默认名称 : ", GUILayout.MaxWidth(90));

            NewObjectName = EditorGUILayout.TextField(NewObjectName);

            EditorGUILayout.LabelField("新节点默认层级 : ", GUILayout.Width(120));

            NewObjectLayer = EditorGUILayout.IntSlider(NewObjectLayer, 1, 3);

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        DisplayTreeStruct = EditorGUILayout.Foldout(DisplayTreeStruct, "剧情树结构 : ", true);

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
