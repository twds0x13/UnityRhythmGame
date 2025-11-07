using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JsonLoader;
using Newtonsoft.Json;
using UnityEngine;
using static ECS.Comp;

namespace ECS
{
    /// <summary>
    /// 故事树管理器 - 负责管理基于ECS架构的故事树结构
    /// 提供故事的加载、保存、验证等核心功能
    /// </summary>
    public partial class StoryTreeManager
    {
        #region 序列化工具

        /// <summary>
        /// JSON序列化工具类
        /// 使用BaseJsonLoader的统一设置确保序列化一致性
        /// </summary>
        private static class Serializer
        {
            /// <summary>
            /// 将对象序列化为JSON字符串
            /// </summary>
            /// <param name="obj">要序列化的对象</param>
            /// <returns>JSON格式的字符串</returns>
            public static string Serialize(object obj) =>
                JsonConvert.SerializeObject(obj, BaseJsonLoader.Settings);

            /// <summary>
            /// 将JSON字符串反序列化为指定类型的对象
            /// </summary>
            /// <typeparam name="T">目标对象类型</typeparam>
            /// <param name="json">JSON字符串</param>
            /// <returns>反序列化后的对象实例</returns>
            public static T Deserialize<T>(string json) =>
                JsonConvert.DeserializeObject<T>(json, BaseJsonLoader.Settings);

            /// <summary>
            /// 将JSON字符串反序列化为指定类型的对象
            /// </summary>
            /// <param name="json">JSON字符串</param>
            /// <param name="type">目标对象类型</param>
            /// <returns>反序列化后的对象实例</returns>
            public static object Deserialize(string json, Type type) =>
                JsonConvert.DeserializeObject(json, type, BaseJsonLoader.Settings);
        }

        #endregion

        #region 保存方法

        /// <summary>
        /// 异步保存故事树（不等待完成）
        /// 适用于不需要等待保存结果的场景
        /// </summary>
        /// <param name="fileName">目标文件名</param>
        /// <param name="output">是否输出操作日志</param>
        /// <example>
        /// <code>
        /// storyTreeManager.SaveAsync("story.json");
        /// </code>
        /// </example>
        public void SaveForget(string fileName, bool output = true)
        {
            Save(fileName, output).Forget();
        }

        /// <summary>
        /// 异步保存故事树到指定文件
        /// 会等待保存操作完成并返回结果
        /// </summary>
        /// <param name="fileName">目标文件名</param>
        /// <param name="output">是否输出操作日志</param>
        /// <param name="ct">取消令牌，用于取消保存操作</param>
        /// <returns>保存是否成功</returns>
        /// <example>
        /// <code>
        /// bool success = await storyTreeManager.Save("story.json");
        /// if (success) { /* 处理成功逻辑 */ }
        /// </code>
        /// </example>
        public async UniTask<bool> Save(
            string fileName,
            bool output = true,
            CancellationToken ct = default
        )
        {
            // 获取所有实体并转换为列表
            var entities = _ecsFramework.GetAllEntities().ToList();

            LogManager.Info($"开始保存故事树到: {fileName}", nameof(StoryTreeManager), output);

            var config = new LoadConfig() { CancellationToken = ct };

            // 使用StoryReader进行异步保存
            bool success = await StoryReader.SaveStoryAsync(fileName, entities, config);

            if (success)
            {
                LogManager.Info($"保存成功: {fileName}", nameof(StoryTreeManager), output);
            }
            else
            {
                LogManager.Error($"保存失败: {fileName}", nameof(StoryTreeManager), output);
            }

            return success;
        }

        /// <summary>
        /// 同步保存故事树到指定文件
        /// 适用于需要立即保存且不关心异步操作的场景
        /// </summary>
        /// <param name="fileName">目标文件名</param>
        /// <param name="output">是否输出操作日志</param>
        /// <remarks>
        /// 注意：同步保存会阻塞当前线程，在UI线程中使用可能导致卡顿
        /// </remarks>
        /// <example>
        /// <code>
        /// storyTreeManager.SaveSync("backup.json");
        /// </code>
        /// </example>
        public void SaveSync(string fileName, bool output = true)
        {
            var entities = _ecsFramework.GetAllEntities().ToList();

            LogManager.Info($"开始保存故事树到: {fileName}", nameof(StoryTreeManager), output);

            // 使用序列化工具将实体列表序列化为JSON并保存
            StoryReader.SaveStoryJson(fileName, Serializer.Serialize(entities));

            LogManager.Info($"保存完成: {fileName}", nameof(StoryTreeManager), output);
        }

