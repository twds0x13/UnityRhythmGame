using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ECS
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Entity
    {
        [JsonProperty]
        public int Id { get; }

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

        public void RemoveComponent<T>()
            where T : IComponent
        {
            _components.Remove(typeof(T).FullName);
        }
    }
}
