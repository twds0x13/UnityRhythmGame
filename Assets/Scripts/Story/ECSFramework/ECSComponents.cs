using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace ECS
{
    public interface IComponent { }

    public class Comp
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class Root : IComponent
        {
            [JsonProperty]
            public string RootName { get; set; }

            // 用来处理反序列化，其余同理
            [JsonConstructor]
            public Root() { }
        }

        /// <summary>
        /// 处理同级节点之间的顺序。序号应当唯一，需要自动维护。
        /// </summary>
        [JsonObject(MemberSerialization.OptIn)]
        public class Order : IComponent
        {
            [JsonProperty]
            public int Number { get; set; } = 0;

            [JsonProperty]
            public string Label { get; set; } = null;

            /// <summary>
            /// 获取格式化的序号标签
            /// </summary>
            [JsonIgnore]
            public string FormattedLabel
            {
                get
                {
                    if (!string.IsNullOrEmpty(Label))
                        return Label;

                    return Number.ToString();
                }
            }

            public Order() { }

            public Order(int number, string label = null)
            {
                Number = number;
                Label = label;
            }

            /// <summary>
            /// 创建章节序号
            /// </summary>
            public static Order CreateChapterOrder(int number)
            {
                return new Order(number, $"第{number}章");
            }

            /// <summary>
            /// 创建情节序号
            /// </summary>
            public static Order CreateEpisodeOrder(int number)
            {
                return new Order(number, $"第{number}节");
            }

            /// <summary>
            /// 创建对白序号
            /// </summary>
            public static Order CreateLineOrder(int number)
            {
                return new Order(number, $"{number}.");
            }
        }

        /// <summary>
        /// 剧情选择组件，允许从当前节点跳转到多个目标节点之一
        /// </summary>
        [JsonObject(MemberSerialization.OptIn)]
        public class Choice : IComponent
        {
            [JsonProperty]
            public List<ChoiceOption> Options { get; set; } = new();

            [JsonProperty]
            public SelectionType Type { get; set; } = SelectionType.Custom;

            /// <summary>
            /// 获取所有目标ID的列表（兼容旧代码）
            /// </summary>
            [JsonIgnore]
            public List<int> TargetIds => Options.Select(o => o.TargetId).ToList();

            /// <summary>
            /// 添加选择选项
            /// </summary>
            public void AddOption(
                int targetId,
                string displayText = null,
                string localizationKey = null
            )
            {
                var option = new ChoiceOption
                {
                    TargetId = targetId,
                    DisplayText = displayText,
                    LocalizationKey = localizationKey,
                };

                Options.Add(option);
            }

            /// <summary>
            /// 移除选择选项
            /// </summary>
            public void RemoveOption(int targetId) =>
                Options.RemoveAll(o => o.TargetId == targetId);

            /// <summary>
            /// 检查是否可以跳转到指定目标
            /// </summary>
            public bool CanJumpTo(int targetId) => Options.Any(o => o.TargetId == targetId);

            /// <summary>
            /// 获取选项的显示文本（优先使用本地化键）
            /// </summary>
            public string GetOptionDisplayText(int targetId, ECSFramework ecsManager)
            {
                var option = Options.FirstOrDefault(o => o.TargetId == targetId);
                if (option == null)
                    return null;

                // 优先使用本地化键
                if (!string.IsNullOrEmpty(option.LocalizationKey))
                {
                    // 这里可以调用本地化系统获取翻译
                    return option.LocalizationKey;
                }

                // 其次使用自定义显示文本
                if (!string.IsNullOrEmpty(option.DisplayText))
                {
                    return option.DisplayText;
                }

                // 最后使用目标实体的默认文本
                var targetEntity = ecsManager.GetEntitySafe(targetId);
                if (targetEntity != null && targetEntity.HasComponent<Localization>())
                {
                    return targetEntity.GetComponent<Localization>().DefaultText;
                }

                return $"Option {targetId}";
            }

            /// <summary>
            /// 自动为所有选项生成本地化键
            /// </summary>
            public void GenerateLocalizationKeysForOptions(
                Entity currentEntity,
                ECSFramework ecsManager
            )
            {
                if (currentEntity == null || !currentEntity.HasComponent<Localization>())
                    return;

                var currentLoc = currentEntity.GetComponent<Localization>();

                for (int i = 0; i < Options.Count; i++)
                {
                    var option = Options[i];
                    var targetEntity = ecsManager.GetEntitySafe(option.TargetId);

                    if (targetEntity != null && targetEntity.HasComponent<Localization>())
                    {
                        var targetLoc = targetEntity.GetComponent<Localization>();

                        // 生成格式为 Cx_Ex_Lx_Cy 的本地化键
                        option.LocalizationKey = $"{currentLoc.ContextKey}_C{i + 1}";
                    }
                }
            }

            [JsonConstructor]
            private Choice() { }

            public Choice(SelectionType type = SelectionType.Custom)
            {
                Type = type;
            }

            public static Choice CreateChoice(List<int> targetIds)
            {
                var choice = new Choice(SelectionType.Custom);
                foreach (var targetId in targetIds)
                {
                    choice.AddOption(targetId);
                }
                return choice;
            }
        }

        /// <summary>
        /// 选择选项
        /// </summary>
        [JsonObject(MemberSerialization.OptIn)]
        public class ChoiceOption
        {
            [JsonProperty]
            public int TargetId { get; set; }

            [JsonProperty]
            public string DisplayText { get; set; }

            [JsonProperty]
            public string LocalizationKey { get; set; }

            [JsonIgnore]
            public Entity TargetEntity { get; set; }
        }

        /// <summary>
        /// 跳转类型枚举
        /// </summary>
        public enum SelectionType
        {
            Null,
            Custom, // 剧情分支
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class IdManager : IComponent
        {
            [JsonProperty]
            private HashSet<int> _usedIds { get; set; } = new();

            [JsonProperty]
            public int NextAvailableId { get; set; } = 0;

            [JsonIgnore]
            public int EntityCount => _usedIds.Count;

            // 获取下一个可用的 ID
            public int GetNextId()
            {
                int id = NextAvailableId;

                while (_usedIds.Contains(id))
                {
                    id++;
                }

                _usedIds.Add(id);
                NextAvailableId = id + 1;
                return id;
            }

            // 注册已使用的 ID（主要用于反序列化）
            public void RegisterId(int id)
            {
                if (_usedIds.Contains(id))
                {
                    throw new ArgumentException($"ID {id} 已被使用");
                }

                _usedIds.Add(id);

                // 更新下一个可用ID
                if (id >= NextAvailableId)
                {
                    NextAvailableId = id + 1;
                }
            }

            // 注销不再使用的 ID
            public void UnregisterId(int id)
            {
                _usedIds.Remove(id);

                // 如果注销的ID小于下一个可用ID，可以考虑更新NextAvailableId
                // 但这会增加复杂性，且不是必需的
            }

            // 检查ID是否已被使用
            public bool IdRegistered(int id)
            {
                return _usedIds.Contains(id);
            }

            /// <summary>
            /// 不要轻易调用！清空所有的 ID 且从 0 开始计数
            /// </summary>
            /// <param name="startId"></param>
            public void Reset(int startId = 0)
            {
                _usedIds.Clear();
                NextAvailableId = startId;
            }

            // 获取所有已使用的ID（用于调试或特殊用途）
            public IEnumerable<int> GetAllUsedIds()
            {
                return _usedIds.ToList();
            }

            public IdManager(bool isRoot = false)
            {
                _usedIds.Clear();

                if (isRoot)
                {
                    // 如果是根节点，强制使用 ID 0
                    NextAvailableId = 1; // 下一个可用 ID 从 1 开始
                    RegisterId(0); // 注册 ID 0
                }
                else
                {
                    NextAvailableId = 0;
                }
            }

            [JsonConstructor]
            private IdManager()
            {
                // 用于反序列化的私有构造函数
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Localization : IComponent, IDisposable
        {
            [JsonIgnore]
            private CancellationTokenSource _cancellationTokenSource;

            public enum NodeType
            {
                Null,
                Chapter,
                Episode,
                Line,
            }

            [JsonProperty]
            public NodeType Type { get; set; }

            [JsonProperty]
            public string DefaultText { get; set; } = "";

            [JsonProperty]
            public int Number { get; set; } = 0;

            [JsonProperty]
            public string SpeakerKey { get; set; } = "";

            [JsonProperty]
            public string ContextKey { get; private set; } = "";

            public async UniTask GenerateLocalizationKey(
                Entity entity,
                ECSFramework ecsManager,
                CancellationToken cancellationToken = default
            )
            {
                if (entity == null || ecsManager == null)
                    return;

                string previousContextKey = ContextKey;

                // 取消之前的操作（如果有）
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

                switch (Type)
                {
                    case NodeType.Chapter:
                        ContextKey = $"C{Number}";
                        break;

                    case NodeType.Episode:
                        if (entity.HasComponent<Parent>())
                        {
                            var parentComp = entity.GetComponent<Parent>();
                            if (parentComp.ParentId.HasValue)
                            {
                                var parent = ecsManager.GetEntitySafe(parentComp.ParentId.Value);
                                if (parent != null && parent.HasComponent<Localization>())
                                {
                                    var parentLoc = parent.GetComponent<Localization>();
                                    ContextKey = $"{parentLoc.ContextKey}_E{Number}";
                                }
                            }
                        }
                        break;

                    case NodeType.Line:
                        if (entity.HasComponent<Parent>())
                        {
                            var parentComp = entity.GetComponent<Parent>();
                            if (parentComp.ParentId.HasValue)
                            {
                                var parent = ecsManager.GetEntitySafe(parentComp.ParentId.Value);
                                if (parent != null && parent.HasComponent<Localization>())
                                {
                                    var parentLoc = parent.GetComponent<Localization>();
                                    ContextKey = $"{parentLoc.ContextKey}_L{Number}";
                                }
                            }
                        }
                        break;
                }

#if UNITY_EDITOR
                // 自动创建 Localization Table Entry
                if (!string.IsNullOrEmpty(ContextKey) && ContextKey != previousContextKey)
                {
                    await CreateLocalizationEntryUniTaskAsync(
                        ContextKey,
                        _cancellationTokenSource.Token
                    );
                }
#endif
            }

            /// <summary>
            /// 使用 UniTask 在 GameStory 表中自动创建本地化条目
            /// </summary>
            private async UniTask<bool> CreateLocalizationEntryUniTaskAsync(
                string key,
                CancellationToken cancellationToken = default
            )
            {
                try
                {
                    // 等待 Localization 系统初始化完成
                    await LocalizationSettings.InitializationOperation.Task;

                    Debug.LogWarning("自动创建本地化条目功能仅在编辑器模式下可用");
                    return false;
                }
                catch (OperationCanceledException)
                {
                    Debug.Log($"创建本地化条目操作被取消: {key}");
                    return false;
                }
                catch (Exception e)
                {
                    Debug.LogError($"创建本地化条目失败: {e.Message}");
                    return false;
                }
            }

            /// <summary>
            /// 清理资源
            /// </summary>
            public void Dispose()
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }

            public static Localization CreateChapter(int number = 0, string defaultText = "")
            {
                return new Localization
                {
                    Type = NodeType.Chapter,
                    DefaultText = defaultText,
                    Number = number,
                };
            }

            public static Localization CreateEpisode(int number = 0, string defaultText = "")
            {
                return new Localization
                {
                    Type = NodeType.Episode,
                    DefaultText = defaultText,
                    Number = number,
                };
            }

            public static Localization CreateLine(
                int number = 0,
                string defaultText = "",
                string speakerKey = ""
            )
            {
                return new Localization
                {
                    Type = NodeType.Line,
                    DefaultText = defaultText,
                    Number = number,
                    SpeakerKey = speakerKey,
                };
            }

            [JsonConstructor]
            private Localization() { }
        }

        // 标记父实体位置的组件
        [JsonObject(MemberSerialization.OptIn)]
        public class Parent : IComponent
        {
            [JsonProperty]
            public int? ParentId { get; set; }

            [JsonIgnore]
            public Entity ParentEntity { get; set; }

            public Parent(Entity parent = null)
            {
                if (parent != null)
                {
                    ParentEntity = parent;
                    ParentId = parent?.Id;
                }
            }

            public void SetParent(Entity parent)
            {
                ParentEntity = parent;
                ParentId = parent?.Id;
            }

            [JsonConstructor]
            private Parent() { }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Children : IComponent
        {
            [JsonProperty]
            public List<int> ChildrenIds { get; set; } = new();

            [JsonIgnore]
            public List<Entity> ChildrenEntities { get; private set; } = new();

            public void AddChild(Entity child)
            {
                if (!ChildrenIds.Contains(child.Id))
                {
                    ChildrenIds.Add(child.Id);
                    ChildrenEntities.Add(child);
                }
            }

            public void BuildChildrenEntities(Func<int, Entity> entityResolver)
            {
                ChildrenEntities = ChildrenIds
                    .Select(id => entityResolver(id))
                    .Where(entity => entity != null)
                    .ToList();
            }

            public void RemoveChild(Entity child)
            {
                ChildrenIds.Remove(child.Id);
                ChildrenEntities.Remove(child);
            }

            public Children(Func<int, Entity> entityResolver = null)
            {
                if (entityResolver != null)
                {
                    BuildChildrenEntities(entityResolver);
                }
            }

            [JsonConstructor]
            private Children() { }
        }
    }
}
