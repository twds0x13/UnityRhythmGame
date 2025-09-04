using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ECS
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Entity
    {
        [JsonProperty]
        public int Id { get; set; }

        [JsonProperty]
        private readonly Dictionary<string, IComponent> _components = new();

        [JsonIgnore]
        public IEnumerable<IComponent> Components => _components.Values;

        public Entity(int id)
        {
            Id = id;
        }

        // 用来处理反序列化
        [JsonConstructor]
        private Entity() { }

        public void AddComponent<T>(T component)
            where T : IComponent
        {
            _components[typeof(T).FullName] = component;
        }

        /// <summary>
        /// 获取实体上所有组件的类型
        /// </summary>
        /// <returns>组件类型集合</returns>
        public IEnumerable<Type> GetComponentTypes()
        {
            return _components.Keys.Select(key => Type.GetType(key)).Where(type => type != null);
        }

        /// <summary>
        /// 获取特定类型的组件
        /// </summary>
        /// <param name="componentType">组件类型</param>
        /// <returns>组件实例，如果不存在则返回null</returns>
        public IComponent GetComponent(Type componentType)
        {
            return _components.ContainsKey(componentType.FullName)
                ? _components[componentType.FullName]
                : null;
        }

        public T GetComponent<T>()
            where T : IComponent
        {
            string name = typeof(T).FullName;
            return _components.ContainsKey(name) ? (T)_components[name] : default;
        }

        public bool HasComponent<T>()
            where T : IComponent
        {
            return _components.ContainsKey(typeof(T).FullName);
        }

        public void RemoveComponent(Type componentType)
        {
            _components.Remove(componentType.FullName);
        }

        public void RemoveComponent<T>()
            where T : IComponent
        {
            _components.Remove(typeof(T).FullName);
        }
    }
}
