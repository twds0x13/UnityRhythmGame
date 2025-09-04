using System;
using System.Collections.Generic;
using System.Linq;
using ECS;
using UnityEditor;
using UnityEngine;
using Story = ECS.StoryTreeManager;

public class ECSTreeEditorWindow : EditorWindow
{
    private Vector2 treeScrollPosition;
    private Vector2 detailsScrollPosition;
    private Vector2 componentScrollPosition;
    private Dictionary<int, bool> expandedNodes = new Dictionary<int, bool>();
    private Entity selectedEntity;
    private Type selectedComponentType;
    private float leftPanelWidth = 300f;
    private float middlePanelWidth = 400f;
    private float rightPanelWidth = 400f;
    private float splitterWidth = 5f;
    private bool isResizingLeft;
    private bool isResizingMiddle;
    private Rect leftPanelRect;
    private Rect middlePanelRect;
    private Rect rightPanelRect;
    private Rect splitterLeftRect;
    private Rect splitterMiddleRect;
    private int hoverEntityId = -1;

    [MenuItem("Tools/ECS Tree Editor")]
    public static void ShowWindow()
    {
        GetWindow<ECSTreeEditorWindow>("ECS Tree Editor");
    }

    private void OnEnable()
    {
        // ��ʼ������С
        leftPanelWidth = 300f;
        middlePanelWidth = 400f;
        rightPanelWidth = position.width - leftPanelWidth - middlePanelWidth - splitterWidth * 2;
    }

