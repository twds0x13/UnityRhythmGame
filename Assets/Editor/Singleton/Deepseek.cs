using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TreeNodeEditor : EditorWindow
{
    [Serializable]
    public class TreeNode
    {
        public string name;
        public int id;
        public int level;
        public List<TreeNode> children = new List<TreeNode>();
        public bool expanded = true;
        public Color nodeColor = Color.white;

        public TreeNode(string name, int id, int level)
        {
            this.name = name;
            this.id = id;
            this.level = level;

            if (level == 0)
                nodeColor = new Color(0.2f, 0.6f, 1f);
            else if (level == 1)
                nodeColor = new Color(0.2f, 0.8f, 0.4f);
            else if (level == 2)
                nodeColor = new Color(1f, 0.8f, 0.2f);
            else
                nodeColor = Color.Lerp(Color.white, Color.magenta, level * 0.1f);
        }
    }

    private List<TreeNode> nodes = new List<TreeNode>();
    private int nextId = 1;
    private TreeNode selectedNode;
    private Vector2 scrollPosition;
    private string newNodeName = "New Node";
    private int newLevel = 0;
    private TreeNode nodeToDelete = null;
    private TreeNode draggedNode = null;
    private TreeNode dragTargetNode = null;
    private bool isDragging = false;
    private Vector2 dragStartPos;

    [MenuItem("Tools/Drag&Drop Node Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<TreeNodeEditor>("Drag&Drop Node Editor");
        window.minSize = new Vector2(800, 600);
        window.position = new Rect(100, 100, 800, 600);
    }

    private void OnEnable()
    {
        if (nodes.Count == 0)
        {
            var root1 = new TreeNode("总部 (等级0)", nextId++, 0);
            root1.children.Add(new TreeNode("技术部 (等级1)", nextId++, 1));
            root1.children.Add(new TreeNode("市场部 (等级1)", nextId++, 1));

            var techDept = root1.children[0];
            techDept.children.Add(new TreeNode("开发组 (等级2)", nextId++, 2));
            techDept.children.Add(new TreeNode("设计组 (等级2)", nextId++, 2));

            nodes.Add(root1);

            var root2 = new TreeNode("分公司 (等级0)", nextId++, 0);
            nodes.Add(root2);
        }
    }

    private void OnGUI()
    {
        HandleDragAndDrop();

        if (nodeToDelete != null)
        {
            DeleteNode(nodeToDelete);
            nodeToDelete = null;
        }

        EditorGUILayout.BeginHorizontal();
        DrawTreePanel();
        DrawDetailsPanel();
        EditorGUILayout.EndHorizontal();

        DrawDragIndicator();
    }

    private void HandleDragAndDrop()
    {
        Event currentEvent = Event.current;

        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                if (currentEvent.button == 0) // 左键点击
                {
                    dragStartPos = currentEvent.mousePosition;
                }
                break;

            case EventType.MouseDrag:
                if (!isDragging && Vector2.Distance(dragStartPos, currentEvent.mousePosition) > 10f)
                {
                    if (selectedNode != null)
                    {
                        isDragging = true;
                        draggedNode = selectedNode;
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                        currentEvent.Use();
                    }
                }
                break;

            case EventType.MouseUp:
                if (isDragging)
                {
                    if (
                        dragTargetNode != null
                        && draggedNode != null
                        && dragTargetNode != draggedNode
                        && !IsChildOf(draggedNode, dragTargetNode)
                    )
                    {
                        // 从原位置移除节点
                        RemoveNodeFromParent(draggedNode);

                        // 添加到新父节点
                        dragTargetNode.children.Add(draggedNode);
                        draggedNode.level = dragTargetNode.level + 1;
                        UpdateChildrenLevel(draggedNode);

                        // 展开父节点
                        dragTargetNode.expanded = true;
                    }

                    isDragging = false;
                    draggedNode = null;
                    dragTargetNode = null;
                    GUIUtility.hotControl = 0;
                    currentEvent.Use();
                }
                break;
        }
    }

    private void DrawDragIndicator()
    {
        if (isDragging && draggedNode != null)
        {
            Rect dragRect = new Rect(
                Event.current.mousePosition.x - 100,
                Event.current.mousePosition.y - 20,
                200,
                40
            );

            EditorGUI.DrawRect(dragRect, new Color(0.2f, 0.4f, 0.8f, 0.5f));
            EditorGUI.LabelField(
                dragRect,
                $"正在拖拽: {draggedNode.name}",
                EditorStyles.whiteBoldLabel
            );

            // 绘制拖拽目标指示器
            if (dragTargetNode != null)
            {
                Rect targetRect = new Rect(
                    Event.current.mousePosition.x - 100,
                    Event.current.mousePosition.y + 30,
                    200,
                    20
                );
                EditorGUI.DrawRect(targetRect, new Color(0.2f, 0.8f, 0.2f, 0.5f));
                EditorGUI.LabelField(
                    targetRect,
                    $"连接到: {dragTargetNode.name}",
                    EditorStyles.whiteBoldLabel
                );
            }
        }
    }

    private bool IsChildOf(TreeNode parent, TreeNode child)
    {
        if (parent == child)
            return true;
        foreach (var node in parent.children)
        {
            if (IsChildOf(node, child))
                return true;
        }
        return false;
    }

    private void RemoveNodeFromParent(TreeNode node)
    {
        // 从根节点查找
        if (nodes.Contains(node))
        {
            nodes.Remove(node);
            return;
        }

        // 递归查找并移除
        foreach (var root in nodes)
        {
            if (RemoveChild(root, node))
            {
                return;
            }
        }
    }

    private bool RemoveChild(TreeNode parent, TreeNode target)
    {
        if (parent.children.Contains(target))
        {
            parent.children.Remove(target);
            return true;
        }

        foreach (var child in parent.children)
        {
            if (RemoveChild(child, target))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateChildrenLevel(TreeNode parent)
    {
        foreach (var child in parent.children)
        {
            child.level = parent.level + 1;
            UpdateChildrenLevel(child);
        }
    }

    private void DrawTreePanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(350));
        EditorGUILayout.LabelField("节点层级树 (拖拽连接)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        newNodeName = EditorGUILayout.TextField(newNodeName);
        newLevel = EditorGUILayout.IntSlider("等级", newLevel, 0, 5);
        if (GUILayout.Button("添加根节点", GUILayout.Width(100)))
        {
            nodes.Add(new TreeNode(newNodeName, nextId++, newLevel));
            newNodeName = "New Node";
        }
        EditorGUILayout.EndHorizontal();

        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.ExpandHeight(true)
        );

        List<TreeNode> nodesToDraw = new List<TreeNode>(nodes);
        foreach (var node in nodesToDraw)
        {
            DrawNode(node);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawNode(TreeNode node)
    {
        Rect nodeRect = EditorGUILayout.BeginVertical(GUI.skin.box);

        // 处理拖拽目标
        if (isDragging && draggedNode != null && draggedNode != node)
        {
            if (nodeRect.Contains(Event.current.mousePosition))
            {
                dragTargetNode = node;
                EditorGUI.DrawRect(nodeRect, new Color(0.5f, 1f, 0.5f, 0.3f));
            }
        }

        // 节点行
        EditorGUILayout.BeginHorizontal();

        // 拖拽手柄
        EditorGUILayout.LabelField("≡", GUILayout.Width(20));

        // 等级标签
        GUILayout.Label($"Lv{node.level}", GetLevelLabelStyle(node.level), GUILayout.Width(40));

        // 折叠/展开箭头
        if (node.children.Count > 0)
        {
            node.expanded = EditorGUILayout.Foldout(node.expanded, GUIContent.none);
        }
        else
        {
            GUILayout.Space(14);
        }

        // 节点选择按钮
        var bgColor = GUI.backgroundColor;
        GUI.backgroundColor = node.nodeColor;

        if (GUILayout.Button(node.name, EditorStyles.miniButton))
        {
            selectedNode = node;
        }

        GUI.backgroundColor = bgColor;

        // 删除按钮
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            nodeToDelete = node;
        }

        EditorGUILayout.EndHorizontal();

        // 添加子节点按钮
        if (node.expanded && node.children.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(60);
            if (GUILayout.Button("添加子节点", EditorStyles.miniButton))
            {
                node.children.Add(
                    new TreeNode(
                        $"子节点{node.children.Count + 1} (等级{node.level + 1})",
                        nextId++,
                        node.level + 1
                    )
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        // 绘制子节点
        if (node.expanded && node.children.Count > 0)
        {
            EditorGUI.indentLevel++;
            List<TreeNode> childrenToDraw = new List<TreeNode>(node.children);
            foreach (var child in childrenToDraw)
            {
                DrawNode(child);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDetailsPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        if (selectedNode != null)
        {
            EditorGUILayout.LabelField("节点详情", EditorStyles.largeLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("节点等级:", GUILayout.Width(80));
            EditorGUILayout.LabelField(
                $"Lv{selectedNode.level}",
                GetLevelLabelStyle(selectedNode.level)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"节点ID: {selectedNode.id}");

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("节点名称:", EditorStyles.boldLabel);
            selectedNode.name = EditorGUILayout.TextField(selectedNode.name);

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField($"子节点数量: {selectedNode.children.Count}");

            EditorGUILayout.Space(10);
            if (GUILayout.Button("添加子节点", GUILayout.Height(30)))
            {
                selectedNode.children.Add(
                    new TreeNode(
                        $"子节点{selectedNode.children.Count + 1} (等级{selectedNode.level + 1})",
                        nextId++,
                        selectedNode.level + 1
                    )
                );
            }

            EditorGUILayout.Space(20);
            if (GUILayout.Button("删除节点", GUIStyles.dangerButton))
            {
                nodeToDelete = selectedNode;
            }
        }
        else
        {
            EditorGUILayout.LabelField("", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("请从左侧选择节点", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndVertical();
    }

    private GUIStyle GetLevelLabelStyle(int level)
    {
        var style = new GUIStyle(EditorStyles.boldLabel);

        if (level == 0)
            style.normal.textColor = new Color(0.1f, 0.4f, 0.8f);
        else if (level == 1)
            style.normal.textColor = new Color(0.1f, 0.6f, 0.2f);
        else if (level == 2)
            style.normal.textColor = new Color(0.8f, 0.6f, 0.1f);
        else
            style.normal.textColor = Color.Lerp(Color.gray, Color.magenta, level * 0.15f);

        style.alignment = TextAnchor.MiddleCenter;
        return style;
    }

    private void DeleteNode(TreeNode node)
    {
        if (nodes.Contains(node))
        {
            nodes.Remove(node);
            if (selectedNode == node)
                selectedNode = null;
            return;
        }

        foreach (var root in nodes)
        {
            if (DeleteChild(root, node))
            {
                if (selectedNode == node)
                    selectedNode = null;
                return;
            }
        }
    }

    private bool DeleteChild(TreeNode parent, TreeNode target)
    {
        if (parent.children.Contains(target))
        {
            parent.children.Remove(target);
            return true;
        }

        foreach (var child in parent.children)
        {
            if (DeleteChild(child, target))
            {
                return true;
            }
        }

        return false;
    }
}

public static class GUIStyles
{
    private static GUIStyle _dangerButton;

    public static GUIStyle dangerButton
    {
        get
        {
            if (_dangerButton == null)
            {
                _dangerButton = new GUIStyle(GUI.skin.button);
                _dangerButton.normal.textColor = Color.white;
                _dangerButton.normal.background = MakeTex(2, 2, new Color(0.8f, 0.2f, 0.2f));
                _dangerButton.fontStyle = FontStyle.Bold;
                _dangerButton.padding = new RectOffset(10, 10, 8, 8);
                _dangerButton.fontSize = 14;
            }
            return _dangerButton;
        }
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
