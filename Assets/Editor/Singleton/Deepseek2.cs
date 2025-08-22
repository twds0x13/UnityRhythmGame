using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DraggableListExample : EditorWindow
{
    [MenuItem("Window/Draggable List Example")]
    static void ShowWindow()
    {
        GetWindow<DraggableListExample>("Draggable List");
    }

    private List<GameObject> itemList = new List<GameObject>();
    private Vector2 scrollPosition;
    private int dragFromIndex = -1;
    private int dragToIndex = -1;
    private bool isDragging = false;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("可拖拽列表", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "拖动手柄(≡)可改变顺序\n从Hierarchy拖入对象可添加为子节点",
            MessageType.Info
        );

        // 绘制外部拖拽区域
        DrawExternalDropArea();

        // 绘制列表
        DrawDraggableList();

        // 绘制按钮区域
        DrawButtonArea();
    }

    private void DrawExternalDropArea()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖入对象添加到此列表", EditorStyles.helpBox);

        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject go && !itemList.Contains(go))
                        {
                            itemList.Add(go);
                        }
                    }
                }
                evt.Use();
                break;
        }
    }

    private void DrawDraggableList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.ExpandHeight(true)
        );

        for (int i = 0; i < itemList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            // 拖拽手柄
            Rect handleRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);
            GUI.Label(handleRect, "≡", EditorStyles.miniButton);

            // 处理拖拽事件
            HandleDragEvents(handleRect, i);

            // 列表项内容
            EditorGUILayout.ObjectField(itemList[i], typeof(GameObject), false);

            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                itemList.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();

            // 绘制拖拽指示线
            if (isDragging && dragToIndex == i)
            {
                Rect lineRect = GUILayoutUtility.GetLastRect();
                lineRect.height = 2;
                lineRect.y -= 1;
                EditorGUI.DrawRect(lineRect, Color.red);
            }
        }

        // 绘制列表底部指示线
        if (isDragging && dragToIndex == itemList.Count)
        {
            Rect lineRect = GUILayoutUtility.GetRect(10, 2);
            EditorGUI.DrawRect(lineRect, Color.red);
        }

        EditorGUILayout.EndScrollView();
    }

    private void HandleDragEvents(Rect handleRect, int index)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.MouseDown:
                if (handleRect.Contains(evt.mousePosition))
                {
                    dragFromIndex = index;
                    isDragging = true;
                    evt.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isDragging && dragFromIndex >= 0)
                {
                    // 计算拖拽目标位置
                    dragToIndex = -1;
                    for (int i = 0; i < itemList.Count; i++)
                    {
                        Rect itemRect = GUILayoutUtility.GetLastRect();
                        if (itemRect.Contains(evt.mousePosition))
                        {
                            dragToIndex = i;
                            break;
                        }
                    }

                    // 如果拖到列表底部
                    if (
                        dragToIndex == -1
                        && GUILayoutUtility.GetLastRect().yMax < evt.mousePosition.y
                    )
                    {
                        dragToIndex = itemList.Count;
                    }

                    evt.Use();
                }
                break;

            case EventType.MouseUp:
                if (isDragging && dragFromIndex >= 0)
                {
                    // 执行重新排序
                    if (dragToIndex >= 0 && dragToIndex != dragFromIndex)
                    {
                        GameObject item = itemList[dragFromIndex];
                        itemList.RemoveAt(dragFromIndex);

                        if (dragToIndex > dragFromIndex)
                            dragToIndex--;

                        itemList.Insert(dragToIndex, item);
                    }

                    isDragging = false;
                    dragFromIndex = -1;
                    dragToIndex = -1;
                    evt.Use();
                }
                break;
        }
    }

    private void DrawButtonArea()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加空项"))
        {
            itemList.Add(null);
        }
        if (GUILayout.Button("清除列表"))
        {
            itemList.Clear();
        }
        EditorGUILayout.EndHorizontal();
    }
}
