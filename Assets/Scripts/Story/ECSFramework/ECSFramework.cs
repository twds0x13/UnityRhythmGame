using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 看起来能用？

namespace ECS
{
    /// <summary>
    /// 用来管理 ECS 实体的类 会在使用时懒初始化
    /// </summary>
    public class ECSFramework
    {
        // 天机工程这一块/.

        public const int MAX_TREE_DEPTH = 3;

        private static readonly Lazy<ECSFramework> _instance = new(() => new());

        public static ECSFramework Inst => _instance.Value;

        private ECSFramework() { }

        private readonly Dictionary<int, Entity> _entities = new();

        public Entity CreateEntity()
        {
            int id;

            var root = GetRootEntity();

            if (root != null && root.HasComponent<Comp.IdManager>())
            {
                var idManager = root.GetComponent<Comp.IdManager>();
                id = idManager.GetNextId();

                // 确保不会返回 0（保留给根节点）
                while (id == 0)
                {
                    id = idManager.GetNextId();
                }
            }
            else
            {
                // 回退到简单计数器（仅用于测试或特殊情况）
                id = 1;

                while (_entities.ContainsKey(id))
                {
                    id++;
                }

                Debug.LogWarning($"无法获取 IdManager ，已回退到简单计数器。(ID : {id} )");
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
        /// 强制添加实体，跳过 ID 检查和管理器注册
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        /// <param name="isRoot">是否是根节点</param>
        public void ForceAddEntity(Entity entity, bool isRoot = false, bool output = false)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // 确保实体有有效的 ID
            if (!(entity.Id == 0))
                throw new ArgumentException("实体必须有一个有效的 ID", nameof(entity));

            int id = entity.Id;

            if (_entities.ContainsKey(id))
            {
                // 如果实体已存在，记录警告但不抛出异常
                LogManager.Warning($"实体 ID {id} 已存在，将强制替换");
                if (isRoot)
                {
                    LogManager.Log($"初始化根节点 Id = {id}");
                }
            }

            _entities[id] = entity;

            // 如果是根节点，需要特殊处理 ID 管理器
            if (isRoot && entity.HasComponent<Comp.IdManager>())
            {
                var idManager = entity.GetComponent<Comp.IdManager>();

                // 确保当前实体的 ID 已注册
                if (!idManager.IdRegistered(id))
                {
                    idManager.RegisterId(id);
                }

                // 设置下一个可用 ID
                if (_entities.Count > 0)
                {
                    int maxId = _entities.Keys.Max();
                    idManager.NextAvailableId = maxId + 1;
                }
                else
                {
                    idManager.NextAvailableId = id + 1; // 从当前 ID + 1 开始
                }

                LogManager.Info(
                    $"ID 管理器已更新，下一个可用 ID: {idManager.NextAvailableId}",
                    nameof(ECSFramework),
                    output
                );
            }
        }

        /// <summary>
        /// 添加一个已存在的实体（从序列化中读取）
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        /// <param name="overwrite">强制覆写，危险操作，后果自负</param>
        public void AddEntity(Entity entity, bool overwrite = false, bool output = false)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            int id = entity.Id;

            if (_entities.ContainsKey(id))
            {
                if (overwrite)
                {
                    LogManager.Warning($"实体 ID {id} 已存在，将强制替换");
                    _entities[id] = entity;
                    return;
                }
                else
                {
                    throw new ArgumentException($"实体ID {id} 已存在");
                }
            }

            _entities[id] = entity;

            LogManager.Info($"已添加实体 ID: {id}", nameof(ECSFramework), output);

            var root = GetRootEntity();

            // 按照流程，所有获取到的实体的 id 都应该已经注册在 Root 节点 的 IdManager 中
            if (root != null && root.HasComponent<Comp.IdManager>())
            {
                var idManager = root.GetComponent<Comp.IdManager>();

                // 检查 ID 是否已注册
                if (!idManager.IdRegistered(id))
                {
                    LogManager.Error($"检测到未注册实体：{id}", nameof(ECSFramework), output);
                }

                // 更新下一个可用 ID
                if (id >= idManager.NextAvailableId)
                {
                    idManager.NextAvailableId = id + 1;
                    LogManager.Info(
                        $"更新下一个可用 ID: {idManager.NextAvailableId}",
                        nameof(ECSFramework),
                        output
                    );
                }
            }
            else
            {
                LogManager.Error("找不到根节点或 ID 管理器", nameof(ECSFramework));
            }
        }

