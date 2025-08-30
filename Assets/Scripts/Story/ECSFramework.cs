using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// 看起来能用？

namespace ECS
{
    /// <summary>
    /// 用来管理 ECS 实体的类，应该在 StoryManager 中创建唯一实例
    /// </summary>
    public class ECSManager
    {
        private readonly Dictionary<int, Entity> _entities = new();

        public Entity CreateEntity()
        {
            int id;

            var root = FindRootEntity();

            if (root != null && root.HasComponent<Comp.IdManager>())
            {
                var idManager = root.GetComponent<Comp.IdManager>();
                id = idManager.GetNextId();
            }
            else
            {
                // 回退到简单计数器（仅用于测试或特殊情况）
                id = _entities.Count > 0 ? _entities.Keys.Max() + 1 : 0;
                Debug.LogWarning("无法获取 IdManager ，已回退到简单计数器");
            }

            var entity = new Entity(id);
            _entities[id] = entity;
            return entity;
        }

        // 清空所有实体
        public void ClearAllEntities()
        {
            _entities.Clear();
        }

        /// <summary>
        /// 添加一个已存在的实体（ 从序列化中读取 ）
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="overwrite">强制覆写，危险操作，后果自负</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void AddEntity(Entity entity, bool overwrite = false)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (_entities.ContainsKey(entity.Id))
            {
                if (overwrite)
                {
                    _entities[entity.Id] = entity;
                    return;
                }
                else
                {
                    throw new ArgumentException($"实体ID {entity.Id} 已存在");
                }
            }

            _entities[entity.Id] = entity;

            // 注册 ID 到管理器
            var root = FindRootEntity();
            if (root != null && root.HasComponent<Comp.IdManager>())
            {
                var idManager = root.GetComponent<Comp.IdManager>();

                try
                {
                    idManager.RegisterId(entity.Id);
                }
                catch (ArgumentException ex)
                {
                    // 如果ID冲突且不允许覆盖，则抛出异常
                    if (!overwrite)
                    {
                        _entities.Remove(entity.Id);
                        throw new InvalidOperationException($"无法获取新 Id", ex);
                    }

                    // 如果允许覆盖，则继续
                    Console.WriteLine($"警告: {ex.Message}");
                }
            }
        }

        public Entity GetEntity(int id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }

        // 添加查找特定类型实体的方法
        public List<Entity> FindEntitiesWithComponent<T>()
            where T : IComponent
        {
            return _entities.Values.Where(e => e.HasComponent<T>()).ToList();
        }

        // 添加根据条件查找实体的方法
        public List<Entity> FindEntities(Func<Entity, bool> predicate)
        {
            return _entities.Values.Where(predicate).ToList();
        }

        public IEnumerable<Entity> GetAllEntities()
        {
            return _entities.Values;
        }

        public bool ExistEntity(int id)
        {
            return _entities.ContainsKey(id);
        }

        public void RemoveEntity(int id)
        {
            if (!ExistEntity(id))
                return;

            var entity = GetEntity(id);

            // 移除父子关系
            if (entity.HasComponent<Comp.Parent>())
            {
                var parentComp = entity.GetComponent<Comp.Parent>();

                if (parentComp.ParentId.HasValue)
                {
                    var parent = GetEntity(parentComp.ParentId.Value);

                    if (parent != null && parent.HasComponent<Comp.Children>())
                    {
                        var childrenComp = parent.GetComponent<Comp.Children>();
                        childrenComp.RemoveChild(entity);
                    }
                }
            }

            if (entity.HasComponent<Comp.Children>())
            {
                var childrenComp = entity.GetComponent<Comp.Children>();

                // 创建副本以避免在遍历时修改集合
                var childrenToRemove = childrenComp.ChildrenEntities.ToList();

                foreach (var child in childrenToRemove)
                {
                    if (child.HasComponent<Comp.Parent>())
                    {
                        var childParentComp = child.GetComponent<Comp.Parent>();
                        childParentComp.ParentId = null;
                        childParentComp.ParentEntity = null;
                    }
                }
            }

            // 更新实体计数
            var root = FindRootEntity();
            if (root != null && root.HasComponent<Comp.IdManager>())
            {
                var idManager = root.GetComponent<Comp.IdManager>();
                idManager.UnregisterId(id);
            }

            _entities.Remove(id);
        }

        // 子节点会作为子树的一部分被移动到新父节点下
        public void SetParent(Entity entity, Entity newParent)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // 检查循环引用
            if (newParent != null && CheckCycleReference(entity, newParent))
                throw new InvalidOperationException("设置父节点会产生循环引用");

