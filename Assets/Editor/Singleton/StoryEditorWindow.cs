using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
        // 初始化面板大小
        leftPanelWidth = 300f;
        middlePanelWidth = 400f;
        rightPanelWidth = position.width - leftPanelWidth - middlePanelWidth - splitterWidth * 2;
    }

    private void OnGUI()
    {
        DrawToolbar();

        // 计算面板区域
        CalculatePanelRects();

        // 绘制左侧面板（树状结构）
        DrawLeftPanel();

        // 绘制中间面板（选中节点状态）
        DrawMiddlePanel();

        // 绘制右侧面板（组件编辑器）
        DrawRightPanel();

        // 绘制分隔线
        DrawSplitters();

        // 处理鼠标事件
        HandleEvents();

        // 重置悬停状态
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

            // 添加Save按钮
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Story.Inst.SaveForget("story.zip", true);
            }

            // 添加Load按钮
            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                Story.Inst.LoadForget("story.zip", true);
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

        // 上方：操作按钮区域
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(100));
        {
            EditorGUILayout.LabelField("节点操作", EditorStyles.boldLabel);

            if (selectedEntity != null)
            {
                // 首先检查是否是根节点
                if (selectedEntity.HasComponent<Comp.Root>())
                {
                    DrawRootOperations();
                }
                // 然后检查节点类型并显示相应的操作按钮
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
                            EditorGUILayout.HelpBox("未知节点类型", MessageType.Info);
                            break;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("此节点没有Localization组件", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个节点以显示操作", MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();

        // 下方：节点详细信息区域
        detailsScrollPosition = EditorGUILayout.BeginScrollView(detailsScrollPosition);
        {
            if (selectedEntity != null)
            {
                EditorGUILayout.LabelField("节点详细信息", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField($"ID: {selectedEntity.Id}");

                    // 显示所有组件
                    foreach (var component in selectedEntity.Components)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(
                            component.GetType().Name,
                            EditorStyles.boldLabel
                        );

                        // 处理特定组件类型
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
                            // 通用组件显示
                            EditorGUILayout.LabelField(component.ToString());
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("选择一个节点以查看详细信息", MessageType.Info);
            }
        }
        EditorGUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void DrawEntityNode(Entity entity, int depth)
    {
        if (entity == null)
            return;

        // 初始化展开状态
        if (!expandedNodes.ContainsKey(entity.Id))
        {
            expandedNodes[entity.Id] = depth < 3; // 默认展开全部
        }

        // 检查是否有子节点
        bool hasChildren =
            entity.HasComponent<Comp.Children>()
            && entity.GetComponent<Comp.Children>().ChildrenEntities.Count > 0;

        // 使用水平组绘制节点行
        EditorGUILayout.BeginHorizontal(GUILayout.Height(18)); // 固定行高
        {
            // 根据深度缩进
            GUILayout.Space(depth * 14);

            // 绘制折叠箭头（如果有子节点）
            if (hasChildren)
            {
                // 获取一个小的矩形区域用于箭头
                Rect foldoutRect = GUILayoutUtility.GetRect(14, 18, GUILayout.Width(14));

                // 调整箭头位置，向下移动2像素
                foldoutRect.y += 2;

                // 绘制箭头图标
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
                // 无子节点时使用空白占位符保持对齐
                GUILayout.Space(16);
            }

            // 绘制节点标签
            string displayName = GetEntityDisplayName(entity);

            // 获取标签的矩形区域
            Rect labelRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));

            // 调整文本位置，向下移动1像素
            labelRect.y += 1;

            // 检查是否悬停或选中
            bool isHovered = labelRect.Contains(Event.current.mousePosition);
            bool isSelected = selectedEntity != null && selectedEntity.Id == entity.Id;

            if (isHovered)
            {
                hoverEntityId = entity.Id;
            }

            // 高亮背景
            if (isSelected || isHovered)
            {
                Color highlightColor = isSelected
                    ? new Color(0.2f, 0.4f, 0.8f, 0.3f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.3f);
                EditorGUI.DrawRect(labelRect, highlightColor);
            }

            // 绘制标签文本
            EditorGUI.LabelField(labelRect, displayName);

            // 处理点击事件 - 整个标签区域可点击选择实体
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

        // 绘制子节点（如果展开且有子节点）
        if (expandedNodes[entity.Id] && hasChildren)
        {
            var children = entity.GetComponent<Comp.Children>().ChildrenEntities;

            // 按Order组件排序子节点
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

            // 递归绘制子节点
            foreach (var child in sortedChildren)
            {
                DrawEntityNode(child, depth + 1);
            }
        }
    }

    // 为根节点绘制操作按钮
    private void DrawRootOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("添加章节"))
            {
                // 使用Story.Inst创建章节
                var newChapter = Story.Inst.CreateChapter("第一章");

                // 自动生成localization key并设置speaker key
                if (newChapter.HasComponent<Comp.Localization>())
                {
                    var locComp = newChapter.GetComponent<Comp.Localization>();
                    locComp.SpeakerKey = "MAIN"; // 设置初始speaker key为Main
                    locComp.GenerateLocalizationKey(newChapter, ECSFramework.Inst).Forget();
                }

                // 确保根节点展开
                expandedNodes[selectedEntity.Id] = true;

                // 选择新创建的实体
                SelectEntity(newChapter);

                RefreshTree();
            }

            if (GUILayout.Button("批量添加章节"))
            {
                // 使用Story.Inst批量创建章节
                var newChapters = Story.Inst.CreateChapters("第一章", "第二章", "第三章");

                // 为每个新创建的章节生成localization key并设置speaker key
                foreach (var chapter in newChapters)
                {
                    if (chapter.HasComponent<Comp.Localization>())
                    {
                        var locComp = chapter.GetComponent<Comp.Localization>();
                        locComp.SpeakerKey = "MAIN"; // 设置初始speaker key为Main
                        _ = locComp.GenerateLocalizationKey(chapter, ECSFramework.Inst);
                    }
                }

                // 确保根节点展开
                expandedNodes[selectedEntity.Id] = true;

                // 选择第一个新创建的实体
                if (newChapters.Count > 0)
                {
                    SelectEntity(newChapters[0]);
                }

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();

        /*

        // 添加自定义批量创建章节的功能
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("自定义批量创建章节"))
            {
                ShowCustomCreateChaptersDialog();
            }

            if (GUILayout.Button("高级批量创建"))
            {
                ShowAdvancedCreateChaptersDialog();
            }
        }
        EditorGUILayout.EndHorizontal();

        */

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("重新排序"))
            {
                // 重新排序子节点
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

            if (GUILayout.Button("生成本地化键"))
            {
                // 为所有子节点生成本地化键
                foreach (var child in selectedEntity.Children())
                {
                    if (child.HasComponent<Comp.Localization>())
                    {
                        var locComp = child.GetComponent<Comp.Localization>();
                        locComp.GenerateLocalizationKey(child, ECSFramework.Inst).Forget();
                    }
                }
                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();

        // 添加删除章节的功能
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("章节管理", EditorStyles.boldLabel);

        // 显示所有章节列表并提供删除选项
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

                    if (GUILayout.Button("选择", GUILayout.Width(50)))
                    {
                        SelectEntity(chapter);
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        if (
                            EditorUtility.DisplayDialog(
                                "确认删除",
                                $"确定要删除章节 '{chapterName}' 吗？",
                                "是",
                                "否"
                            )
                        )
                        {
                            // 删除章节及其所有子节点
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
            EditorGUILayout.HelpBox("没有找到任何章节", MessageType.Info);
        }
    }

    // 添加重新排序和更新本地化的方法
    private void ReorderAndUpdateLocalization(Entity parent)
    {
        if (parent == null || !parent.HasComponent<Comp.Children>())
            return;

        var children = parent.GetComponent<Comp.Children>().ChildrenEntities;

        // 按当前Order排序（因为可能有些子节点已经被删除了，需要重新编号）
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

        // 重新编号并更新本地化
        for (int i = 0; i < sortedChildren.Count; i++)
        {
            var child = sortedChildren[i];
            int newOrder = i + 1;

            // 更新Order组件
            if (child.HasComponent<Comp.Order>())
            {
                child.GetComponent<Comp.Order>().Number = newOrder;
            }

            // 更新Localization组件
            if (child.HasComponent<Comp.Localization>())
            {
                var locComp = child.GetComponent<Comp.Localization>();
                locComp.Number = newOrder;

                // 重新生成本地化键（这会更新ContextKey和本地化条目）
                locComp.GenerateLocalizationKey(child, ECSFramework.Inst).Forget();
            }
        }
    }

    // 修改DeleteChapter方法，在删除后重新排序
    private void DeleteChapter(Entity chapter)
    {
        try
        {
            // 获取父节点（根节点）
            var parent = chapter.Parent();

            // 获取所有子节点（包括孙子节点等）
            var allDescendants = ECSFramework.Inst.GetAllDescendants(chapter);

            // 先删除所有子节点
            foreach (var descendant in allDescendants)
            {
                ECSFramework.Inst.RemoveEntity(descendant.Id);
            }

            // 然后删除章节本身
            ECSFramework.Inst.RemoveEntity(chapter.Id);

            // 重新排序父节点的所有子节点并更新本地化
            if (parent != null)
            {
                ReorderAndUpdateLocalization(parent);
            }

            Debug.Log($"已删除章节 ID: {chapter.Id} 及其所有子节点");
        }
        catch (Exception ex)
        {
            Debug.LogError($"删除章节时发生错误: {ex.Message}");
        }
    }

    // 为章节节点绘制操作按钮
    private void DrawChapterOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("添加小节"))
            {
                // 使用Story.Inst创建小节
                var newEpisode = Story.Inst.CreateEpisode(selectedEntity, "第一节");

                // 自动生成localization key并设置speaker key
                if (newEpisode.HasComponent<Comp.Localization>())
                {
                    var locComp = newEpisode.GetComponent<Comp.Localization>();
                    locComp.SpeakerKey = "MAIN"; // 设置初始speaker key为Main
                    locComp.GenerateLocalizationKey(newEpisode, ECSFramework.Inst).Forget();
                }

                // 确保父节点展开
                expandedNodes[selectedEntity.Id] = true;

                // 选择新创建的实体
                SelectEntity(newEpisode);

                RefreshTree();
            }

            if (GUILayout.Button("批量添加小节"))
            {
                // 使用Story.Inst批量创建小节
                var episodes = Story.Inst.CreateEpisodes(
                    selectedEntity,
                    "第一节",
                    "第二节",
                    "第三节",
                    "第四节",
                    "第五节"
                );

                // 为每个新创建的小节生成localization key并设置speaker key
                foreach (var episode in episodes)
                {
                    if (episode.HasComponent<Comp.Localization>())
                    {
                        var locComp = episode.GetComponent<Comp.Localization>();
                        locComp.SpeakerKey = "MAIN"; // 设置初始speaker key为Main
                        locComp.GenerateLocalizationKey(episode, ECSFramework.Inst).Forget();
                    }
                }

                // 确保父节点展开
                expandedNodes[selectedEntity.Id] = true;

                // 选择第一个新创建的实体
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
            if (GUILayout.Button("重新排序"))
            {
                // 重新排序子节点
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

            if (GUILayout.Button("生成本地化键"))
            {
                // 为所有子节点生成本地化键
                foreach (var child in selectedEntity.Children())
                {
                    if (child.HasComponent<Comp.Localization>())
                    {
                        var locComp = child.GetComponent<Comp.Localization>();
                        locComp.GenerateLocalizationKey(child, ECSFramework.Inst).Forget();
                    }
                }
                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        // 添加删除按钮
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("危险操作", EditorStyles.boldLabel);

        if (GUILayout.Button("删除本章节", GUILayout.Height(30)))
        {
            if (
                EditorUtility.DisplayDialog(
                    "确认删除",
                    $"确定要删除章节 '{GetEntityDisplayName(selectedEntity)}' 及其所有内容吗？此操作不可恢复！",
                    "删除",
                    "取消"
                )
            )
            {
                // 获取父节点ID以便保持展开状态
                int? parentId = null;
                if (selectedEntity.HasComponent<Comp.Parent>())
                {
                    var parentComp = selectedEntity.GetComponent<Comp.Parent>();
                    parentId = parentComp.ParentId;
                }

                // 删除章节及其所有子节点
                DeleteEntityRecursive(selectedEntity);

                // 保持父节点展开状态
                if (parentId.HasValue)
                {
                    expandedNodes[parentId.Value] = true;
                }

                // 清除选择
                selectedEntity = null;

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // 为小节节点绘制操作按钮
    private void DrawEpisodeOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("添加对话行"))
            {
                // 使用Story.Inst创建对话行
                var newLine = Story.Inst.CreateLine(selectedEntity, "第一行");

                // 自动生成localization key并设置speaker key
                if (newLine.HasComponent<Comp.Localization>())
                {
                    var locComp = newLine.GetComponent<Comp.Localization>();
                    locComp.SpeakerKey = "MAIN"; // 设置初始speaker key为Main
                    locComp.GenerateLocalizationKey(newLine, ECSFramework.Inst).Forget();
                }

                // 确保父节点展开
                expandedNodes[selectedEntity.Id] = true;

                // 选择新创建的实体
                SelectEntity(newLine);

                RefreshTree();
            }

            if (GUILayout.Button("批量添加对话行"))
            {
                // 使用Story.Inst批量创建对话行
                var lines = Story.Inst.CreateLines(
                    selectedEntity,
                    "第一行",
                    "第二行",
                    "第三行",
                    "第四行",
                    "第五行"
                );

                // 为每个新创建的对话行生成localization key并设置speaker key
                foreach (var line in lines)
                {
                    if (line.HasComponent<Comp.Localization>())
                    {
                        var locComp = line.GetComponent<Comp.Localization>();
                        locComp.SpeakerKey = "MAIN"; // 设置初始speaker key为Main
                        locComp.GenerateLocalizationKey(line, ECSFramework.Inst).Forget();
                    }
                }

                // 确保父节点展开
                expandedNodes[selectedEntity.Id] = true;

                // 选择第一个新创建的实体
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
            // 添加上移按钮
            if (GUILayout.Button("上移"))
            {
                // 上移当前小节 - 使用SwapEntities方法
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex > 0)
                {
                    var prevEntity = siblings[currentIndex - 1];

                    // 同时交换实体位置和Order组件
                    bool success = SwapEntitiesAndOrders(selectedEntity, prevEntity);

                    if (success)
                    {
                        // 保持父节点（章节）展开状态
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // 强制立即刷新界面
                        RefreshTreeImmediate();
                    }
                }
            }

            // 添加下移按钮
            if (GUILayout.Button("下移"))
            {
                // 下移当前小节 - 使用SwapEntities方法
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex < siblings.Count - 1)
                {
                    var nextEntity = siblings[currentIndex + 1];

                    // 同时交换实体位置和Order组件
                    bool success = SwapEntitiesAndOrders(selectedEntity, nextEntity);

                    if (success)
                    {
                        // 保持父节点（章节）展开状态
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // 强制立即刷新界面
                        RefreshTreeImmediate();
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("重新排序"))
            {
                // 重新排序子节点
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

            if (GUILayout.Button("生成本地化键"))
            {
                // 为所有子节点生成本地化键
                foreach (var child in selectedEntity.Children())
                {
                    if (child.HasComponent<Comp.Localization>())
                    {
                        var locComp = child.GetComponent<Comp.Localization>();
                        locComp.GenerateLocalizationKey(child, ECSFramework.Inst).Forget();
                    }
                }
                RefreshTree();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        // 添加删除按钮
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("危险操作", EditorStyles.boldLabel);

        if (GUILayout.Button("删除本小节", GUILayout.Height(30)))
        {
            if (
                EditorUtility.DisplayDialog(
                    "确认删除",
                    $"确定要删除小节 '{GetEntityDisplayName(selectedEntity)}' 及其所有内容吗？此操作不可恢复！",
                    "删除",
                    "取消"
                )
            )
            {
                // 获取父节点ID以便保持展开状态
                int? parentId = null;
                if (selectedEntity.HasComponent<Comp.Parent>())
                {
                    var parentComp = selectedEntity.GetComponent<Comp.Parent>();
                    parentId = parentComp.ParentId;
                }

                // 删除小节及其所有子节点
                DeleteEntityRecursive(selectedEntity);

                // 保持父节点展开状态
                if (parentId.HasValue)
                {
                    expandedNodes[parentId.Value] = true;
                }

                // 清除选择
                selectedEntity = null;

                RefreshTree();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLineOperations()
    {
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("上移"))
            {
                // 上移当前对话行 - 使用SwapEntities方法
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex > 0)
                {
                    var prevEntity = siblings[currentIndex - 1];

                    // 同时交换实体位置和Order组件
                    bool success = SwapEntitiesAndOrders(selectedEntity, prevEntity);

                    if (success)
                    {
                        // 保持父节点展开状态
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // 强制立即刷新界面
                        RefreshTreeImmediate();
                    }
                }
            }

            if (GUILayout.Button("下移"))
            {
                // 下移当前对话行 - 使用SwapEntities方法
                var siblings = Story.Inst.GetSiblingsOrdered(selectedEntity.Id);
                int currentIndex = siblings.FindIndex(e => e.Id == selectedEntity.Id);

                if (currentIndex < siblings.Count - 1)
                {
                    var nextEntity = siblings[currentIndex + 1];

                    // 同时交换实体位置和Order组件
                    bool success = SwapEntitiesAndOrders(selectedEntity, nextEntity);

                    if (success)
                    {
                        // 保持父节点展开状态
                        var parent = selectedEntity.Parent();
                        if (parent != null)
                        {
                            expandedNodes[parent.Id] = true;
                        }

                        // 强制立即刷新界面
                        RefreshTreeImmediate();
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("复制"))
            {
                // 复制当前对话行
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

                        // 保持父节点展开状态
                        expandedNodes[parent.Id] = true;

                        // 对于新创建的实体，展开它自己
                        expandedNodes[newLine.Id] = true;

                        RefreshTree();
                    }
                }
            }

            // 添加选择分支按钮
            if (GUILayout.Button("添加选择分支"))
            {
                AddChoiceComponentToLine();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("删除"))
            {
                // 删除当前对话行
                if (
                    EditorUtility.DisplayDialog(
                        "确认删除",
                        $"确定要删除对话行 '{GetEntityDisplayName(selectedEntity)}' 吗？",
                        "删除",
                        "取消"
                    )
                )
                {
                    // 获取父节点ID以便保持展开状态
                    int? parentId = null;
                    if (selectedEntity.HasComponent<Comp.Parent>())
                    {
                        var parentComp = selectedEntity.GetComponent<Comp.Parent>();
                        parentId = parentComp.ParentId;
                    }

                    // 删除对话行
                    DeleteEntityRecursive(selectedEntity);

                    // 保持父节点展开状态
                    if (parentId.HasValue)
                    {
                        expandedNodes[parentId.Value] = true;
                    }

                    // 清除选择
                    selectedEntity = null;

                    RefreshTree();
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // 递归删除实体及其所有子节点
    private void DeleteEntityRecursive(Entity entity)
    {
        if (entity == null)
            return;

        try
        {
            // 获取父节点
            var parent = entity.Parent();

            // 先获取所有要删除的实体（深度优先顺序）
            var entitiesToDelete = ECSFramework.Inst.TraverseDepthFirst(entity);

            // 按深度优先顺序删除所有实体
            foreach (var entityToDelete in entitiesToDelete)
            {
                ECSFramework.Inst.RemoveEntity(entityToDelete.Id);
            }

            // 重新排序父节点的所有子节点并更新本地化
            if (parent != null)
            {
                ReorderAndUpdateLocalization(parent);
            }

            Debug.Log($"已删除实体 ID: {entity.Id} 及其所有子节点");
        }
        catch (Exception ex)
        {
            Debug.LogError($"删除实体时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 给当前选中的Line节点添加Choice组件
    /// </summary>
    private void AddChoiceComponentToLine()
    {
        if (selectedEntity == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个对话行节点", "确定");
            return;
        }

        // 检查是否已经是Line类型
        if (!selectedEntity.HasComponent<Comp.Localization>())
        {
            EditorUtility.DisplayDialog("错误", "选中的节点没有Localization组件", "确定");
            return;
        }

        var locComp = selectedEntity.GetComponent<Comp.Localization>();
        if (locComp.Type != Comp.Localization.NodeType.Line)
        {
            EditorUtility.DisplayDialog("错误", "只能给对话行节点添加选择分支", "确定");
            return;
        }

        // 检查是否已经存在Choice组件
        if (selectedEntity.HasComponent<Comp.Choice>())
        {
            if (
                EditorUtility.DisplayDialog(
                    "提示",
                    "该节点已经存在选择分支组件，是否要替换？",
                    "替换",
                    "取消"
                )
            )
            {
                // 移除现有的Choice组件
                selectedEntity.RemoveComponent<Comp.Choice>();
            }
            else
            {
                return;
            }
        }

        try
        {
            // 创建新的Choice组件并添加到实体
            var choiceComponent = new Comp.Choice(Comp.SelectionType.Custom);
            selectedEntity.AddComponent(choiceComponent);

            // 刷新界面
            RefreshTree();

            // 自动选择新添加的Choice组件以便编辑
            selectedComponentType = typeof(Comp.Choice);

            Debug.Log($"已为对话行 ID: {selectedEntity.Id} 添加选择分支组件");
        }
        catch (Exception ex)
        {
            Debug.LogError($"添加Choice组件时发生错误: {ex.Message}");
            EditorUtility.DisplayDialog("错误", $"添加选择分支失败: {ex.Message}", "确定");
        }
    }

    // 同时交换实体位置和Order组件
    private bool SwapEntitiesAndOrders(Entity entity1, Entity entity2)
    {
        try
        {
            // 保存当前的Order值
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

            // 交换实体位置
            bool swapSuccess = ECSFramework.Inst.SwapEntities(entity1, entity2);

            if (!swapSuccess)
            {
                return false;
            }

            // 交换Order值
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

            // 如果实体有Localization组件，也更新其中的Number
            if (entity1.HasComponent<Comp.Localization>())
            {
                entity1.GetComponent<Comp.Localization>().Number =
                    entity1.HasComponent<Comp.Order>()
                        ? entity1.GetComponent<Comp.Order>().Number
                        : 0;
                // 重新生成localization key
                entity1
                    .GetComponent<Comp.Localization>()
                    .GenerateLocalizationKey(entity1, ECSFramework.Inst)
                    .Forget();
            }

            if (entity2.HasComponent<Comp.Localization>())
            {
                entity2.GetComponent<Comp.Localization>().Number =
                    entity2.HasComponent<Comp.Order>()
                        ? entity2.GetComponent<Comp.Order>().Number
                        : 0;
                // 重新生成localization key
                entity2
                    .GetComponent<Comp.Localization>()
                    .GenerateLocalizationKey(entity2, ECSFramework.Inst)
                    .Forget();
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"交换实体和Order时发生错误: {ex.Message}");
            return false;
        }
    }

    // 显示交换组件对话框
    private void ShowSwapComponentDialog()
    {
        // 获取所有同类型的实体
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
            EditorUtility.DisplayDialog("提示", "没有找到同类型的其他实体", "确定");
            return;
        }

        // 创建通用菜单
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

            // 添加菜单项
            menu.AddItem(
                new GUIContent(menuText),
                false,
                () =>
                {
                    // 执行组件交换
                    bool success = ECSFramework.Inst.SwapComponents(selectedEntity, entity);

                    if (success)
                    {
                        // 保持父节点展开状态
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

        // 显示菜单
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

                // 默认选择Localization组件（如果存在）
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

        // 默认选择Localization组件（如果存在）
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
                localization.GenerateLocalizationKey(selectedEntity, ECSFramework.Inst).Forget();
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
        else if (component is Comp.Choice choice)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Choice Component Editor", EditorStyles.boldLabel);

            // 选择类型
            choice.Type = (Comp.SelectionType)
                EditorGUILayout.EnumPopup("Selection Type", choice.Type);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Choice Options", EditorStyles.boldLabel);

            // 显示当前选项列表
            if (choice.Options.Count == 0)
            {
                EditorGUILayout.HelpBox("No choice options added", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < choice.Options.Count; i++)
                {
                    var option = choice.Options[i];
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.LabelField($"Option {i + 1}", EditorStyles.boldLabel);

                        var targetEntity = ECSFramework.Inst.GetEntitySafe(option.TargetId);
                        string targetDisplay =
                            targetEntity != null
                                ? GetEntityDisplayName(targetEntity)
                                : $"ID: {option.TargetId} (Missing)";

                        EditorGUILayout.LabelField("Target:", targetDisplay);

                        // 显示文本编辑
                        option.DisplayText = EditorGUILayout.TextField(
                            "Display Text",
                            option.DisplayText
                        );

                        // 本地化键编辑
                        EditorGUILayout.BeginHorizontal();
                        {
                            option.LocalizationKey = EditorGUILayout.TextField(
                                "Localization Key",
                                option.LocalizationKey
                            );

                            if (GUILayout.Button("Auto", GUILayout.Width(50)))
                            {
                                // 自动生成本地化键
                                if (
                                    selectedEntity != null
                                    && selectedEntity.HasComponent<Comp.Localization>()
                                )
                                {
                                    var currentLoc =
                                        selectedEntity.GetComponent<Comp.Localization>();
                                    option.LocalizationKey = $"{currentLoc.ContextKey}_C{i + 1}";
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        // 操作按钮
                        EditorGUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("Select Target"))
                            {
                                if (targetEntity != null)
                                {
                                    SelectEntity(targetEntity);
                                }
                            }

                            if (GUILayout.Button("Use Target Text"))
                            {
                                if (
                                    targetEntity != null
                                    && targetEntity.HasComponent<Comp.Localization>()
                                )
                                {
                                    var targetLoc = targetEntity.GetComponent<Comp.Localization>();
                                    option.DisplayText = targetLoc.DefaultText;
                                }
                            }

                            if (GUILayout.Button("Remove"))
                            {
                                choice.Options.RemoveAt(i);
                                i--; // 调整索引
                                GUI.changed = true;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.Space();

            // 添加选项的按钮
            if (GUILayout.Button("Add Choice Option"))
            {
                ShowAddChoiceOptionDialog(choice);
            }

            // 批量添加兄弟节点作为选项
            if (GUILayout.Button("Add All Siblings as Options"))
            {
                AddAllSiblingsAsOptions(choice);
            }

            // 自动生成本地化键
            if (GUILayout.Button("Auto Generate Localization Keys"))
            {
                choice.GenerateLocalizationKeysForOptions(selectedEntity, ECSFramework.Inst);
                GUI.changed = true;
            }

            // 清空所有选项
            if (choice.Options.Count > 0 && GUILayout.Button("Clear All Options"))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Confirm Clear",
                        "Clear all choice options?",
                        "Yes",
                        "No"
                    )
                )
                {
                    choice.Options.Clear();
                    GUI.changed = true;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Total Options: {choice.Options.Count}",
                EditorStyles.helpBox
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"No custom editor for {component.GetType().Name}",
                MessageType.Info
            );
        }
    }

    /// <summary>
    /// 显示添加选择选项的对话框
    /// </summary>
    private void ShowAddChoiceOptionDialog(Comp.Choice choice)
    {
        var availableEntities = Story.Inst.GetAvailableTargetEntities(selectedEntity);

        if (availableEntities.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No available target entities", "OK");
            return;
        }

        GenericMenu menu = new GenericMenu();

        foreach (var entity in availableEntities)
        {
            string menuText = GetEntityDisplayName(entity);
            menu.AddItem(
                new GUIContent(menuText),
                false,
                () =>
                {
                    // 获取目标实体的默认文本作为显示文本
                    string displayText = string.Empty;
                    if (entity.HasComponent<Comp.Localization>())
                    {
                        displayText = entity.GetComponent<Comp.Localization>().DefaultText;
                    }

                    choice.AddOption(entity.Id, displayText);
                    GUI.changed = true;
                    Debug.Log($"Added choice option: {menuText}");
                }
            );
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// 添加所有兄弟节点作为选项
    /// </summary>
    private void AddAllSiblingsAsOptions(Comp.Choice choice)
    {
        if (selectedEntity == null)
            return;

        var siblings = Story
            .Inst.GetSiblingsOrdered(selectedEntity.Id)
            .Where(sibling => sibling.Id != selectedEntity.Id)
            .ToList();

        if (siblings.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No siblings found", "OK");
            return;
        }

        int addedCount = 0;
        foreach (var sibling in siblings)
        {
            if (!choice.Options.Any(o => o.TargetId == sibling.Id))
            {
                string displayText = string.Empty;
                if (sibling.HasComponent<Comp.Localization>())
                {
                    displayText = sibling.GetComponent<Comp.Localization>().DefaultText;
                }

                choice.AddOption(sibling.Id, displayText);
                addedCount++;
            }
        }

        // 自动生成本地化键
        choice.GenerateLocalizationKeysForOptions(selectedEntity, ECSFramework.Inst);

        GUI.changed = true;
        Debug.Log($"Added {addedCount} siblings as options");
    }

    /// <summary>
    /// 显示添加目标实体的对话框
    /// </summary>
    private void ShowAddTargetEntityDialog(Comp.Choice choice)
    {
        // 获取所有可用的实体（排除当前实体）
        var allEntities = ECSFramework.Inst.GetAllEntities().ToList();

        if (allEntities.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No available entities to add", "OK");
            return;
        }

        GenericMenu menu = new GenericMenu();

        foreach (var entity in allEntities)
        {
            string menuText = GetEntityDisplayName(entity);
            menu.AddItem(
                new GUIContent(menuText),
                false,
                () =>
                {
                    if (!choice.TargetIds.Contains(entity.Id))
                    {
                        choice.TargetIds.Add(entity.Id);
                        GUI.changed = true;
                        Debug.Log($"Added target entity: {menuText}");
                    }
                }
            );
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// 添加所有兄弟节点作为目标
    /// </summary>
    private void AddAllSiblingsAsTargets(Comp.Choice choice)
    {
        if (selectedEntity == null)
            return;

        var siblings = Story
            .Inst.GetSiblingsOrdered(selectedEntity.Id)
            .Where(sibling => sibling.Id != selectedEntity.Id)
            .ToList();

        if (siblings.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No siblings found", "OK");
            return;
        }

        int addedCount = 0;
        foreach (var sibling in siblings)
        {
            if (!choice.TargetIds.Contains(sibling.Id))
            {
                choice.TargetIds.Add(sibling.Id);
                addedCount++;
            }
        }

        GUI.changed = true;
        Debug.Log($"Added {addedCount} siblings as targets");
    }

    private void DrawSplitters()
    {
        // 绘制分隔线
        EditorGUI.DrawRect(splitterLeftRect, Color.gray);
        EditorGUI.DrawRect(splitterMiddleRect, Color.gray);
    }

    private void HandleEvents()
    {
        // 处理分隔线拖拽
        Event currentEvent = Event.current;

        // 左侧分隔线
        if (
            currentEvent.type == EventType.MouseDown
            && splitterLeftRect.Contains(currentEvent.mousePosition)
        )
        {
            isResizingLeft = true;
            currentEvent.Use();
        }

        // 中间分隔线
        if (
            currentEvent.type == EventType.MouseDown
            && splitterMiddleRect.Contains(currentEvent.mousePosition)
        )
        {
            isResizingMiddle = true;
            currentEvent.Use();
        }

        // 鼠标释放
        if (currentEvent.type == EventType.MouseUp)
        {
            isResizingLeft = false;
            isResizingMiddle = false;
        }

        // 处理拖拽
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

        // 更新右侧面板宽度
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
        // 清理不存在的实体的展开状态
        var allEntityIds = ECSFramework.Inst.GetAllEntities().Select(e => e.Id).ToHashSet();
        var keysToRemove = expandedNodes.Keys.Where(id => !allEntityIds.Contains(id)).ToList();
        foreach (var key in keysToRemove)
        {
            expandedNodes.Remove(key);
        }

        ECSFramework.Inst.RebuildEntityReferences();
        Repaint();
    }

    // 添加一个立即刷新的方法
    private void RefreshTreeImmediate()
    {
        // 清理不存在的实体的展开状态
        var allEntityIds = ECSFramework.Inst.GetAllEntities().Select(e => e.Id).ToHashSet();
        var keysToRemove = expandedNodes.Keys.Where(id => !allEntityIds.Contains(id)).ToList();
        foreach (var key in keysToRemove)
        {
            expandedNodes.Remove(key);
        }

        ECSFramework.Inst.RebuildEntityReferences();

        // 强制立即重绘
        Repaint();

        // 如果需要，还可以强制重新布局
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