    private void OnGUI()
    {
        DrawToolbar();

        // �����������
        CalculatePanelRects();

        // ���������壨��״�ṹ��
        DrawLeftPanel();

        // �����м���壨ѡ�нڵ�״̬��
        DrawMiddlePanel();

        // �����Ҳ���壨����༭����
        DrawRightPanel();

        // ���Ʒָ���
        DrawSplitters();

        // ��������¼�
        HandleEvents();

        // ������ͣ״̬
        if (Event.current.type == EventType.Repaint)
        {
            hoverEntityId = -1;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshTree();
            }

            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton))
            {
                ExpandAll();
            }

            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton))
            {
                CollapseAll();
            }

            // ���Save��ť
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Story.Inst.SaveStoryTreeAsyncForget("story.zip", true);
            }

            // ���Load��ť
            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                Story.Inst.LoadStoryTreeAsyncForget("story.zip", true);
            }

            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void CalculatePanelRects()
    {
        float toolbarHeight = EditorStyles.toolbar.fixedHeight;
        float y = toolbarHeight;
        float height = position.height - toolbarHeight;

        leftPanelRect = new Rect(0, y, leftPanelWidth, height);
        splitterLeftRect = new Rect(leftPanelWidth, y, splitterWidth, height);
        middlePanelRect = new Rect(leftPanelWidth + splitterWidth, y, middlePanelWidth, height);
        splitterMiddleRect = new Rect(
            leftPanelWidth + splitterWidth + middlePanelWidth,
            y,
            splitterWidth,
            height
        );
        rightPanelRect = new Rect(
            leftPanelWidth + splitterWidth + middlePanelWidth + splitterWidth,
            y,
            rightPanelWidth,
            height
        );
    }

    private void DrawLeftPanel()
    {
        GUILayout.BeginArea(leftPanelRect, GUI.skin.box);
        treeScrollPosition = EditorGUILayout.BeginScrollView(treeScrollPosition);

        var root = ECSFramework.Inst.GetRootEntity();
        if (root != null)
        {
            DrawEntityNode(root, 0);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No root entity found. Please initialize the ECS framework.",
                MessageType.Info
            );
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawMiddlePanel()
    {
        GUILayout.BeginArea(middlePanelRect, GUI.skin.box);

        // �Ϸ���������ť����
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(100));
        {
            EditorGUILayout.LabelField("�ڵ����", EditorStyles.boldLabel);

            if (selectedEntity != null)
            {
                // ���ȼ���Ƿ��Ǹ��ڵ�
                if (selectedEntity.HasComponent<Comp.Root>())
                {
                    DrawRootOperations();
                }
                // Ȼ����ڵ����Ͳ���ʾ��Ӧ�Ĳ�����ť
                else if (selectedEntity.HasComponent<Comp.Localization>())
                {
                    var locComp = selectedEntity.GetComponent<Comp.Localization>();

                    switch (locComp.Type)
                    {
                        case Comp.Localization.NodeType.Chapter:
                            DrawChapterOperations();
                            break;
                        case Comp.Localization.NodeType.Episode:
                            DrawEpisodeOperations();
                            break;
                        case Comp.Localization.NodeType.Line:
                            DrawLineOperations();
                            break;
                        default:
                            EditorGUILayout.HelpBox("δ֪�ڵ�����", MessageType.Info);
                            break;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("�˽ڵ�û��Localization���", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("��ѡ��һ���ڵ�����ʾ����", MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();

        // �·����ڵ���ϸ��Ϣ����
        detailsScrollPosition = EditorGUILayout.BeginScrollView(detailsScrollPosition);
        {
            if (selectedEntity != null)
            {
                EditorGUILayout.LabelField("�ڵ���ϸ��Ϣ", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField($"ID: {selectedEntity.Id}");

                    // ��ʾ�������
                    foreach (var component in selectedEntity.Components)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(
                            component.GetType().Name,
                            EditorStyles.boldLabel
                        );

                        // �����ض��������
                        if (component is Comp.Order order)
                        {
                            EditorGUILayout.LabelField($"Number: {order.Number}");
                            EditorGUILayout.LabelField($"Label: {order.Label}");
                            EditorGUILayout.LabelField($"Formatted: {order.FormattedLabel}");
                        }
                        else if (component is Comp.Localization localization)
                        {
                            EditorGUILayout.LabelField($"Type: {localization.Type}");
                            EditorGUILayout.LabelField($"Default Text: {localization.DefaultText}");
                            EditorGUILayout.LabelField($"Number: {localization.Number}");
                            EditorGUILayout.LabelField($"Speaker Key: {localization.SpeakerKey}");
                            EditorGUILayout.LabelField($"Context Key: {localization.ContextKey}");
                        }
                        else if (component is Comp.Parent parent)
                        {
                            EditorGUILayout.LabelField($"Parent ID: {parent.ParentId}");
                        }
                        else if (component is Comp.Children children)
                        {
                            EditorGUILayout.LabelField(
                                $"Children Count: {children.ChildrenEntities.Count}"
                            );
                            EditorGUILayout.LabelField(
                                $"Children IDs: {string.Join(", ", children.ChildrenIds)}"
                            );
                        }
                        else if (component is Comp.Root root)
                        {
                            EditorGUILayout.LabelField($"Root Name: {root.RootName}");
                        }
                        else if (component is Comp.IdManager idManager)
                        {
                            EditorGUILayout.LabelField(
                                $"Next Available ID: {idManager.NextAvailableId}"
                            );
                            EditorGUILayout.LabelField($"Entity Count: {idManager.EntityCount}");
                        }
                        else
                        {
                            // ͨ�������ʾ
                            EditorGUILayout.LabelField(component.ToString());
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("ѡ��һ���ڵ��Բ鿴��ϸ��Ϣ", MessageType.Info);
            }
        }
        EditorGUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void DrawEntityNode(Entity entity, int depth)
    {
        if (entity == null)
            return;

        // ��ʼ��չ��״̬
        if (!expandedNodes.ContainsKey(entity.Id))
        {
            expandedNodes[entity.Id] = depth < 3; // Ĭ��չ��ȫ��
        }

        // ����Ƿ����ӽڵ�
        bool hasChildren =
            entity.HasComponent<Comp.Children>()
            && entity.GetComponent<Comp.Children>().ChildrenEntities.Count > 0;

        // ʹ��ˮƽ����ƽڵ���
        EditorGUILayout.BeginHorizontal(GUILayout.Height(18)); // �̶��и�
        {
            // �����������
            GUILayout.Space(depth * 14);

            // �����۵���ͷ��������ӽڵ㣩
            if (hasChildren)
            {
                // ��ȡһ��С�ľ����������ڼ�ͷ
                Rect foldoutRect = GUILayoutUtility.GetRect(14, 18, GUILayout.Width(14));

                // ������ͷλ�ã������ƶ�2����
                foldoutRect.y += 2;

                // ���Ƽ�ͷͼ��
                GUIContent arrowContent = expandedNodes[entity.Id]
                    ? EditorGUIUtility.IconContent("IN foldout on")
                    : EditorGUIUtility.IconContent("IN foldout");

                if (GUI.Button(foldoutRect, arrowContent, EditorStyles.label))
                {
                    expandedNodes[entity.Id] = !expandedNodes[entity.Id];
                    Event.current.Use();
                }
            }
            else
            {
                // ���ӽڵ�ʱʹ�ÿհ�ռλ�����ֶ���
                GUILayout.Space(16);
            }

            // ���ƽڵ��ǩ
            string displayName = GetEntityDisplayName(entity);

            // ��ȡ��ǩ�ľ�������
            Rect labelRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));

            // �����ı�λ�ã������ƶ�1����
            labelRect.y += 1;

            // ����Ƿ���ͣ��ѡ��
            bool isHovered = labelRect.Contains(Event.current.mousePosition);
            bool isSelected = selectedEntity != null && selectedEntity.Id == entity.Id;

            if (isHovered)
            {
                hoverEntityId = entity.Id;
            }

            // ��������
            if (isSelected || isHovered)
            {
                Color highlightColor = isSelected
                    ? new Color(0.2f, 0.4f, 0.8f, 0.3f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.3f);
                EditorGUI.DrawRect(labelRect, highlightColor);
            }

            // ���Ʊ�ǩ�ı�
            EditorGUI.LabelField(labelRect, displayName);

            // �������¼� - ������ǩ����ɵ��ѡ��ʵ��
            if (
                Event.current.type == EventType.MouseDown
                && labelRect.Contains(Event.current.mousePosition)
            )
            {
                SelectEntity(entity);
                Event.current.Use();
            }
        }
        EditorGUILayout.EndHorizontal();

        // �����ӽڵ㣨���չ�������ӽڵ㣩
        if (expandedNodes[entity.Id] && hasChildren)
        {
            var children = entity.GetComponent<Comp.Children>().ChildrenEntities;

            // ��Order��������ӽڵ�
            var sortedChildren = children
                .OrderBy(child =>
                {
                    if (child.HasComponent<Comp.Order>())
                    {
                        return child.GetComponent<Comp.Order>().Number;
                    }
                    return child.Id;
                })
                .ToList();

            // �ݹ�����ӽڵ�
            foreach (var child in sortedChildren)
            {
                DrawEntityNode(child, depth + 1);
            }
        }
    }

    // Ϊ���ڵ���Ʋ�����ť
    private void DrawRootOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("����½�"))
            {
                // ʹ��Story.Inst�����½�
                var newChapter = Story.Inst.CreateChapter("���½�");

                // ȷ�����ڵ�չ��
                expandedNodes[selectedEntity.Id] = true;

                // ѡ���´�����ʵ��
                SelectEntity(newChapter);

                RefreshTree();
            }

            if (GUILayout.Button("��������½�"))
            {
                // ʹ��Story.Inst���������½�
                var newChapters = Story.Inst.CreateChapters("�½�1", "�½�2", "�½�3");

                // ȷ�����ڵ�չ��
                expandedNodes[selectedEntity.Id] = true;

                // ѡ���һ���´�����ʵ��
                if (newChapters.Count > 0)
                {
                    SelectEntity(newChapters[0]);
                }

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();

        /*

        // ����Զ������������½ڵĹ���
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("�Զ������������½�"))
            {
                ShowCustomCreateChaptersDialog();
            }

            if (GUILayout.Button("�߼���������"))
            {
                ShowAdvancedCreateChaptersDialog();
            }
        }
        EditorGUILayout.EndHorizontal();

        */

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("��������"))
            {
                // ���������ӽڵ�
                var children = selectedEntity.Children();
                int order = 1;
                foreach (var child in children.OrderBy(c => Story.Inst.GetEntityOrder(c)))
                {
                    if (child.HasComponent<Comp.Order>())
                    {
                        child.GetComponent<Comp.Order>().Number = order;
                    }
                    if (child.HasComponent<Comp.Localization>())
                    {
                        child.GetComponent<Comp.Localization>().Number = order;
                    }
                    order++;
                }
                RefreshTree();
            }

            if (GUILayout.Button("���ɱ��ػ���"))
            {
                // Ϊ�����ӽڵ����ɱ��ػ���
                foreach (var child in selectedEntity.Children())
                {
                    if (child.HasComponent<Comp.Localization>())
                    {
                        var locComp = child.GetComponent<Comp.Localization>();
                        locComp.GenerateLocalizationKey(child, ECSFramework.Inst);
                    }
                }
                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();

        // ���ɾ���½ڵĹ���
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("�½ڹ���", EditorStyles.boldLabel);

        // ��ʾ�����½��б��ṩɾ��ѡ��
        var chapters = Story.Inst.GetChapters();
        if (chapters.Count > 0)
        {
            foreach (var chapter in chapters)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    string chapterName = $"ID: {chapter.Id}";
                    if (chapter.HasComponent<Comp.Localization>())
                    {
                        var locComp = chapter.GetComponent<Comp.Localization>();
                        if (!string.IsNullOrEmpty(locComp.DefaultText))
                        {
                            chapterName = locComp.DefaultText;
                        }
                    }

                    EditorGUILayout.LabelField(chapterName, GUILayout.Width(150));

                    if (GUILayout.Button("ѡ��", GUILayout.Width(50)))
                    {
                        SelectEntity(chapter);
                    }

                    if (GUILayout.Button("ɾ��", GUILayout.Width(50)))
                    {
                        if (
                            EditorUtility.DisplayDialog(
                                "ȷ��ɾ��",
                                $"ȷ��Ҫɾ���½� '{chapterName}' ��",
                                "��",
                                "��"
                            )
                        )
                        {
                            // ɾ���½ڼ��������ӽڵ�
                            DeleteChapter(chapter);
                            RefreshTree();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("û���ҵ��κ��½�", MessageType.Info);
        }
    }

    // ɾ���½ڼ��������ӽڵ�
    private void DeleteChapter(Entity chapter)
    {
        try
        {
            // ��ȡ�����ӽڵ㣨�������ӽڵ�ȣ�
            var allDescendants = ECSFramework.Inst.GetAllDescendants(chapter);

            // ��ɾ�������ӽڵ�
            foreach (var descendant in allDescendants)
            {
                ECSFramework.Inst.RemoveEntity(descendant.Id);
            }

            // Ȼ��ɾ���½ڱ���
            ECSFramework.Inst.RemoveEntity(chapter.Id);

            Debug.Log($"��ɾ���½� ID: {chapter.Id} ���������ӽڵ�");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ɾ���½�ʱ��������: {ex.Message}");
        }
    }

    // Ϊ�½ڽڵ���Ʋ�����ť
    private void DrawChapterOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("���С��"))
            {
                // ʹ��Story.Inst����С��
                var newEpisode = Story.Inst.CreateEpisode(selectedEntity, "��С��");

                // ȷ�����ڵ�չ��
                expandedNodes[selectedEntity.Id] = true;

                // ѡ���´�����ʵ��
                SelectEntity(newEpisode);

                RefreshTree();
            }

            if (GUILayout.Button("�������С��"))
            {
                // ʹ��Story.Inst��������С��
                var episodes = Story.Inst.CreateEpisodes(selectedEntity, "С��1", "С��2", "С��3");

                // ȷ�����ڵ�չ��
                expandedNodes[selectedEntity.Id] = true;

                // ѡ���һ���´�����ʵ��
                if (episodes.Count > 0)
                {
                    SelectEntity(episodes[0]);
                }

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("��������"))
            {
                // ���������ӽڵ�
                var children = selectedEntity.Children();
                int order = 1;
                foreach (var child in children.OrderBy(c => Story.Inst.GetEntityOrder(c)))
                {
                    if (child.HasComponent<Comp.Order>())
                    {
                        child.GetComponent<Comp.Order>().Number = order;
                    }
                    if (child.HasComponent<Comp.Localization>())
                    {
                        child.GetComponent<Comp.Localization>().Number = order;
                    }
                    order++;
                }
                RefreshTree();
            }

            if (GUILayout.Button("���ɱ��ػ���"))
            {
                // Ϊ�����ӽڵ����ɱ��ػ���
                foreach (var child in selectedEntity.Children())
                {
                    if (child.HasComponent<Comp.Localization>())
                    {
                        var locComp = child.GetComponent<Comp.Localization>();
                        locComp.GenerateLocalizationKey(child, ECSFramework.Inst);
                    }
                }
                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        // ���ɾ����ť
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Σ�ղ���", EditorStyles.boldLabel);

        if (GUILayout.Button("ɾ�����½�", GUILayout.Height(30)))
        {
            if (
                EditorUtility.DisplayDialog(
                    "ȷ��ɾ��",
                    $"ȷ��Ҫɾ���½� '{GetEntityDisplayName(selectedEntity)}' �������������𣿴˲������ɻָ���",
                    "ɾ��",
                    "ȡ��"
                )
            )
            {
                // ��ȡ���ڵ�ID�Ա㱣��չ��״̬
                int? parentId = null;
                if (selectedEntity.HasComponent<Comp.Parent>())
                {
                    var parentComp = selectedEntity.GetComponent<Comp.Parent>();
                    parentId = parentComp.ParentId;
                }

                // ɾ���½ڼ��������ӽڵ�
                DeleteEntityRecursive(selectedEntity);

                // ���ָ��ڵ�չ��״̬
                if (parentId.HasValue)
                {
                    expandedNodes[parentId.Value] = true;
                }

                // ���ѡ��
                selectedEntity = null;

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ΪС�ڽڵ���Ʋ�����ť
    private void DrawEpisodeOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("��ӶԻ���"))
            {
                // ʹ��Story.Inst�����Ի���
                var newLine = Story.Inst.CreateLine(selectedEntity, "�¶Ի�");

                // ȷ�����ڵ�չ��
                expandedNodes[selectedEntity.Id] = true;

                // ѡ���´�����ʵ��
                SelectEntity(newLine);

                RefreshTree();
            }

            if (GUILayout.Button("������ӶԻ���"))
            {
                // ʹ��Story.Inst���������Ի���
                var lines = Story.Inst.CreateLines(selectedEntity, "�Ի�1", "�Ի�2", "�Ի�3");

                // ȷ�����ڵ�չ��
                expandedNodes[selectedEntity.Id] = true;

                // ѡ���һ���´�����ʵ��
                if (lines.Count > 0)
                {
                    SelectEntity(lines[0]);
                }

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            // ������ư�ť
            if (GUILayout.Button("����"))
            {
                // ���Ƶ�ǰС�� - ʹ��SwapEntities����
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex > 0)
                {
                    var prevEntity = siblings[currentIndex - 1];

                    // ͬʱ����ʵ��λ�ú�Order���
                    bool success = SwapEntitiesAndOrders(selectedEntity, prevEntity);

                    if (success)
                    {
                        // ���ָ��ڵ㣨�½ڣ�չ��״̬
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // ǿ������ˢ�½���
                        RefreshTreeImmediate();
                    }
                }
            }

            // ������ư�ť
            if (GUILayout.Button("����"))
            {
                // ���Ƶ�ǰС�� - ʹ��SwapEntities����
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex < siblings.Count - 1)
                {
                    var nextEntity = siblings[currentIndex + 1];

                    // ͬʱ����ʵ��λ�ú�Order���
                    bool success = SwapEntitiesAndOrders(selectedEntity, nextEntity);

                    if (success)
                    {
                        // ���ָ��ڵ㣨�½ڣ�չ��״̬
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // ǿ������ˢ�½���
                        RefreshTreeImmediate();
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("��������"))
            {
                // ���������ӽڵ�
                var children = selectedEntity.Children();
                int order = 1;
                foreach (var child in children.OrderBy(c => Story.Inst.GetEntityOrder(c)))
                {
                    if (child.HasComponent<Comp.Order>())
                    {
                        child.GetComponent<Comp.Order>().Number = order;
                    }
                    if (child.HasComponent<Comp.Localization>())
                    {
                        child.GetComponent<Comp.Localization>().Number = order;
                    }
                    order++;
                }
                RefreshTree();
            }

            if (GUILayout.Button("���ɱ��ػ���"))
            {
                // Ϊ�����ӽڵ����ɱ��ػ���
                foreach (var child in selectedEntity.Children())
                {
                    if (child.HasComponent<Comp.Localization>())
                    {
                        var locComp = child.GetComponent<Comp.Localization>();
                        locComp.GenerateLocalizationKey(child, ECSFramework.Inst);
                    }
                }
                RefreshTree();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        // ���ɾ����ť
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Σ�ղ���", EditorStyles.boldLabel);

        if (GUILayout.Button("ɾ����С��", GUILayout.Height(30)))
        {
            if (
                EditorUtility.DisplayDialog(
                    "ȷ��ɾ��",
                    $"ȷ��Ҫɾ��С�� '{GetEntityDisplayName(selectedEntity)}' �������������𣿴˲������ɻָ���",
                    "ɾ��",
                    "ȡ��"
                )
            )
            {
                // ��ȡ���ڵ�ID�Ա㱣��չ��״̬
                int? parentId = null;
                if (selectedEntity.HasComponent<Comp.Parent>())
                {
                    var parentComp = selectedEntity.GetComponent<Comp.Parent>();
                    parentId = parentComp.ParentId;
                }

                // ɾ��С�ڼ��������ӽڵ�
                DeleteEntityRecursive(selectedEntity);

                // ���ָ��ڵ�չ��״̬
                if (parentId.HasValue)
                {
                    expandedNodes[parentId.Value] = true;
                }

                // ���ѡ��
                selectedEntity = null;

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // Ϊ�Ի��нڵ���Ʋ�����ť
    private void DrawLineOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("����"))
            {
                // ���Ƶ�ǰ�Ի��� - ʹ��SwapEntities����
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex > 0)
                {
                    var prevEntity = siblings[currentIndex - 1];

                    // ͬʱ����ʵ��λ�ú�Order���
                    bool success = SwapEntitiesAndOrders(selectedEntity, prevEntity);

                    if (success)
                    {
                        // ���ָ��ڵ�չ��״̬
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // ǿ������ˢ�½���
                        RefreshTreeImmediate();
                    }
                }
            }

            if (GUILayout.Button("����"))
            {
                // ���Ƶ�ǰ�Ի��� - ʹ��SwapEntities����
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex < siblings.Count - 1)
                {
                    var nextEntity = siblings[currentIndex + 1];

                    // ͬʱ����ʵ��λ�ú�Order���
                    bool success = SwapEntitiesAndOrders(selectedEntity, nextEntity);

                    if (success)
                    {
                        // ���ָ��ڵ�չ��״̬
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // ǿ������ˢ�½���
                        RefreshTreeImmediate();
                    }
                }
            }
        }

        if (GUILayout.Button("����"))
        {
            // ���Ƶ�ǰ�Ի���
            var parent = selectedEntity.Parent();

            if (parent != null)
            {
                int nextOrder = Story.Inst.GetNextOrderNumber(parent.Id);

                if (selectedEntity.HasComponent<Comp.Localization>())
                {
                    var locComp = selectedEntity.GetComponent<Comp.Localization>();
                    var newLine = Story.Inst.CreateLine(
                        parent,
                        nextOrder,
                        locComp.DefaultText,
                        locComp.SpeakerKey
                    );

                    // ���ָ��ڵ�չ��״̬
                    expandedNodes[parent.Id] = true;

                    // �����´�����ʵ�壬չ�����Լ�
                    expandedNodes[newLine.Id] = true;

                    RefreshTree();
                }
            }
        }

        if (GUILayout.Button("ɾ��"))
        {
            // ɾ����ǰ�Ի���
            if (
                EditorUtility.DisplayDialog(
                    "ȷ��ɾ��",
                    $"ȷ��Ҫɾ���Ի��� '{GetEntityDisplayName(selectedEntity)}' ��",
                    "ɾ��",
                    "ȡ��"
                )
            )
            {
                // ��ȡ���ڵ�ID�Ա㱣��չ��״̬
                int? parentId = null;
                if (selectedEntity.HasComponent<Comp.Parent>())
                {
                    var parentComp = selectedEntity.GetComponent<Comp.Parent>();
                    parentId = parentComp.ParentId;
                }

                // ɾ���Ի���
                DeleteEntityRecursive(selectedEntity);

                // ���ָ��ڵ�չ��״̬
                if (parentId.HasValue)
                {
                    expandedNodes[parentId.Value] = true;
                }

                // ���ѡ��
                selectedEntity = null;

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // �ݹ�ɾ��ʵ�弰�������ӽڵ�
    private void DeleteEntityRecursive(Entity entity)
    {
        if (entity == null)
            return;

        try
        {
            // �Ȼ�ȡ����Ҫɾ����ʵ�壨�������˳��
            var entitiesToDelete = ECSFramework.Inst.TraverseDepthFirst(entity);

            // ���������˳��ɾ������ʵ��
            foreach (var entityToDelete in entitiesToDelete)
            {
                ECSFramework.Inst.RemoveEntity(entityToDelete.Id);
            }

            Debug.Log($"��ɾ��ʵ�� ID: {entity.Id} ���������ӽڵ�");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ɾ��ʵ��ʱ��������: {ex.Message}");
        }
    }

    // ͬʱ����ʵ��λ�ú�Order���
    private bool SwapEntitiesAndOrders(Entity entity1, Entity entity2)
    {
        try
        {
            // ���浱ǰ��Orderֵ
            int order1 = 0,
                order2 = 0;
            bool hasOrder1 = false,
                hasOrder2 = false;

            if (entity1.HasComponent<Comp.Order>())
            {
                order1 = entity1.GetComponent<Comp.Order>().Number;
                hasOrder1 = true;
            }

            if (entity2.HasComponent<Comp.Order>())
            {
                order2 = entity2.GetComponent<Comp.Order>().Number;
                hasOrder2 = true;
            }

            // ����ʵ��λ��
            bool swapSuccess = ECSFramework.Inst.SwapEntities(entity1, entity2);

            if (!swapSuccess)
            {
                return false;
            }

            // ����Orderֵ
            if (hasOrder1 && hasOrder2)
            {
                entity1.GetComponent<Comp.Order>().Number = order2;
                entity2.GetComponent<Comp.Order>().Number = order1;
            }
            else if (hasOrder1)
            {
                entity2.AddComponent(new Comp.Order(order1, ""));
            }
            else if (hasOrder2)
            {
                entity1.AddComponent(new Comp.Order(order2, ""));
            }

            // ���ʵ����Localization�����Ҳ�������е�Number
            if (entity1.HasComponent<Comp.Localization>())
            {
                entity1.GetComponent<Comp.Localization>().Number =
                    entity1.HasComponent<Comp.Order>()
                        ? entity1.GetComponent<Comp.Order>().Number
                        : 0;
            }

            if (entity2.HasComponent<Comp.Localization>())
            {
                entity2.GetComponent<Comp.Localization>().Number =
                    entity2.HasComponent<Comp.Order>()
                        ? entity2.GetComponent<Comp.Order>().Number
                        : 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"����ʵ���Orderʱ��������: {ex.Message}");
            return false;
        }
    }

    // ��ʾ��������Ի���
    private void ShowSwapComponentDialog()
    {
        // ��ȡ����ͬ���͵�ʵ��
        var allEntities = ECSFramework.Inst.GetAllEntities().ToList();
        var sameTypeEntities = allEntities
            .Where(e =>
                e.Id != selectedEntity.Id
                && e.HasComponent<Comp.Localization>()
                && e.GetComponent<Comp.Localization>().Type
                    == selectedEntity.GetComponent<Comp.Localization>().Type
            )
            .ToList();

        if (sameTypeEntities.Count == 0)
        {
            EditorUtility.DisplayDialog("��ʾ", "û���ҵ�ͬ���͵�����ʵ��", "ȷ��");
            return;
        }

        // ����ͨ�ò˵�
        GenericMenu menu = new GenericMenu();

        foreach (var entity in sameTypeEntities)
        {
            string menuText = $"ID: {entity.Id}";
            if (entity.HasComponent<Comp.Localization>())
            {
                var loc = entity.GetComponent<Comp.Localization>();
                if (!string.IsNullOrEmpty(loc.DefaultText))
                {
                    string shortText =
                        loc.DefaultText.Length > 30
                            ? loc.DefaultText.Substring(0, 30) + "..."
                            : loc.DefaultText;
                    menuText += $" - {shortText}";
                }
            }

            // ��Ӳ˵���
            menu.AddItem(
                new GUIContent(menuText),
                false,
                () =>
                {
                    // ִ���������
                    bool success = ECSFramework.Inst.SwapComponents(selectedEntity, entity);

                    if (success)
                    {
                        // ���ָ��ڵ�չ��״̬
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        RefreshTree();
                    }
                }
            );
        }

        // ��ʾ�˵�
        menu.ShowAsContext();
    }

    private void DrawRightPanel()
    {
        GUILayout.BeginArea(rightPanelRect, GUI.skin.box);
        componentScrollPosition = EditorGUILayout.BeginScrollView(componentScrollPosition);

        if (selectedEntity != null)
        {
            EditorGUILayout.LabelField("Component Editor", EditorStyles.boldLabel);

            // Component selector dropdown
            var componentTypes = selectedEntity.GetComponentTypes().ToList();
            if (componentTypes.Count > 0)
            {
                string[] componentNames = componentTypes.Select(t => t.Name).ToArray();

                // Ĭ��ѡ��Localization�����������ڣ�
                if (
                    selectedComponentType == null
                    && componentTypes.Contains(typeof(Comp.Localization))
                )
                {
                    selectedComponentType = typeof(Comp.Localization);
                }

                int currentIndex =
                    selectedComponentType != null
                        ? componentTypes.IndexOf(selectedComponentType)
                        : 0;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Select Component:");
                int newIndex = EditorGUILayout.Popup(currentIndex, componentNames);

                if (newIndex != currentIndex)
                {
                    selectedComponentType = componentTypes[newIndex];
                }

                // Draw component editor
                if (selectedComponentType != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(
                        $"Editing: {selectedComponentType.Name}",
                        EditorStyles.boldLabel
                    );

                    var component = selectedEntity.GetComponent(selectedComponentType);
                    DrawComponentEditor(component);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No components on this entity", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select an entity to edit components", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void SelectEntity(Entity entity)
    {
        selectedEntity = entity;

        // Ĭ��ѡ��Localization�����������ڣ�
        if (selectedEntity != null && selectedEntity.HasComponent<Comp.Localization>())
        {
            selectedComponentType = typeof(Comp.Localization);
        }
        else if (selectedEntity != null && selectedEntity.Components.Any())
        {
            selectedComponentType = selectedEntity.GetComponentTypes().First();
        }
        else
        {
            selectedComponentType = null;
        }

        Repaint();
    }

    private void DrawComponentEditor(IComponent component)
    {
        if (component is Comp.Order order)
        {
            EditorGUILayout.Space();
            order.Number = EditorGUILayout.IntField("Number", order.Number);
            order.Label = EditorGUILayout.TextField("Label", order.Label);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Formatted Label: " + order.FormattedLabel,
                EditorStyles.helpBox
            );

            // ���Ԥ�谴ť
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set as Chapter"))
            {
                order.Number = order.Number;
                order.Label = $"��{order.Number}��";
            }
            if (GUILayout.Button("Set as Episode"))
            {
                order.Number = order.Number;
                order.Label = $"��{order.Number}��";
            }
            if (GUILayout.Button("Set as Line"))
            {
                order.Number = order.Number;
                order.Label = $"{order.Number}.";
            }
            EditorGUILayout.EndHorizontal();
        }
        else if (component is Comp.Localization localization)
        {
            EditorGUILayout.Space();
            localization.Type = (Comp.Localization.NodeType)
                EditorGUILayout.EnumPopup("Type", localization.Type);
            localization.DefaultText = EditorGUILayout.TextField(
                "Default Text",
                localization.DefaultText
            );
            localization.Number = EditorGUILayout.IntField("Number", localization.Number);
            localization.SpeakerKey = EditorGUILayout.TextField(
                "Speaker Key",
                localization.SpeakerKey
            );

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Localization Key"))
            {
                localization.GenerateLocalizationKey(selectedEntity, ECSFramework.Inst);
            }

            EditorGUILayout.LabelField(
                "Context Key: " + localization.ContextKey,
                EditorStyles.helpBox
            );
        }
        else if (component is Comp.Root root)
        {
            EditorGUILayout.Space();
            root.RootName = EditorGUILayout.TextField("Root Name", root.RootName);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"No custom editor for {component.GetType().Name}",
                MessageType.Info
            );
        }
    }

    private void DrawSplitters()
    {
        // ���Ʒָ���
        EditorGUI.DrawRect(splitterLeftRect, Color.gray);
        EditorGUI.DrawRect(splitterMiddleRect, Color.gray);
    }

    private void HandleEvents()
    {
        // ����ָ�����ק
        Event currentEvent = Event.current;

        // ���ָ���
        if (
            currentEvent.type == EventType.MouseDown
            && splitterLeftRect.Contains(currentEvent.mousePosition)
        )
        {
            isResizingLeft = true;
            currentEvent.Use();
        }

        // �м�ָ���
        if (
            currentEvent.type == EventType.MouseDown
            && splitterMiddleRect.Contains(currentEvent.mousePosition)
        )
        {
            isResizingMiddle = true;
            currentEvent.Use();
        }

        // ����ͷ�
        if (currentEvent.type == EventType.MouseUp)
        {
            isResizingLeft = false;
            isResizingMiddle = false;
        }

        // ������ק
        if (isResizingLeft && currentEvent.type == EventType.MouseDrag)
        {
            leftPanelWidth += currentEvent.delta.x;
            leftPanelWidth = Mathf.Clamp(
                leftPanelWidth,
                200,
                position.width - middlePanelWidth - rightPanelWidth - splitterWidth * 2 - 100
            );
            currentEvent.Use();
            Repaint();
        }

        if (isResizingMiddle && currentEvent.type == EventType.MouseDrag)
        {
            middlePanelWidth += currentEvent.delta.x;
            middlePanelWidth = Mathf.Clamp(
                middlePanelWidth,
                200,
                position.width - leftPanelWidth - rightPanelWidth - splitterWidth * 2 - 100
            );
            currentEvent.Use();
            Repaint();
        }

        // �����Ҳ������
        rightPanelWidth = position.width - leftPanelWidth - middlePanelWidth - splitterWidth * 2;
    }

    private string GetEntityDisplayName(Entity entity)
    {
        string displayName = $"ID: {entity.Id}";

        // Try to get Order component for better display
        if (entity.HasComponent<Comp.Order>())
        {
            var order = entity.GetComponent<Comp.Order>();
            displayName = $"{order.FormattedLabel} (ID: {entity.Id})";
        }

        // Add Localization info if available
        if (entity.HasComponent<Comp.Localization>())
        {
            var loc = entity.GetComponent<Comp.Localization>();
            if (!string.IsNullOrEmpty(loc.DefaultText))
            {
                string shortText =
                    loc.DefaultText.Length > 20
                        ? loc.DefaultText.Substring(0, 20) + "..."
                        : loc.DefaultText;
                displayName += $" - {shortText}";
            }
        }

        // Add Root marker if applicable
        if (entity.HasComponent<Comp.Root>())
        {
            var root = entity.GetComponent<Comp.Root>();
            if (!string.IsNullOrEmpty(root.RootName))
            {
                displayName = $"{root.RootName} - {displayName}";
            }
            else
            {
                displayName = $"Root - {displayName}";
            }
        }

        return displayName;
    }

    private void RefreshTree()
    {
        // �������ڵ�ʵ���չ��״̬
        var allEntityIds = ECSFramework.Inst.GetAllEntities().Select(e => e.Id).ToHashSet();
        var keysToRemove = expandedNodes.Keys.Where(id => !allEntityIds.Contains(id)).ToList();
        foreach (var key in keysToRemove)
        {
            expandedNodes.Remove(key);
        }

        ECSFramework.Inst.RebuildEntityReferences();
        Repaint();
    }

    // ���һ������ˢ�µķ���
    private void RefreshTreeImmediate()
    {
        // �������ڵ�ʵ���չ��״̬
        var allEntityIds = ECSFramework.Inst.GetAllEntities().Select(e => e.Id).ToHashSet();
        var keysToRemove = expandedNodes.Keys.Where(id => !allEntityIds.Contains(id)).ToList();
        foreach (var key in keysToRemove)
        {
            expandedNodes.Remove(key);
        }

        ECSFramework.Inst.RebuildEntityReferences();

        // ǿ�������ػ�
        Repaint();

        // �����Ҫ��������ǿ�����²���
        GUI.changed = true;
    }

    private void ExpandAll()
    {
        var allEntities = ECSFramework.Inst.GetAllEntities().ToList();
        foreach (var entity in allEntities)
        {
            expandedNodes[entity.Id] = true;
        }
        Repaint();
    }

    private void CollapseAll()
    {
        var allEntities = ECSFramework.Inst.GetAllEntities().ToList();
        foreach (var entity in allEntities)
        {
            expandedNodes[entity.Id] = false;
        }
        Repaint();
    }

    private void OnFocus()
    {
        // Refresh when window gains focus
        RefreshTree();
    }
}