            // 如果有父组件，就获取当前父节点组件
            Comp.Parent currentParentComp = null;
            if (entity.HasComponent<Comp.Parent>())
            {
                currentParentComp = entity.GetComponent<Comp.Parent>();

                // 从当前父节点移除（ 如果存在 ParentId ）
                if (currentParentComp.ParentId.HasValue)
                {
                    var currentParent = GetEntity(currentParentComp.ParentId.Value);

                    if (currentParent != null && currentParent.HasComponent<Comp.Children>())
                    {
                        var currentChildrenComp = currentParent.GetComponent<Comp.Children>();
                        currentChildrenComp.RemoveChild(entity);
                    }
                }
            }

            // 如果没有父组件，就生成新 Parent 组件
            if (currentParentComp == null)
            {
                currentParentComp = new Comp.Parent();
                entity.AddComponent(currentParentComp);
            }

            // 设置新的父节点
            currentParentComp.SetParent(newParent);

            // 将实体添加到新父节点
            if (newParent != null)
            {
                // 确保新父节点有Children组件
                if (!newParent.HasComponent<Comp.Children>())
                {
                    newParent.AddComponent(new Comp.Children());
                }

                var newChildrenComp = newParent.GetComponent<Comp.Children>();
                newChildrenComp.AddChild(entity);
            }
        }

        // 检查是否形成循环引用
        public bool CheckCycleReference(Entity child, Entity potentialParent)
        {
            // 快速检查：如果潜在父节点是子节点本身
            if (child == potentialParent)
                return true;

            // 对于深度只有3的树，直接向上遍历3次即可
            Entity current = potentialParent;

            // 最多遍历3层（深度为3）
            for (int i = 0; i < 6; i++)
            {
                // 如果当前节点没有父节点，停止遍历
                if (current == null || !current.HasComponent<Comp.Parent>())
                    break;

                var parentComp = current.GetComponent<Comp.Parent>();
                if (!parentComp.ParentId.HasValue)
                    break;

                // 获取父节点
                current = GetEntity(parentComp.ParentId.Value);
                if (current == null)
                    break;

                // 如果找到子节点，说明有循环引用
                if (current == child)
                    return true;
            }

            return false;
        }

        // 获取所有直接子节点
        public List<Entity> GetChildren(Entity parent)
        {
            if (parent == null || !parent.HasComponent<Comp.Children>())
                return new List<Entity>();

            return parent.GetComponent<Comp.Children>().ChildrenEntities.ToList(); // 会返回一个新的列表，和之前的列表解除引用
        }

        // 递归获取所有后代节点
        public List<Entity> GetAllDescendants(Entity parent)
        {
            var descendants = new List<Entity>();

            if (parent == null || !parent.HasComponent<Comp.Children>())
                return descendants;

            var children = parent.GetComponent<Comp.Children>().ChildrenEntities;

            foreach (var child in children)
            {
                descendants.Add(child);
                descendants.AddRange(GetAllDescendants(child));
            }

            return descendants;
        }

        // 获取节点的层级
        public int GetNodeLevel(Entity entity)
        {
            if (!entity.HasComponent<Comp.Parent>())
                return 0;

            var parentComp = entity.GetComponent<Comp.Parent>();
            if (!parentComp.ParentId.HasValue)
                return 1;

            var parent = GetEntity(parentComp.ParentId.Value);
            if (parent == null)
                return 1;

            return 1 + GetNodeLevel(parent);
        }

        // 添加获取根节点的公共方法
        public Entity FindRootEntity()
        {
            return _entities.Values.FirstOrDefault(e =>
                !e.HasComponent<Comp.Parent>() || !e.GetComponent<Comp.Parent>().ParentId.HasValue
            );
        }

        // 获取根节点
        public Entity GetRoot(Entity entity)
        {
            if (!entity.HasComponent<Comp.Parent>())
                return entity;

            var parentComp = entity.GetComponent<Comp.Parent>();
            if (!parentComp.ParentId.HasValue)
                return entity;

            var parent = GetEntity(parentComp.ParentId.Value);
            return parent != null ? GetRoot(parent) : entity;
        }

        // 判断是否是叶子节点
        public bool IsLeaf(Entity entity)
        {
            return !entity.HasComponent<Comp.Children>()
                || entity.GetComponent<Comp.Children>().ChildrenEntities.Count == 0;
        }

        // 重建实体间的引用关系（在反序列化后调用）
        public void RebuildReferences()
        {
            // 记录重建过程中发现的错误
            var errors = new List<string>();

            foreach (var entity in _entities.Values)
            {
                try
                {
                    // 重建父引用
                    if (entity.HasComponent<Comp.Parent>())
                    {
                        var parentComp = entity.GetComponent<Comp.Parent>();
                        if (parentComp.ParentId.HasValue)
                        {
                            var parentEntity = GetEntity(parentComp.ParentId.Value);
                            if (parentEntity != null)
                            {
                                parentComp.ParentEntity = parentEntity;
                            }
                            else
                            {
                                errors.Add(
                                    $"实体 {entity.Id} 的父实体 {parentComp.ParentId.Value} 不存在"
                                );
                                parentComp.ParentId = null; // 清除无效的父引用
                            }
                        }
                        else
                        {
                            parentComp.ParentEntity = null; // 确保 ParentEntity 与 ParentId 一致
                        }
                    }

                    // 重建子引用
                    if (entity.HasComponent<Comp.Children>())
                    {
                        var childrenComp = entity.GetComponent<Comp.Children>();

                        // 确保 ChildrenIds 不为 null
                        if (childrenComp.ChildrenIds == null)
                        {
                            childrenComp.ChildrenIds = new List<int>();
                            errors.Add(
                                $"实体 {entity.Id} 的 ChildrenIds 为 null，已初始化为空列表"
                            );
                        }

                        // 重建 ChildrenEntities
                        childrenComp.BuildChildrenEntities(GetEntity);

                        // 验证 ChildrenIds 和 ChildrenEntities 的一致性
                        if (childrenComp.ChildrenIds.Count != childrenComp.ChildrenEntities.Count)
                        {
                            errors.Add(
                                $"实体 {entity.Id} 的 ChildrenIds 和 ChildrenEntities 数量不一致"
                            );

                            // 尝试修复：根据 ChildrenIds 重新构建 ChildrenEntities
                            childrenComp.BuildChildrenEntities(GetEntity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"重建实体 {entity.Id} 的引用时发生异常: {ex.Message}");
                }
            }

            // 输出重建过程中的错误信息
            if (errors.Count > 0)
            {
                Console.WriteLine($"重建引用时发现 {errors.Count} 个问题:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            else
            {
                Console.WriteLine("引用重建完成，未发现错误");
            }

            // 验证重建后的树结构
            if (!ValidateStoryTree())
            {
                Console.WriteLine("警告: 重建后的树结构验证失败");
            }
        }

        // 验证剧情树结构是否完整
        public bool ValidateStoryTree()
        {
            // 检查是否有多个根节点
            var roots = _entities
                .Values.Where(e =>
                    !e.HasComponent<Comp.Parent>()
                    || !e.GetComponent<Comp.Parent>().ParentId.HasValue
                )
                .ToList();

            if (roots.Count != 1)
            {
                Console.WriteLine($"剧情树应该有且只有一个根节点，当前有 {roots.Count} 个");
                return false;
            }

            var root = roots[0];

            // 检查最大深度是否为3
            var maxDepth = GetMaxDepth(root);
            if (maxDepth > 3)
            {
                Console.WriteLine($"剧情树深度不应超过3，当前深度为 {maxDepth}");
                return false;
            }

            // 检查是否有循环引用
            if (HasCycle(root))
            {
                Console.WriteLine("剧情树中存在循环引用");
                return false;
            }

            return true;
        }

        // 获取树的最大深度
        private int GetMaxDepth(Entity node)
        {
            if (!node.HasComponent<Comp.Children>())
                return 1;

            var childrenComp = node.GetComponent<Comp.Children>();
            if (childrenComp.ChildrenEntities.Count == 0)
                return 1;

            int maxDepth = 0;
            foreach (var child in childrenComp.ChildrenEntities)
            {
                int childDepth = GetMaxDepth(child);
                if (childDepth > maxDepth)
                    maxDepth = childDepth;
            }

            return 1 + maxDepth;
        }

        // 检查树中是否有循环引用
        public bool HasCycle(Entity root)
        {
            var visited = new HashSet<int>();
            return HasCycle(root, visited);
        }

        private bool HasCycle(Entity node, HashSet<int> visited)
        {
            if (visited.Contains(node.Id))
                return true;
            visited.Add(node.Id);

            if (node.HasComponent<Comp.Children>())
            {
                var childrenComp = node.GetComponent<Comp.Children>();
                foreach (var child in childrenComp.ChildrenEntities)
                {
                    if (HasCycle(child, visited))
                        return true;
                }
            }

            return false;
        }
    }
}
