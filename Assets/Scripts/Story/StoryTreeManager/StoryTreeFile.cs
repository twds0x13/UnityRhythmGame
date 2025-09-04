using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JsonLoader;
using Newtonsoft.Json;
using UnityEngine;
using static ECS.Comp;
using StoryJson = JsonLoader.StoryJsonManager;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public static class StorySerializerHelper
        {
            public static string Serialize(object obj)
            {
                return JsonConvert.SerializeObject(obj, BaseJsonLoader.Settings);
            }

            public static T Deserialize<T>(string json)
            {
                return JsonConvert.DeserializeObject<T>(json, BaseJsonLoader.Settings);
            }

            public static object Deserialize(string json, Type type)
            {
                return JsonConvert.DeserializeObject(json, type, BaseJsonLoader.Settings);
            }
        }

        #region Save To File


        public void SaveStoryTreeAsyncForget(string fileName, bool output = true)
        {
            // 启动异步保存任务
            SaveStoryTreeAsync(fileName, output).Forget();
        }

        // 在 StoryTreeManager 中使用异步保存
        public async UniTask<bool> SaveStoryTreeAsync(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            var entities = _ecsFramework.GetAllEntities().ToList();

            LogManager.Info(
                $"======Story.开始异步保存故事树到文件======",
                nameof(StoryTreeManager),
                output
            );

            // 使用异步保存方法
            bool saveSuccess = await StoryJson.TrySaveJsonToZipAsync(
                fileName,
                entities,
                BaseJsonLoader.Settings,
                output: output,
                cancellationToken: cancellationToken
            );

            if (saveSuccess)
            {
                LogManager.Info(
                    $"======Story.完成异步保存故事树到文件======\n",
                    nameof(StoryTreeManager),
                    output
                );
            }
            else
            {
                LogManager.Error(
                    $"======Story.保存故事树失败======\n",
                    nameof(StoryTreeManager),
                    output
                );
            }

            return saveSuccess;
        }

        // 保存故事树到文件
        public void SaveStoryTree(string fileName, bool output = true)
        {
            var entities = _ecsFramework.GetAllEntities().ToList();

            LogManager.Info(
                $"======Story.开始保存故事树到文件======",
                nameof(StoryTreeManager),
                output
            );

            StoryJson.TrySaveToZip(fileName, StorySerializerHelper.Serialize(entities));

            LogManager.Info(
                $"======Story.完成保存故事树到文件======\n",
                nameof(StoryTreeManager),
                output
            );
        }

        #endregion

        public void ClearStoryTree()
        {
            _ecsFramework.ClearAllEntities();
            _rootEntity = null;
        }

        #region Load From File

        public void LoadStoryTreeAsyncForget(
            string fileName,
            bool output = false,
            bool createNewIfMissing = true
        )
        {
            // 启动异步加载任务
            LoadStoryTreeAsync(fileName, output, createNewIfMissing).Forget();
        }

        /// <summary>
        /// 从文件加载故事树
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="output"></param>
        /// <param name="createNewIfMissing"></param>
        /// <returns></returns>
        public async UniTask<bool> LoadStoryTreeAsync(
            string fileName,
            bool output = false,
            bool createNewIfMissing = true
        )
        {
            LogManager.Info(
                $"======Story.开始异步加载故事树======\n",
                nameof(StoryTreeManager),
                true
            );

            // 检查文件是否存在
            string filePath = Path.Join(Application.persistentDataPath, fileName);
            if (!File.Exists(filePath))
            {
                string errorMessage = $"故事树文件不存在: {fileName}";

                // 记录错误日志
                LogManager.Error(errorMessage, nameof(StoryTreeManager), output);

                if (createNewIfMissing)
                {
                    LogManager.Info("尝试创建新的故事树", nameof(StoryTreeManager), output);

                    // 清空当前管理器
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // 创建新的根节点
                    try
                    {
                        CreateRoot("Root", true);

                        LogManager.Info("已创建新的故事树根节点", nameof(StoryTreeManager), output);

                        // 可选：立即保存新创建的故事树
                        SaveStoryTree(fileName, false);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Exception(ex, nameof(StoryTreeManager), output);
                        LogManager.Error($"创建新故事树失败: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    LogManager.Error(errorMessage);
                    return false;
                }
            }

            try
            {
                var (loadSuccess, loadedEntities) = await StoryJson.TryLoadEntitiyListAsync<Entity>(
                    fileName,
                    output
                );

                if (!loadSuccess || loadedEntities == null || loadedEntities.Count == 0)
                {
                    string errorMessage = $"故事树文件损坏或格式不正确: {fileName}";
                    LogManager.Error(errorMessage, nameof(StoryTreeManager), output);

                    if (createNewIfMissing)
                    {
                        LogManager.Info("尝试创建新的故事树", nameof(StoryTreeManager), output);

                        // 清空当前管理器
                        _ecsFramework.ClearAllEntities();
                        _rootEntity = null;

                        // 创建新的根节点
                        CreateRoot("Root", true);

                        LogManager.Info("已创建新的故事树根节点", nameof(StoryTreeManager), output);

                        SaveStoryTree(fileName, false);

                        return true;
                    }
                    else
                    {
                        LogManager.Error(errorMessage);
                        return false;
                    }
                }

                // 清空当前管理器
                _ecsFramework.ClearAllEntities();
                _rootEntity = null;

                // 首先找到根节点实体
                Entity rootEntity = loadedEntities.FirstOrDefault(e => e.HasComponent<Root>());

                LogManager.Info(
                    $"共找到 {loadedEntities.Count} 个故事节点实体, 根节点实体: {(rootEntity != null ? " Name : " + rootEntity.GetComponent<Root>().RootName : "未找到")}",
                    nameof(StoryTreeManager),
                    false
                );

                if (rootEntity == null)
                {
                    LogManager.Error("找不到根节点实体", nameof(StoryTreeManager), output);
                    if (createNewIfMissing)
                    {
                        // 创建新的根节点
                        CreateRoot("Root", true);
                        return true;
                    }
                    return false;
                }

                // 添加实体
                int successCount = 0;
                int errorCount = 0;

                // 首先添加根节点
                try
                {
                    _ecsFramework.ForceAddEntity(rootEntity, true);
                    _rootEntity = rootEntity;
                    successCount++;
                    LogManager.Info(
                        $"根节点状态正常 (ID: {rootEntity.Id})\n",
                        nameof(StoryTreeManager),
                        output
                    );

                    LogManager.Info(
                        $"根节点是否拥有 IdManager : {rootEntity.HasComponent<IdManager>()}",
                        nameof(StoryTreeManager),
                        false
                    );
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LogManager.Exception(ex, nameof(StoryTreeManager), output);
                    LogManager.Error(
                        $"添加根节点实体失败: {ex.Message}",
                        nameof(StoryTreeManager),
                        output
                    );

                    // 如果根节点添加失败，尝试创建新的故事树
                    if (createNewIfMissing)
                    {
                        LogManager.Info("尝试创建新的故事树", nameof(StoryTreeManager), output);
                        CreateRoot("Root", true);
                        return true;
                    }
                    return false;
                }

                // 然后添加其他实体
                foreach (var entity in loadedEntities)
                {
                    if (entity.Id == rootEntity.Id)
                        continue;

                    try
                    {
                        _ecsFramework.AddEntity(entity, true); // 强制添加
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error(
                            $"添加实体失败 (ID: {entity?.Id}): {ex.Message}",
                            nameof(StoryTreeManager),
                            output
                        );
                        errorCount++;
                    }
                }

                // 重建引用关系
                _ecsFramework.RebuildEntityReferences();

                // 更新根节点引用
                _rootEntity = _ecsFramework.FindRootEntity();

                // 更新ID管理器
                UpdateIdManager();

                string logMessage =
                    $"故事树已从 {fileName} 加载. 成功: {successCount}, 失败: {errorCount}";

                if (errorCount > 0)
                {
                    LogManager.Warning(logMessage, nameof(StoryTreeManager), output);
                }
                else
                {
                    LogManager.Info(logMessage, nameof(StoryTreeManager), output);
                }

                LogManager.Info(
                    $"======Story.完成异步加载故事树======\n",
                    nameof(StoryTreeManager)
                );

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = $"加载故事树失败: {fileName}. 错误: {ex.Message}";
                LogManager.Exception(ex, nameof(StoryTreeManager), output);
                LogManager.Error(errorMessage);

                if (createNewIfMissing)
                {
                    LogManager.Info("尝试创建新的故事树", nameof(StoryTreeManager), output);

                    // 清空当前管理器
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // 创建新的根节点
                    CreateRoot("Root", true);

                    LogManager.Info("已创建新的故事树根节点", nameof(StoryTreeManager), output);

                    LogManager.Info(
                        $"======Story.完成异步加载故事树======\n",
                        nameof(StoryTreeManager)
                    );

                    return true;
                }

                return false;
            }
        }

        #endregion

        // 验证故事树结构
        public bool ValidateStoryTree(bool output = true)
        {
            LogManager.Info("======Story.开始验证树结构======", nameof(StoryTreeManager), output);

            // 检查是否有根节点
            if (!HasRoot())
            {
                LogManager.Log("错误: 没有根节点");
                return false;
            }

            var root = GetOrCreateRoot();

            // 检查根节点是否有 Root 组件
            if (!root.HasComponent<Root>())
            {
                LogManager.Log("错误: 根节点没有 Root 组件", nameof(StoryTreeManager));
                return false;
            }

            // 检查根节点是否有父节点（不应该有）
            if (root.HasComponent<Parent>())
            {
                var parentComp = root.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    LogManager.Log("错误: 根节点有父节点", nameof(StoryTreeManager));
                    return false;
                }
            }

            // 检查所有章节是否有正确的本地化键
            var chapters = GetChapters();
            foreach (var chapter in chapters)
            {
                if (!chapter.HasComponent<Localization>())
                {
                    LogManager.Log($"错误: 章节 {chapter.Id} 没有 Localization 组件");
                    return false;
                }

                var locComp = chapter.GetComponent<Localization>();
                if (string.IsNullOrEmpty(locComp.ContextKey) || !locComp.ContextKey.StartsWith("C"))
                {
                    LogManager.Log(
                        $"错误: 章节 {chapter.Id} 的本地化键不正确: {locComp.ContextKey}"
                    );
                    return false;
                }
            }

            // 可以添加更多验证逻辑...

            LogManager.Info("======Story.完成验证树结构======\n", nameof(StoryTreeManager), output);

            return true;
        }
    }
}