        #endregion

        #region 加载方法

        /// <summary>
        /// 异步加载故事树（不等待完成）
        /// 适用于后台加载场景
        /// </summary>
        /// <param name="fileName">源文件名</param>
        /// <param name="output">是否输出操作日志</param>
        /// <param name="createNewIfMissing">当文件不存在时是否创建新的故事树</param>
        /// <example>
        /// <code>
        /// storyTreeManager.LoadAsync("story.json");
        /// </code>
        /// </example>
        public void LoadForget(string fileName, bool output = false, bool createNewIfMissing = true)
        {
            Load(fileName, output, createNewIfMissing).Forget();
        }

        /// <summary>
        /// 异步从文件加载故事树
        /// 包含完整的错误处理和文件不存在时的恢复逻辑
        /// </summary>
        /// <param name="fileName">源文件名</param>
        /// <param name="output">是否输出详细操作日志</param>
        /// <param name="createNewIfMissing">当文件不存在或损坏时是否创建新的故事树</param>
        /// <returns>加载是否成功</returns>
        /// <exception cref="FileNotFoundException">当文件不存在且createNewIfMissing为false时</exception>
        /// <example>
        /// <code>
        /// bool success = await storyTreeManager.Load("story.json");
        /// if (success) { /* 处理加载成功逻辑 */ }
        /// </code>
        /// </example>
        public async UniTask<bool> Load(
            string fileName,
            bool output = false,
            bool createNewIfMissing = true
        )
        {
            LogManager.Info($"开始加载故事树: {fileName}", nameof(StoryTreeManager), true);

            string filePath = Path.Join(Application.persistentDataPath, fileName);

            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                return await HandleMissingFile(fileName, output, createNewIfMissing);
            }

            try
            {
                // 异步加载实体列表
                var loadedEntities = await StoryReader.LoadEntitiesAsync(fileName);

                // 检查加载的实体是否有效
                if (loadedEntities == null || loadedEntities.Count == 0)
                {
                    return await HandleEmptyFile(fileName, output, createNewIfMissing);
                }

                // 处理加载的实体数据
                return await ProcessLoadedEntities(
                    fileName,
                    loadedEntities,
                    output,
                    createNewIfMissing
                );
            }
            catch (Exception ex)
            {
                LogManager.Exception(ex, nameof(StoryTreeManager), output);
                LogManager.Error($"加载失败: {fileName}", nameof(StoryTreeManager), output);

                return await HandleLoadError(fileName, output, createNewIfMissing);
            }
        }

        #endregion

        #region 核心操作

        /// <summary>
        /// 清空当前故事树的所有内容
        /// 包括所有实体和根节点引用
        /// </summary>
        /// <remarks>
        /// 清空后需要重新创建根节点才能继续使用故事树
        /// </remarks>
        /// <example>
        /// <code>
        /// storyTreeManager.Clear();
        /// storyTreeManager.CreateRoot("New Root", true);
        /// </code>
        /// </example>
        public void Clear()
        {
            _ecsFramework.ClearAllEntities();
            _rootEntity = null;
        }

