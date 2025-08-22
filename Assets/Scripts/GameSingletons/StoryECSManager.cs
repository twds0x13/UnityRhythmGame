using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryECS = StoryECSNS.StoryECSManager;

namespace StoryECSNS
{
    // 尝试用 ECS 设计模式重构 OOP 剧情框架

    [JsonObject(MemberSerialization.OptIn)]
    public struct Entity : IEquatable<Entity>
    {
        [JsonProperty]
        public int Id;

        public override string ToString() => $"Entity_{Id}";

        public bool Equals(Entity other) => Id == other.Id;

        public override int GetHashCode() => Id.GetHashCode();
    }

    public interface IEntityComponent { }

    [JsonConverter(typeof(ComponentContainerConverter))]
    public class ComponentContainer
    {
        public Dictionary<Type, IEntityComponent> Components { get; } = new();
    }

    public class ComponentContainerConverter : JsonConverter<ComponentContainer>
    {
        public override void WriteJson(
            JsonWriter Writer,
            ComponentContainer Value,
            JsonSerializer Serializer
        )
        {
            Writer.WriteStartObject();
            foreach (var Kvp in Value.Components)
            {
                Writer.WritePropertyName(Kvp.Key.FullName);
                Serializer.Serialize(Writer, Kvp.Value);
            }
            Writer.WriteEndObject();
        }

        public override ComponentContainer ReadJson(
            JsonReader Reader,
            Type ObjectType,
            ComponentContainer ExistingValue,
            bool HasExistingValue,
            JsonSerializer Serializer
        )
        {
            var Container = new ComponentContainer();
            var jObject = JObject.Load(Reader);

            foreach (var Prop in jObject.Properties())
            {
                var Type = System.Type.GetType(Prop.Name);
                if (Type != null)
                {
                    var Component = (IEntityComponent)Prop.Value.ToObject(Type, Serializer);
                    Container.Components[Type] = Component;
                }
            }

            return Container;
        }
    }

    // 用组件代替 OOP 中的大量参数

    [JsonObject(MemberSerialization.OptIn)]
    public struct StoryPosition : IEntityComponent // 人类可读
    {
        [JsonProperty]
        public int Chapter;

        [JsonProperty]
        public int Episode;