        /// <summary>
        /// 获取实体的完整路径
        /// </summary>
        public string GetEntityPath(Entity entity)
        {
            if (entity == null)
                return string.Empty;

            var path = new List<string>();
            var current = entity;

            while (current != null)
            {
                if (current.HasComponent<Comp.Localization>())
                {
                    var loc = current.GetComponent<Comp.Localization>();
                    path.Insert(0, $"{loc.Type}_{loc.Number}");
                }
                else
                {
                    path.Insert(0, $"Entity_{current.Id}");
                }

                if (current.HasComponent<Comp.Parent>())
                {
                    var parentComp = current.GetComponent<Comp.Parent>();
                    current = parentComp.ParentId.HasValue
                        ? GetEntitySafe(parentComp.ParentId.Value)
                        : null;
                }
                else
                {
                    current = null;
                }
            }

            return string.Join("/", path);
        }

        /// <summary>
        /// 检查实体是否是指定实体的后代
        /// </summary>
        public bool IsDescendantOf(Entity entity, Entity potentialAncestor)
        {
            if (entity == null || potentialAncestor == null)
                return false;

            var current = entity;
            while (current != null && current.HasComponent<Comp.Parent>())
            {
                var parentComp = current.GetComponent<Comp.Parent>();
                if (parentComp.ParentId == potentialAncestor.Id)
                    return true;

                current = parentComp.ParentId.HasValue
                    ? GetEntitySafe(parentComp.ParentId.Value)
                    : null;
            }

            return false;
        }

        /// <summary>
        /// 从指定节点开始从深到浅遍历实体树，并返回实体列表
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="entity">起始实体</param>
        /// <param name="func">要对每个实体执行的操作</param>
        /// <returns>操作结果列表（深度优先顺序）</returns>
        public List<Entity> TraverseDepthFirst(Entity entity)
        {
            return TraverseDepthFirst(entity, e => e).ToList();
        }

        /// <summary>
        /// 从指定节点开始从深到浅遍历实体树，对每个实体执行指定操作，并返回操作结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="entity">起始实体</param>
        /// <param name="func">要对每个实体执行的操作</param>
        /// <returns>操作结果列表（深度优先顺序）</returns>
        public List<T> TraverseDepthFirst<T>(Entity entity, Func<Entity, T> func)
        {
            var results = new List<T>();

            if (entity == null)
                return results;

            // 先处理所有子节点
            if (entity.HasComponent<Comp.Children>())
            {
                var children = entity.GetComponent<Comp.Children>().ChildrenEntities.ToList();
                foreach (var child in children)
                {
                    results.AddRange(TraverseDepthFirst(child, func));
                }
            }

            // 然后处理当前节点
            results.Add(func(entity));

            return results;
        }

        public List<T> TraverseDepthFirst<T>(int entityId, Func<Entity, T> func)
        {
            var entity = GetEntitySafe(entityId);
            if (entity != null)
            {
                return TraverseDepthFirst(entity, func);
            }
            return new List<T>();
        }

        /// <summary>
        /// 从指定节点开始从深到浅遍历实体树，对每个实体执行指定操作
        /// </summary>
        /// <param name="entity">起始实体</param>
        /// <param name="action">要对每个实体执行的操作</param>
        public void TraverseDepthFirst(Entity entity, Action<Entity> action)
        {
            if (entity == null || !entity.HasComponent<Comp.Children>())
                return;

            // 先处理所有子节点
            if (entity.HasComponent<Comp.Children>())
            {
                var children = entity.GetComponent<Comp.Children>().ChildrenEntities.ToList();
                foreach (var child in children)
                {
                    TraverseDepthFirst(child, action);
                }
            }

            // 然后处理当前节点
            action(entity);
        }

