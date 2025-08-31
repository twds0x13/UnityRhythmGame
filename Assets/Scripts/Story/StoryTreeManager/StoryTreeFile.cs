using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.Plastic.Antlr3.Runtime.Debug;
using UnityEngine;
using static ECS.Comp;
using Json = JsonLoader.JsonManager;

namespace ECS
{
    public partial class StoryTreeManager
    {
        // 保存故事树到文件
        public void SaveStoryTree(string filePath)
        {
            var entities = _ecsFramework.GetAllEntities().ToList();

            var settings = new JsonSerializerSettings { Formatting = Formatting.None };
            Json.TrySaveJsonToZip(filePath, entities, settings);
        }

        // 从文件加载故事树
        public bool LoadStoryTree(string fileName, bool createNewIfMissing = true)
        {
            // 检查文件是否存在
            if (!File.Exists(Path.Join(Application.persistentDataPath, fileName)))
            {
                string errorMessage = $"故事树文件不存在: {fileName}";

                // 记录错误日志
                LogFile.Error(errorMessage, "StoryTreeManager");

                if (createNewIfMissing)
                {
                    LogFile.Info("尝试创建新的故事树", "StoryTreeManager");

                    // 清空当前管理器
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // 创建新的根节点
                    try
                    {
                        CreateRoot("新建故事树");
                        LogFile.Info("已创建新的故事树根节点", "StoryTreeManager");

                        // 可选：立即保存新创建的故事树
                        SaveStoryTree(fileName);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogFile.Exception(ex, "StoryTreeManager");
                        Debug.LogError($"创建新故事树失败: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError(errorMessage);
                    return false;
                }
            }

            try
            {
                // 尝试从文件加载实体
                List<Entity> entities;
                bool loadSuccess = Json.TryLoadJsonWithDebug(fileName, out entities);

                if (!loadSuccess || entities == null || entities.Count == 0)
                {
                    string errorMessage = $"故事树文件损坏或格式不正确: {fileName}";
                    LogFile.Error(errorMessage, "StoryTreeManager");

                    if (createNewIfMissing)
                    {
                        LogFile.Info("尝试创建新的故事树", "StoryTreeManager");

                        // 清空当前管理器
                        _ecsFramework.ClearAllEntities();
                        _rootEntity = null;

                        // 创建新的根节点
                        CreateRoot("新建故事树");
                        LogFile.Info("已创建新的故事树根节点", "StoryTreeManager");
                        return true;
                    }
                    else
                    {
                        Debug.LogError(errorMessage);
                        return false;
                    }
                }

                // 清空当前管理器
                _ecsFramework.ClearAllEntities();
                _rootEntity = null;

                // 添加所有实体
                int successCount = 0;
                int errorCount = 0;

                foreach (var entity in entities)
                {
                    try
                    {
                        _ecsFramework.AddEntity(entity);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogFile.Error(
                            $"添加实体失败 (ID: {entity?.Id}): {ex.Message}",
                            "StoryTreeManager"
                        );
                        errorCount++;
                    }
                }

                // 重建引用关系
                _ecsFramework.RebuildReferences();

                // 更新根节点引用
                _rootEntity = _ecsFramework.FindRootEntity();

                // 更新ID管理器
                UpdateIdManager();

                string logMessage =
                    $"故事树已从 {fileName} 加载. 成功: {successCount}, 失败: {errorCount}";

                if (errorCount > 0)
                {
                    LogFile.Warning(logMessage, "StoryTreeManager");
                }
                else
                {
                    LogFile.Info(logMessage, "StoryTreeManager");
                }

                Debug.Log(logMessage);
                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = $"加载故事树失败: {fileName}. 错误: {ex.Message}";
                LogFile.Exception(ex, "StoryTreeManager");
                Debug.LogError(errorMessage);

                if (createNewIfMissing)
                {
                    LogFile.Info("尝试创建新的故事树", "StoryTreeManager");

                    // 清空当前管理器
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // 创建新的根节点
                    CreateRoot("新建故事树");
                    LogFile.Info("已创建新的故事树根节点", "StoryTreeManager");
                    return true;
                }

                return false;
            }
        }

        // 验证故事树结构
        public bool ValidateStoryTree()
        {
            // 检查是否有根节点
            if (!HasRoot())
            {
                Debug.Log("错误: 没有根节点");
                return false;
            }

            var root = GetOrCreateRoot();

            // 检查根节点是否有 Root 组件
            if (!root.HasComponent<Root>())
            {
                Debug.Log("错误: 根节点没有 Root 组件");
                return false;
            }

            // 检查根节点是否有父节点（不应该有）
            if (root.HasComponent<Parent>())
            {
                var parentComp = root.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    Debug.Log("错误: 根节点有父节点");
                    return false;
                }
            }

            // 检查所有章节是否有正确的本地化键
            var chapters = GetChapters();
            foreach (var chapter in chapters)
            {
                if (!chapter.HasComponent<Localization>())
                {
                    Debug.Log($"错误: 章节 {chapter.Id} 没有 Localization 组件");
                    return false;
                }

                var locComp = chapter.GetComponent<Localization>();
                if (string.IsNullOrEmpty(locComp.ContextKey) || !locComp.ContextKey.StartsWith("C"))
                {
                    Debug.Log($"错误: 章节 {chapter.Id} 的本地化键不正确: {locComp.ContextKey}");
                    return false;
                }
            }

            // 可以添加更多验证逻辑...

            return true;
        }
    }
}