        [JsonProperty]
        public int Line;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct StoryNode : IEntityComponent
    {
        [JsonProperty]
        public StoryType Type;

        [JsonProperty]
        public int Depth;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct Localization : IEntityComponent
    {
        [JsonProperty]
        public string Key;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct Parent : IEntityComponent // 棍木
    {
        [JsonProperty]
        public Entity ParentEntity;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct Children : IEntityComponent // 子节点
    {
        [JsonProperty]
        public List<Entity> ChildEntity;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct Metadata : IEntityComponent // 这剧情谁改的？
    {
        [JsonProperty]
        public string Modifier;

        [JsonProperty]
        public DateTime CreatedDate;

        [JsonProperty]
        public DateTime ModifiedDate;
    }

    public enum StoryType
    {
        Chapter,
        Episode,
        Line,
    }

    public static class LocalizationUtils
    {
        public static string GenerateLocalizationKey(Entity Entity, StoryECS Manager)
        {
            if (!Manager.HasComponent<StoryPosition>(Entity))
                return "UNKNOWN";

            var Pos = Manager.GetComponent<StoryPosition>(Entity);
            return $"C{Pos.Chapter}_E{Pos.Episode}_L{Pos.Line}";
        }
    }

    public static class StoryTreeUtils
    {
        /// <summary>
        /// 为该 <paramref name="Parent"/> 实体添加一个 <see cref="Children"/> 实体
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Parent"></param>
        /// <param name="Child"></param>
        public static void AddChild(
            this StoryECS Manager,
            Entity Parent,
            Entity Child,
            StoryType Type
        )
        {
            if (!Manager.HasComponent<Children>(Parent))
            {
                Manager.AddComponent(Parent, new Children { ChildEntity = new() });
            }

            var Children = Manager.GetComponent<Children>(Parent);
            Children.ChildEntity.Add(Child);

            Manager.AddComponent(Child, new Parent { ParentEntity = Parent });

            Manager.AddComponent(Child, new StoryNode { Type = Type });

            SetPositionInfo(Manager, Parent, Child, Type);

            string Key = LocalizationUtils.GenerateLocalizationKey(Child, Manager);

            Manager.AddComponent(Child, new Localization { Key = Key });

            Manager.AddComponent(
                Child,
                new Metadata
                {
                    Modifier = Environment.UserName,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                }
            );
        }

        private static void SetPositionInfo(
            StoryECS Manager,
            Entity Parent,
            Entity Child,
            StoryType Type
        )
        {
            var Position = new StoryPosition();

            if (Manager.HasComponent<StoryPosition>(Parent))
            {
                var ParentPos = Manager.GetComponent<StoryPosition>(Parent);
                Position.Chapter = ParentPos.Chapter;

                switch (Type)
                {
                    case StoryType.Episode:
                        Position.Episode = GetNextEpisodeIndex(Manager, Parent);
                        Position.Line = 0;
                        break;

                    case StoryType.Line:
                        Position.Episode = ParentPos.Episode;
                        Position.Line = GetNextLineIndex(Manager, Parent);
                        break;
                }
            }
            else // 张杰
            {
                Position.Chapter = GetNextChapterIndex(Manager);
                Position.Episode = 0;
                Position.Line = 0;
            }

            Manager.AddComponent(Child, Position);
        }

        // 获取下一个章节索引
        private static int GetNextChapterIndex(StoryECS Manager)
        {
            var Chapters = Manager
                .GetEntitiesWith<StoryNode>()
                .Where(e => Manager.GetComponent<StoryNode>(e).Type == StoryType.Chapter)
                .Select(e => Manager.GetComponent<StoryPosition>(e).Chapter);

            return Chapters.Any() ? Chapters.Max() + 1 : 1;
        }

        // 获取下一个片段索引
        private static int GetNextEpisodeIndex(StoryECS Manager, Entity parent)
        {
            var Childrens = Manager
                .GetChildrens(parent)
                .Where(e =>
                    Manager.HasComponent<StoryNode>(e)
                    && Manager.GetComponent<StoryNode>(e).Type == StoryType.Episode
                );

            return Childrens.Any()
                ? Childrens.Max(e => Manager.GetComponent<StoryPosition>(e).Episode) + 1
                : 1;
        }

        private static int GetNextLineIndex(StoryECS Manager, Entity Parent)
        {
            var Lines = Manager
                .GetChildrens(Parent)
                .Where(e =>
                    Manager.HasComponent<StoryNode>(e)
                    && Manager.GetComponent<StoryNode>(e).Type == StoryType.Line
                );

            return Lines.Any()
                ? Lines.Max(e => Manager.GetComponent<StoryPosition>(e).Line) + 1
                : 1;
        }

        /// <summary>
        /// 获取该 <paramref name="Parent"/> 实体的全部 <see cref="Children"/> 实体
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Parent"></param>
        /// <returns></returns>
        public static IEnumerable<Entity> GetChildrens(this StoryECS Manager, Entity Parent)
        {
            if (Manager.HasComponent<Children>(Parent))
            {
                return Manager.GetComponent<Children>(Parent).ChildEntity;
            }
            return Enumerable.Empty<Entity>();
        }

        /// <summary>
        /// 递归获取该 <paramref name="Parent"/> 实体的全部 <see cref="Children"/> 后代
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Parent"></param>
        /// <returns></returns>
        public static IEnumerable<Entity> GetAllDescendants(this StoryECS Manager, Entity Parent)
        {
            var Descendants = new List<Entity>();

            foreach (var child in Manager.GetChildrens(Parent)) // 卧槽 这么有智慧
            {
                Descendants.Add(child);
                Descendants.AddRange(Manager.GetAllDescendants(child));
            }

            return Descendants;
        }

        public static Entity? GetParent(this StoryECS Manager, Entity Child)
        {
            if (Manager.HasComponent<Parent>(Child))
            {
                return Manager.GetComponent<Parent>(Child).ParentEntity;
            }
            return null;
        }

        /// <summary>
        /// 用的时候不要改 <paramref name="Depth"/>
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Root"></param>
        /// <param name="Action"></param>
        /// <param name="Depth"></param>
        public static void TraverseTree(
            this StoryECS Manager,
            Entity Root,
            Action<Entity, int> Action,
            int Depth = 0
        )
        {
            Action?.Invoke(Root, Depth);

            foreach (var Child in Manager.GetChildrens(Root))
            {
                Manager.TraverseTree(Child, Action, Depth + 1);
            }
        }
    }

    public class StoryECSManager
    {
        [JsonProperty]
        private int EntityId = 1;

        [JsonProperty]
        private readonly Dictionary<int, ComponentContainer> EntityComponents = new();

        public Entity CreateEntity()
        {
            var Id = EntityId++;
            EntityComponents[Id] = new();
            return new Entity { Id = Id };
        }

        public void AddComponent<T>(Entity Entity, T Component)
            where T : IEntityComponent
        {
            if (!EntityComponents.TryGetValue(Entity.Id, out var Container))
            {
                Container = new ComponentContainer();
                EntityComponents[Entity.Id] = Container;
            }
            Container.Components[typeof(T)] = Component;
        }

        public T GetComponent<T>(Entity Entity)
            where T : IEntityComponent
        {
            return (T)EntityComponents[Entity.Id].Components[typeof(T)];
        }

        public bool HasComponent<T>(Entity Entity)
            where T : IEntityComponent
        {
            return EntityComponents.TryGetValue(Entity.Id, out var Container)
                && Container.Components.ContainsKey(typeof(T));
        }

        public IEnumerable<Entity> GetEntitiesWith<T>()
            where T : IEntityComponent
        {
            foreach (var Pair in EntityComponents)
            {
                if (Pair.Value.Components.ContainsKey(typeof(T)))
                {
                    yield return new Entity { Id = Pair.Key };
                }
            }
        }

        public StoryECS Reference => this;
    }
}