        /// <summary>
        /// 从深到浅遍历实体树，对每个实体执行指定操作（通过实体ID）
        /// </summary>
        /// <param name="entityId">起始实体ID</param>
        /// <param name="action">要对每个实体执行的操作</param>
        public void TraverseDepthFirst(int entityId, Action<Entity> action)
        {
            var entity = GetEntitySafe(entityId);
            if (entity != null)
            {
                TraverseDepthFirst(entity, action);
            }
        }

        /// <summary>
        /// 从指定节点深度向上查找所有父节点，直到根节点
        /// </summary>
        /// <param name="startEntity">起始节点</param>
        /// <returns>父节点列表（从直接父节点到根节点，不包括起始节点）</returns>
        public List<Entity> GetPathToRoot(Entity startEntity)
        {
            var path = new List<Entity>();

            if (startEntity == null)
                return path;

            var current = startEntity;

            while (current != null)
            {
                // 检查当前节点是否有父节点
                if (!current.HasComponent<Comp.Parent>())
                    break;

                var parentComp = current.GetComponent<Comp.Parent>();
                if (!parentComp.ParentId.HasValue)
                    break;

                // 获取父节点
                var parent = GetEntitySafe(parentComp.ParentId.Value);
                if (parent == null)
                    break;

                // 添加到路径
                path.Add(parent);

                // 如果到达根节点，停止遍历
                if (parent.HasComponent<Comp.Root>())
                    break;

                current = parent;
            }

            return path;
        }

        /// <summary>
        /// 获取从当前节点到根节点的完整路径（包括起始节点）
        /// </summary>
        public List<Entity> GetFullPathFromRoot(Entity startEntity)
        {
            var path = GetPathToRoot(startEntity);

            // 反转路径，使其从根节点到当前节点的父节点
            path.Reverse();

            // 添加起始节点
            if (startEntity != null)
            {
                path.Add(startEntity);
            }

            return path;
        }

        /// <summary>
        /// 一个更安全的获取实体的方法，带有详细的日志输出
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Entity GetEntitySafe(int? id)
        {
            return id.HasValue ? GetEntitySafe(id.Value) : null;
        }

