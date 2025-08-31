using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

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
            public string Label { get; set; } = "";

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

            public Order(int number, string label = "")
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
            public bool IsIdUsed(int id)
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
        public class Localization : IComponent
        {
            public enum NodeType
            {
                Chapter,
                Episode,
                Line,
            }

            [JsonProperty]
            public NodeType Type { get; set; }

            // 仅供测试使用。你在过剧情的时候不应该看见 "请输入文宇..."
            [JsonProperty]
            public string DefaultText { get; set; } = "";

            [JsonProperty]
            public int Number { get; set; } = 0;

            [JsonProperty]
            public string SpeakerKey { get; set; } = "";

            [JsonProperty]
            public string ContextKey { get; private set; } = "";

            // 添加对 Order 组件的引用
            [JsonIgnore]
            public int OrderNumber
            {
                get
                {
                    // 如果实体有 Order 组件，优先使用 Order 组件的序号
                    if (_entity != null && _entity.HasComponent<Order>())
                    {
                        return _entity.GetComponent<Order>().Number;
                    }

                    // 否则使用 Localization 组件自身的 Number

                    Debug.LogWarning(
                        $"实体 ID {_entity?.Id} 缺失 Order 组件，使用 Localization 组件的 Number 属性。"
                    );

                    return Number;
                }
            }

            [JsonIgnore]
            private Entity _entity;

            // 设置关联的实体（用于获取 Order 组件）
            public void SetEntity(Entity entity)
            {
                _entity = entity;
            }

            // 仅供开发者模式使用
            public void GenerateLocalizationKey(Entity entity, ECSFramework ecsManager)
            {
                if (entity == null || ecsManager == null)
                    return;

                SetEntity(entity);

                switch (Type)
                {
                    case NodeType.Chapter:
                        ContextKey = $"C{OrderNumber}";
                        break;

                    case NodeType.Episode:
                        // 获取父章节
                        if (entity.HasComponent<Parent>())
                        {
                            var parentComp = entity.GetComponent<Parent>();
                            if (parentComp.ParentId.HasValue)
                            {
                                var parent = ecsManager.GetEntity(parentComp.ParentId.Value);
                                if (parent != null && parent.HasComponent<Localization>())
                                {
                                    var parentLoc = parent.GetComponent<Localization>();
                                    ContextKey = $"{parentLoc.ContextKey}_E{OrderNumber}";
                                }
                            }
                        }
                        break;

                    case NodeType.Line:
                        // 获取父小节
                        if (entity.HasComponent<Parent>())
                        {
                            var parentComp = entity.GetComponent<Parent>();
                            if (parentComp.ParentId.HasValue)
                            {
                                var parent = ecsManager.GetEntity(parentComp.ParentId.Value);
                                if (parent != null && parent.HasComponent<Localization>())
                                {
                                    var parentLoc = parent.GetComponent<Localization>();
                                    ContextKey = $"{parentLoc.ContextKey}_L{OrderNumber}";
                                }
                            }
                        }
                        break;
                }
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