        /// <summary>
        /// 验证故事树的结构完整性
        /// 检查根节点、章节结构、本地化键等关键元素
        /// </summary>
        /// <param name="output">是否输出验证详情</param>
        /// <returns>故事树结构是否有效</returns>
        /// <example>
        /// <code>
        /// if (storyTreeManager.Validate())
        /// {
        ///     Console.WriteLine("故事树结构有效");
        /// }
        /// </code>
        /// </example>
        public bool Validate(bool output = true)
        {
            LogManager.Info("开始验证故事树结构", nameof(StoryTreeManager), output);

            // 检查根节点是否存在
            if (!HasRoot())
            {
                LogManager.Error("缺少根节点", nameof(StoryTreeManager), output);
                return false;
            }

            var root = GetOrCreateRoot();

            // 验证根节点结构
            if (!ValidateRoot(root, output))
                return false;

            // 验证所有章节结构
            if (!ValidateChapters(output))
                return false;

            LogManager.Info("故事树验证通过", nameof(StoryTreeManager), output);
            return true;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 处理文件不存在的情况
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="output">是否输出日志</param>
        /// <param name="createNew">是否创建新故事树</param>
        /// <returns>处理结果</returns>
        private async UniTask<bool> HandleMissingFile(string fileName, bool output, bool createNew)
        {
            LogManager.Error($"文件不存在: {fileName}", nameof(StoryTreeManager), output);

            if (createNew)
            {
                LogManager.Info("创建新故事树", nameof(StoryTreeManager), output);
                return await CreateNewStoryTree(fileName, output);
            }

            return false;
        }

        /// <summary>
        /// 处理文件为空或损坏的情况
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="output">是否输出日志</param>
        /// <param name="createNew">是否创建新故事树</param>
        /// <returns>处理结果</returns>
        private async UniTask<bool> HandleEmptyFile(string fileName, bool output, bool createNew)
        {
            LogManager.Error($"文件为空或损坏: {fileName}", nameof(StoryTreeManager), output);

            if (createNew)
            {
                LogManager.Info("创建新故事树", nameof(StoryTreeManager), output);
                return await CreateNewStoryTree(fileName, output);
            }

            return false;
        }

        /// <summary>
        /// 处理加载过程中的异常情况
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="output">是否输出日志</param>
        /// <param name="createNew">是否创建新故事树</param>
        /// <returns>处理结果</returns>
        private async UniTask<bool> HandleLoadError(string fileName, bool output, bool createNew)
        {
            if (createNew)
            {
                LogManager.Info("创建新故事树", nameof(StoryTreeManager), output);
                return await CreateNewStoryTree(fileName, output);
            }

            return false;
        }

        /// <summary>
        /// 创建新的故事树结构
        /// 包含根节点创建和初始保存
        /// </summary>
        /// <param name="fileName">保存文件名</param>
        /// <param name="output">是否输出日志</param>
        /// <returns>创建是否成功</returns>
        private UniTask<bool> CreateNewStoryTree(string fileName, bool output)
        {
            try
            {
                // 清空现有结构
                Clear();

                // 创建新的根节点
                CreateRoot("Root", true);

                LogManager.Info("已创建新故事树", nameof(StoryTreeManager), output);

                // 保存新创建的故事树
                SaveSync(fileName, false);

                return UniTask.FromResult(true);
            }
            catch (Exception ex)
            {
                LogManager.Exception(ex, nameof(StoryTreeManager), output);
                LogManager.Error($"创建新故事树失败", nameof(StoryTreeManager), output);
                return UniTask.FromResult(false);
            }
        }

        /// <summary>
        /// 处理加载的实体数据
        /// 包括根节点识别、实体添加和框架重建
        /// </summary>
        /// <param name="fileName">源文件名</param>
        /// <param name="loadedEntities">加载的实体列表</param>
        /// <param name="output">是否输出日志</param>
        /// <param name="createNewIfMissing">当根节点不存在时是否创建新故事树</param>
        /// <returns>处理是否成功</returns>
        private async UniTask<bool> ProcessLoadedEntities(
            string fileName,
            System.Collections.Generic.List<Entity> loadedEntities,
            bool output,
            bool createNewIfMissing
        )
        {
            // 清空当前故事树
            Clear();

            // 查找根节点（拥有Root组件的实体）
            Entity rootEntity = loadedEntities.FirstOrDefault(e => e.HasComponent<Root>());

            LogManager.Info(
                $"找到 {loadedEntities.Count} 个节点, 根节点: {(rootEntity != null ? rootEntity.GetComponent<Root>().RootName : "无")}",
                nameof(StoryTreeManager),
                false
            );

            // 检查根节点是否存在
            if (rootEntity == null)
            {
                LogManager.Error("找不到根节点", nameof(StoryTreeManager), output);
                return createNewIfMissing ? await CreateNewStoryTree(fileName, output) : false;
            }

            // 将实体添加到ECS框架
            var (successCount, errorCount) = AddEntitiesToFramework(
                loadedEntities,
                rootEntity,
                output
            );

            // 重建实体引用和更新框架状态
            RebuildFramework();

            // 记录加载结果
            LogLoadResult(fileName, successCount, errorCount, output);

            return true;
        }

        /// <summary>
        /// 将实体列表添加到ECS框架
        /// 包含错误处理和成功计数
        /// </summary>
        /// <param name="entities">要添加的实体列表</param>
        /// <param name="rootEntity">根节点实体</param>
        /// <param name="output">是否输出日志</param>
        /// <returns>成功和失败的数量</returns>
        private (int success, int errors) AddEntitiesToFramework(
            System.Collections.Generic.List<Entity> entities,
            Entity rootEntity,
            bool output
        )
        {
            int successCount = 0;
            int errorCount = 0;

            // 先添加根节点（确保根节点最先添加）
            try
            {
                _ecsFramework.ForceAddEntity(rootEntity, true);
                _rootEntity = rootEntity;
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                LogManager.Exception(ex, nameof(StoryTreeManager), output);
                return (successCount, errorCount);
            }

            // 添加其他实体（跳过根节点，因为已经添加）
            foreach (var entity in entities.Where(e => e.Id != rootEntity.Id))
            {
                try
                {
                    _ecsFramework.AddEntity(entity, true);
                    successCount++;
                }
                catch (Exception ex)
                {
                    LogManager.Exception(ex, nameof(StoryTreeManager), output);
                    errorCount++;
                }
            }

            return (successCount, errorCount);
        }

        /// <summary>
        /// 重建ECS框架的引用关系
        /// 包括实体引用重建和根节点更新
        /// </summary>
        private void RebuildFramework()
        {
            _ecsFramework.RebuildEntityReferences();
            _rootEntity = _ecsFramework.FindRootEntity();
            UpdateIdManager();
        }

        /// <summary>
        /// 记录加载操作的结果
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="success">成功数量</param>
        /// <param name="errors">失败数量</param>
        /// <param name="output">是否输出日志</param>
        private void LogLoadResult(string fileName, int success, int errors, bool output)
        {
            string message = $"加载完成: {fileName}. 成功: {success}, 失败: {errors}";

            if (errors > 0)
            {
                LogManager.Warning(message, nameof(StoryTreeManager), output);
            }
            else
            {
                LogManager.Info(message, nameof(StoryTreeManager), output);
            }
        }

        /// <summary>
        /// 验证根节点的结构完整性
        /// </summary>
        /// <param name="root">根节点实体</param>
        /// <param name="output">是否输出日志</param>
        /// <returns>根节点是否有效</returns>
        private bool ValidateRoot(Entity root, bool output)
        {
            // 检查根节点是否有Root组件
            if (!root.HasComponent<Root>())
            {
                LogManager.Error("根节点缺少Root组件", nameof(StoryTreeManager), output);
                return false;
            }

            // 检查根节点是否错误地拥有父节点
            if (root.HasComponent<Parent>() && root.GetComponent<Parent>().ParentId.HasValue)
            {
                LogManager.Error("根节点不应有父节点", nameof(StoryTreeManager), output);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证所有章节的结构完整性
        /// 包括本地化组件和上下文键格式
        /// </summary>
        /// <param name="output">是否输出日志</param>
        /// <returns>所有章节是否有效</returns>
        private bool ValidateChapters(bool output)
        {
            var chapters = GetChapters();

            foreach (var chapter in chapters)
            {
                // 检查章节是否有本地化组件
                if (!chapter.HasComponent<Localization>())
                {
                    LogManager.Error(
                        $"章节 {chapter.Id} 缺少本地化组件",
                        nameof(StoryTreeManager),
                        output
                    );
                    return false;
                }

                var locComp = chapter.GetComponent<Localization>();

                // 检查本地化键格式（应以"C"开头）
                if (string.IsNullOrEmpty(locComp.ContextKey) || !locComp.ContextKey.StartsWith("C"))
                {
                    LogManager.Error(
                        $"章节 {chapter.Id} 本地化键格式错误",
                        nameof(StoryTreeManager),
                        output
                    );
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