        /// <summary>
        /// 一个更安全的获取实体的方法，带有详细的日志输出
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // 或许我应该把已经完整测试过的方法都加上 "Safe" 标签?
        public Entity GetEntitySafe(int id)
        {
            // 检查实体字典是否已初始化
            if (_entities == null)
            {
                LogManager.Error("实体字典为 null");
                return null;
            }

            // 尝试获取实体
            if (_entities.TryGetValue(id, out var entity))
            {
                return entity;
            }

            // 记录详细的错误信息

            LogManager.Warning($"找不到实体 ID: {id}");

            // 输出所有可用的实体 ID 以帮助诊断

            var availableIds = _entities.Keys.OrderBy(k => k).ToList();
            LogManager.Info($"可用的实体 ID: {string.Join(", ", availableIds)}");

            return null;
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

            var entity = GetEntitySafe(id);

            // 移除父子关系
            if (entity.HasComponent<Comp.Parent>())
            {
                var parentComp = entity.GetComponent<Comp.Parent>();

                if (parentComp.ParentId.HasValue)
                {
                    var parent = GetEntitySafe(parentComp.ParentId.Value);

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
                    var currentParent = GetEntitySafe(currentParentComp.ParentId.Value);

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

            // 直接向上遍历即可
            Entity current = potentialParent;

            // 最深遍历次数为树的高度（当然，你想多遍历几个来预防节点超级大回环也可以，但是现在的代码结构应该不会生成类似的东西）
            for (int i = 0; i < MAX_TREE_DEPTH; i++)
            {
                // 如果当前节点没有父节点，停止遍历
                if (current == null || !current.HasComponent<Comp.Parent>())
                    break;

                var parentComp = current.GetComponent<Comp.Parent>();
                if (!parentComp.ParentId.HasValue)
                    break;

                // 获取父节点
                current = GetEntitySafe(parentComp.ParentId.Value);
                if (current == null)
                    break;

                // 如果找到子节点，说明有循环引用
                if (current == child)
                    return true;
            }

            return false;
        }

        // 获取所有直接子节点
        public List<Entity> GetChildren(int entityId)
        {
            return GetChildren(GetEntitySafe(entityId));
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

            var parent = GetEntitySafe(parentComp.ParentId.Value);
            if (parent == null)
                return 1;

            return 1 + GetNodeLevel(parent);
        }

        // 在绝大多数情况下，你可以安全的用这个方法来获取根节点
        public Entity GetRootEntity()
        {
            return GetEntitySafe(0);
        }

        // 在ECSFramework中手动查找根节点的方法
        public Entity FindRootEntity()
        {
            // 查找所有没有父组件的实体
            var potentialRoots = _entities
                .Values.Where(e =>
                    !e.HasComponent<Comp.Parent>()
                    || (
                        e.HasComponent<Comp.Parent>()
                        && !e.GetComponent<Comp.Parent>().ParentId.HasValue
                    )
                )
                .ToList();

            // 如果有多个候选根节点，选择有Root组件的那个
            var rootWithComponent = potentialRoots.FirstOrDefault(e => e.HasComponent<Comp.Root>());
            if (rootWithComponent != null)
            {
                return rootWithComponent;
            }

            // 如果没有找到有Root组件的根节点，返回第一个候选
            return potentialRoots.FirstOrDefault();
        }

        // 从一个普通节点向上获取到根节点
        public Entity GetRoot(Entity entity)
        {
            if (!entity.HasComponent<Comp.Parent>())
                return entity;

            var parentComp = entity.GetComponent<Comp.Parent>();
            if (!parentComp.ParentId.HasValue)
                return entity;

            var parent = GetEntitySafe(parentComp.ParentId.Value);
            return parent != null ? GetRoot(parent) : entity;
        }

        // 判断是否是叶子节点
        public bool IsLeaf(Entity entity)
        {
            return !entity.HasComponent<Comp.Children>()
                || entity.GetComponent<Comp.Children>().ChildrenEntities.Count == 0;
        }

        // 重建实体间的引用关系（在反序列化后调用）
        public void RebuildEntityReferences(bool output = false)
        {
            LogManager.Log("======ECS.开始重建引用======", nameof(ECSFramework), output);

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
                            var parentEntity = GetEntitySafe(parentComp.ParentId.Value);
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

                        LogManager.Log(
                            $"实体 {entity.Id} 有 {childrenComp.ChildrenIds.Count} 个子节点 ID",
                            nameof(ECSFramework),
                            output
                        );

                        // 重建 ChildrenEntities
                        childrenComp.BuildChildrenEntities(GetEntitySafe);

                        LogManager.Log(
                            $"重建实体 {entity.Id} 的子节点引用，共获取到 {childrenComp.ChildrenEntities.Count} 个子节点",
                            nameof(ECSFramework),
                            output
                        );

                        // 验证 ChildrenIds 和 ChildrenEntities 的一致性
                        if (childrenComp.ChildrenIds.Count != childrenComp.ChildrenEntities.Count)
                        {
                            errors.Add(
                                $"实体 {entity.Id} 的 ChildrenIds 和 ChildrenEntities 数量不一致"
                            );
                        }
                    }

                    // 重建 Choice 组件的目标实体引用
                    if (entity.HasComponent<Comp.Choice>())
                    {
                        var choiceComp = entity.GetComponent<Comp.Choice>();

                        // 确保 Options 不为 null
                        if (choiceComp.Options == null)
                        {
                            choiceComp.Options = new List<Comp.ChoiceOption>();
                            errors.Add(
                                $"实体 {entity.Id} 的 Choice.Options 为 null，已初始化为空列表"
                            );
                            continue;
                        }

                        LogManager.Log(
                            $"实体 {entity.Id} 有 {choiceComp.Options.Count} 个选择选项",
                            nameof(ECSFramework),
                            output
                        );

                        // 重建每个选项的目标实体引用
                        foreach (var option in choiceComp.Options)
                        {
                            if (option.TargetId <= 0)
                            {
                                errors.Add(
                                    $"实体 {entity.Id} 的选择选项有无效的目标ID: {option.TargetId}"
                                );
                                continue;
                            }

                            var targetEntity = GetEntitySafe(option.TargetId);
                            if (targetEntity != null)
                            {
                                option.TargetEntity = targetEntity;

                                // 如果没有显示文本，使用目标实体的默认文本
                                if (
                                    string.IsNullOrEmpty(option.DisplayText)
                                    && targetEntity.HasComponent<Comp.Localization>()
                                )
                                {
                                    var targetLoc = targetEntity.GetComponent<Comp.Localization>();
                                    option.DisplayText = targetLoc.DefaultText;
                                }
                            }
                            else
                            {
                                errors.Add(
                                    $"实体 {entity.Id} 的选择选项目标实体 {option.TargetId} 不存在"
                                );
                            }
                        }

                        LogManager.Log(
                            $"重建实体 {entity.Id} 的选择选项引用，共处理 {choiceComp.Options.Count} 个选项",
                            nameof(ECSFramework),
                            output
                        );
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
                LogManager.Error($"重建引用时发现 {errors.Count} 个问题:");
                foreach (var error in errors)
                {
                    LogManager.Error($"  - {error}");
                }
            }
            else
            {
                LogManager.Log("引用重建完成，未发现问题", nameof(ECSFramework), output);
            }

            // 验证重建后的树结构
            if (!ValidateStoryTree(false))
            {
                LogManager.Error("警告: 重建后的树结构验证失败");
            }

            LogManager.Log("======ECS.结束重建引用======\n", nameof(ECSFramework), output);
        }

        // 验证剧情树 作为一棵树的结构是否完整
        public bool ValidateStoryTree(bool output = true)
        {
            LogManager.Info("======ECS.开始验证树结构======", nameof(ECSFramework));

            // 检查是否有多个根节点
            var roots = _entities
                .Values.Where(e =>
                    !e.HasComponent<Comp.Parent>()
                    || !e.GetComponent<Comp.Parent>().ParentId.HasValue
                )
                .ToList();

            if (roots.Count != 1)
            {
                LogManager.Error($"剧情树应该有且只有一个根节点，当前有 {roots.Count} 个");
                return false;
            }

            var root = roots[0];

            // 检查最大深度是否正确 ( 树中仅包含根节点记为1 )
            var maxDepth = GetMaxDepth(root, 0);
            if (maxDepth > MAX_TREE_DEPTH)
            {
                LogManager.Error($"剧情树深度不应超过 {MAX_TREE_DEPTH}，当前深度为 {maxDepth}");
                return false;
            }

            // 检查是否有循环引用
            if (HasCycle(root))
            {
                LogManager.Error("剧情树中存在循环引用");
                return false;
            }

            LogManager.Info("ECS 检测无异常", nameof(ECSFramework));
            LogManager.Info("======ECS.结束验证树结构======\n", nameof(ECSFramework));

            return true;
        }

        // 获取子树的最大深度（从当前节点开始，设当前节点深度为0）
        private int GetMaxDepth(Entity node, int currentDepth = 0)
        {
            // 记录当前节点的深度
            // LogFile.Info($"节点 {node.Id} 的深度: {currentDepth}");

            if (!node.HasComponent<Comp.Children>())
                return currentDepth;

            var childrenComp = node.GetComponent<Comp.Children>();
            if (childrenComp.ChildrenEntities.Count == 0)
                return currentDepth;

            int maxDepth = currentDepth;
            foreach (var child in childrenComp.ChildrenEntities)
            {
                int childDepth = GetMaxDepth(child, currentDepth + 1);
                if (childDepth > maxDepth)
                    maxDepth = childDepth;
            }

            return maxDepth;
        }

        // 检查树中是否有循环引用
        public bool HasCycle(Entity root)
        {
            var visited = new HashSet<int?>();
            return HasCycle(root, visited);
        }

        // 子方法 不可外部调用
        private bool HasCycle(Entity node, HashSet<int?> visited)
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

        /// <summary>
        /// 交换两个实体及其所有子节点（通过交换父组件实现）
        /// 只能在同树深度内进行交换操作
        /// </summary>
        /// <param name="entity1">第一个实体</param>
        /// <param name="entity2">第二个实体</param>
        /// <returns>是否成功交换</returns>
        public bool SwapEntities(Entity entity1, Entity entity2)
        {
            if (entity1 == null || entity2 == null)
            {
                LogManager.Error("交换实体失败：实体不能为null");
                return false;
            }

            // 检查是否同一个实体
            if (entity1.Id == entity2.Id)
            {
                LogManager.Warning("交换实体失败：不能交换同一个实体");
                return false;
            }

            // 检查是否在同一层级（chapter/episode/line）
            if (!AreEntitiesAtSameHierarchyLevel(entity1, entity2))
            {
                LogManager.Error("交换实体失败：实体不在同一层级");
                return false;
            }

            // 获取父节点
            Entity parent1 = entity1.HasComponent<Comp.Parent>()
                ? GetEntitySafe(entity1.GetComponent<Comp.Parent>().ParentId)
                : null;
            Entity parent2 = entity2.HasComponent<Comp.Parent>()
                ? GetEntitySafe(entity2.GetComponent<Comp.Parent>().ParentId)
                : null;

            try
            {
                // 从各自的父节点移除实体
                if (parent1 != null && parent1.HasComponent<Comp.Children>())
                {
                    var childrenComp1 = parent1.GetComponent<Comp.Children>();
                    childrenComp1.RemoveChild(entity1);
                }

                if (parent2 != null && parent2.HasComponent<Comp.Children>())
                {
                    var childrenComp2 = parent2.GetComponent<Comp.Children>();
                    childrenComp2.RemoveChild(entity2);
                }

                // 交换父组件中的引用
                if (entity1.HasComponent<Comp.Parent>() && entity2.HasComponent<Comp.Parent>())
                {
                    var parentComp1 = entity1.GetComponent<Comp.Parent>();
                    var parentComp2 = entity2.GetComponent<Comp.Parent>();

                    // 交换父ID和父实体引用
                    int? tempParentId = parentComp1.ParentId;
                    Entity tempParentEntity = parentComp1.ParentEntity;

                    parentComp1.ParentId = parentComp2.ParentId;
                    parentComp1.ParentEntity = parentComp2.ParentEntity;

                    parentComp2.ParentId = tempParentId;
                    parentComp2.ParentEntity = tempParentEntity;
                }

                // 将实体添加到新的父节点
                if (parent2 != null && parent2.HasComponent<Comp.Children>())
                {
                    var childrenComp2 = parent2.GetComponent<Comp.Children>();

                    childrenComp2.AddChild(entity1);
                }

                if (parent1 != null && parent1.HasComponent<Comp.Children>())
                {
                    var childrenComp1 = parent1.GetComponent<Comp.Children>();

                    childrenComp1.AddChild(entity2);
                }

                LogManager.Info($"成功交换实体 {entity1.Id} 和 {entity2.Id}，包括它们的父节点关系");

                // 添加调试输出
                Debug.Log($"交换实体: {entity1.Id} <-> {entity2.Id}");
                Debug.Log($"实体 {entity1.Id} 的新父节点: {parent2?.Id}");
                Debug.Log($"实体 {entity2.Id} 的新父节点: {parent1?.Id}");

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Error($"交换实体时发生错误: {ex.Message}");

                // 尝试恢复原始状态
                try
                {
                    // 从新父节点移除实体
                    if (parent2 != null && parent2.HasComponent<Comp.Children>())
                    {
                        var childrenComp2 = parent2.GetComponent<Comp.Children>();
                        childrenComp2.RemoveChild(entity1);
                    }

                    if (parent1 != null && parent1.HasComponent<Comp.Children>())
                    {
                        var childrenComp1 = parent1.GetComponent<Comp.Children>();
                        childrenComp1.RemoveChild(entity2);
                    }

                    // 恢复原始父关系
                    if (entity1.HasComponent<Comp.Parent>() && entity2.HasComponent<Comp.Parent>())
                    {
                        var parentComp1 = entity1.GetComponent<Comp.Parent>();
                        var parentComp2 = entity2.GetComponent<Comp.Parent>();

                        parentComp1.ParentId = parent1?.Id;
                        parentComp1.ParentEntity = parent1;

                        parentComp2.ParentId = parent2?.Id;
                        parentComp2.ParentEntity = parent2;
                    }

                    // 将实体添加回原始父节点
                    if (parent1 != null && parent1.HasComponent<Comp.Children>())
                    {
                        var childrenComp1 = parent1.GetComponent<Comp.Children>();
                        childrenComp1.AddChild(entity1);
                    }

                    if (parent2 != null && parent2.HasComponent<Comp.Children>())
                    {
                        var childrenComp2 = parent2.GetComponent<Comp.Children>();
                        childrenComp2.AddChild(entity2);
                    }
                }
                catch (Exception recoveryEx)
                {
                    LogManager.Error($"恢复交换状态时发生错误: {recoveryEx.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// 只交换两个实体上的组件，保持引用关系不变
        /// 只能在同树深度内进行交换操作
        /// </summary>
        /// <param name="entity1">第一个实体</param>
        /// <param name="entity2">第二个实体</param>
        /// <returns>是否成功交换</returns>
        public bool SwapComponents(Entity entity1, Entity entity2)
        {
            if (entity1 == null || entity2 == null)
            {
                LogManager.Error("交换组件失败：实体不能为空");
                return false;
            }

            // 检查是否同一个实体
            if (entity1.Id == entity2.Id)
            {
                LogManager.Warning("交换组件失败：不能交换同一个实体");
                return false;
            }

            // 检查是否在同一层级（chapter/episode/line）
            if (!AreEntitiesAtSameHierarchyLevel(entity1, entity2))
            {
                LogManager.Error("交换组件失败：实体不在同一层级");
                return false;
            }

            try
            {
                // 获取所有组件类型（排除Parent和Children组件）
                var componentTypes1 = entity1
                    .GetComponentTypes()
                    .Where(t => t != typeof(Comp.Parent) && t != typeof(Comp.Children))
                    .ToList();

                var componentTypes2 = entity2
                    .GetComponentTypes()
                    .Where(t => t != typeof(Comp.Parent) && t != typeof(Comp.Children))
                    .ToList();

                // 临时存储组件
                var tempComponents = new Dictionary<Type, IComponent>();

                // 将entity1的组件暂存
                foreach (var type in componentTypes1)
                {
                    tempComponents[type] = entity1.GetComponent(type);
                    entity1.RemoveComponent(type);
                }

                // 将entity2的组件移动到entity1
                foreach (var type in componentTypes2)
                {
                    var component = entity2.GetComponent(type);
                    entity2.RemoveComponent(type);
                    entity1.AddComponent(component);

                    // 特殊处理Localization组件，确保其内部实体引用正确
                    if (type == typeof(Comp.Localization))
                    {
                        var locComponent = component as Comp.Localization;
                    }
                }

                // 将暂存的组件移动到entity2
                foreach (var kvp in tempComponents)
                {
                    entity2.AddComponent(kvp.Value);

                    // 特殊处理Localization组件，确保其内部实体引用正确
                    if (kvp.Key == typeof(Comp.Localization))
                    {
                        var locComponent = kvp.Value as Comp.Localization;
                    }
                }

                LogManager.Info($"成功交换实体 {entity1.Id} 和 {entity2.Id} 的组件");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Error($"交换组件时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查两个实体是否在同一层级（ Chapter / Episode / Line）
        /// </summary>
        /// <param name="entity1">第一个实体</param>
        /// <param name="entity2">第二个实体</param>
        /// <returns>是否在同一层级</returns>
        private bool AreEntitiesAtSameHierarchyLevel(Entity entity1, Entity entity2)
        {
            int level1 = GetNodeLevel(entity1);
            int level2 = GetNodeLevel(entity2);

            // 深度不同肯定不在同一层级
            if (level1 != level2)
                return false;

            // 检查两个实体是否有Localization组件
            bool hasLoc1 = entity1.HasComponent<Comp.Localization>();
            bool hasLoc2 = entity2.HasComponent<Comp.Localization>();

            // 如果都没有Localization组件，则无法确定层级，不允许交换
            if (!hasLoc1 && !hasLoc2)
            {
                LogManager.Error("无法确定实体层级：两个实体都没有Localization组件");
                return false;
            }

            // 如果只有一个有Localization组件，也不允许交换
            if (hasLoc1 != hasLoc2)
            {
                LogManager.Error("无法确定实体层级：只有一个实体有Localization组件");
                return false;
            }

            // 如果两个都有Localization组件，比较它们的NodeType
            var loc1 = entity1.GetComponent<Comp.Localization>();
            var loc2 = entity2.GetComponent<Comp.Localization>();

            return loc1.Type == loc2.Type;
        }
    }
}
