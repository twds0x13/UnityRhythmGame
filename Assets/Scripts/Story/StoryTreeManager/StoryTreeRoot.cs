using System;
using System.Collections.Generic;
using System.Linq;
using static ECS.Comp;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public bool HasRoot()
        {
            return FindActualRootEntities(false).Any();
        }

        /// <summary>
        /// 查找所有可能成为根节点的实体（没有父节点或父节点不存在的实体）
        /// 优先考虑 ID=0 的实体
        /// </summary>
        /// <returns>潜在根节点实体的列表，按优先级排序</returns>
        public List<Entity> FindPotentialRootEntities(bool enableLog = true)
        {
            var potentialRoots = new List<Entity>();
            var allEntities = _ecsFramework.GetAllEntities();

            // 首先查找 ID=0 的实体
            var entityWithIdZero = allEntities.FirstOrDefault(e => e.Id == 0);
            if (entityWithIdZero != null)
            {
                // ID=0 的实体有最高优先级
                potentialRoots.Add(entityWithIdZero);

                LogManager.Log(
                    $"搜索到 ID=0 的实体: ID={entityWithIdZero.Id}",
                    nameof(StoryTreeManager),
                    enableLog
                );
            }

            foreach (var entity in allEntities)
            {
                // 跳过 ID=0 的实体，因为已经处理过了
                if (entity.Id == 0)
                    continue;

                // 检查实体是否有父组件
                if (!entity.HasComponent<Parent>())
                {
                    // 没有父组件，可能是根节点
                    potentialRoots.Add(entity);
                    LogManager.Log(
                        $"搜索到潜在根节点 (无父组件): ID={entity.Id}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                    continue;
                }

                var parentComp = entity.GetComponent<Parent>();

                // 检查父组件是否有父ID
                if (!parentComp.ParentId.HasValue)
                {
                    // 有父组件但没有父ID，可能是根节点
                    potentialRoots.Add(entity);
                    LogManager.Log(
                        $"搜索到潜在根节点 (无父ID): ID={entity.Id}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                    continue;
                }

                // 检查父ID对应的实体是否存在
                var parentEntity = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
                if (parentEntity == null)
                {
                    // 父实体不存在，可能是根节点
                    potentialRoots.Add(entity);
                    LogManager.Log(
                        $"找到潜在根节点 (父实体不存在): ID={entity.Id}, 父ID={parentComp.ParentId.Value}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                }
            }

            LogManager.Info(
                $"共搜索到 {potentialRoots.Count} 个潜在的根节点实体",
                nameof(StoryTreeManager)
            );
            return potentialRoots;
        }

        public Entity GetOrCreateRoot()
        {
            if (_rootEntity != null)
            {
                LogManager.Info(
                    $"检测根节点是否非空 : {_rootEntity != null}",
                    nameof(StoryTreeManager),
                    false
                );

                return _rootEntity;
            }

            // 使用公共方法查找根节点
            _rootEntity = _ecsFramework.FindRootEntity();

            if (_rootEntity != null)
            {
                // 确保根节点有 Root 组件
                if (!_rootEntity.HasComponent<Root>())
                {
                    _rootEntity.AddComponent(new Root { RootName = RootName });

                    LogManager.Warning($"自动为 Root 实体 {_rootEntity.Id} 添加 Root 组件");
                }

                // 确保根节点有 ID 管理组件
                if (!_rootEntity.HasComponent<IdManager>())
                {
                    _rootEntity.AddComponent(new IdManager(true));

                    LogManager.Warning(
                        $"自动为 Root 实体 {_rootEntity.Id} 添加 IdManager 组件，初始值设定为 {0}"
                    );
                }

                if (_rootEntity.HasComponent<Parent>())
                {
                    _rootEntity.RemoveComponent<Parent>();

                    LogManager.Warning($"自动移除 Root 实体 {_rootEntity.Id} 上的 Parent 组件");
                }

                return _rootEntity;
            }
            else
            {
                // 创建新的根节点 (只有在没有找到根节点时才执行)

                return CreateRoot(RootName, true);
            }
        }

        /// <summary>
        /// 验证故事树中的根节点结构
        /// </summary>
        /// <returns>验证结果和相关信息</returns>
        public (bool isValid, string message) ValidateRootStructure()
        {
            var actualRoots = FindActualRootEntities(false);

            if (actualRoots.Count == 0)
            {
                return (false, "没有找到根节点实体");
            }

            if (actualRoots.Count > 1)
            {
                var rootIds = string.Join(", ", actualRoots.Select(r => r.Id));
                return (false, $"找到多个根节点实体: {rootIds}");
            }

            var root = actualRoots[0];

            // 检查根节点是否有父节点（不应该有）
            if (root.HasComponent<Parent>())
            {
                var parentComp = root.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    return (
                        false,
                        $"根节点 (ID={root.Id}) 有父节点 (ID={parentComp.ParentId.Value})"
                    );
                }
            }

            // 检查根节点是否有IdManager组件
            if (!root.HasComponent<IdManager>())
            {
                return (false, $"根节点 (ID={root.Id}) 没有IdManager组件");
            }

            return (true, $"根节点结构正常: ID={root.Id}");
        }

        /// <summary>
        /// 查找并返回所有真正的根节点实体（ 有Root组件 没有有效父节点 Id为0 的实体）
        /// </summary>
        /// <returns>真正的根节点实体的列表</returns>
        public List<Entity> FindActualRootEntities(bool enableLog = true)
        {
            var potentialRoots = FindPotentialRootEntities(enableLog);
            var actualRoots = new List<Entity>();

            foreach (var entity in potentialRoots)
            {
                if (entity.HasComponent<Root>())
                {
                    // 有Root组件，确认是根节点
                    actualRoots.Add(entity);
                    LogManager.Log(
                        $"确认根节点: ID={entity.Id}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                }
            }

            LogManager.Info(
                $"共搜索到 {actualRoots.Count} 个真正的根节点实体",
                nameof(StoryTreeManager)
            );
            return actualRoots;
        }

        /// <summary>
        /// 查找并修复根节点问题（多个根节点或没有根节点）
        /// </summary>
        /// <returns>修复后的根节点实体，如果修复失败返回null</returns>
        public Entity FindAndFixRootIssues()
        {
            var actualRoots = FindActualRootEntities();

            // 情况1：没有根节点
            if (actualRoots.Count == 0)
            {
                LogManager.Warning("没有找到根节点实体", nameof(StoryTreeManager));

                var potentialRoots = FindPotentialRootEntities();

                if (potentialRoots.Count > 0)
                {
                    // 选择第一个潜在根节点并添加Root组件
                    var selectedRoot = potentialRoots[0];
                    LogManager.Info(
                        $"将实体 ID={selectedRoot.Id} 转换为根节点",
                        nameof(StoryTreeManager)
                    );

                    selectedRoot.AddComponent(new Root { RootName = "修复的根节点" });

                    // 确保没有父组件
                    if (selectedRoot.HasComponent<Parent>())
                    {
                        selectedRoot.RemoveComponent<Parent>();
                    }

                    // 确保有IdManager组件
                    if (!selectedRoot.HasComponent<IdManager>())
                    {
                        selectedRoot.AddComponent(new IdManager(true));
                    }

                    _rootEntity = selectedRoot;
                    return selectedRoot;
                }
                else
                {
                    // 没有潜在根节点，创建新的根节点
                    LogManager.Info("创建新的根节点", nameof(StoryTreeManager));
                    return CreateRoot("新建根节点");
                }
            }

            // 情况2：有多个根节点
            if (actualRoots.Count > 1)
            {
                LogManager.Warning(
                    $"找到 {actualRoots.Count} 个根节点实体",
                    nameof(StoryTreeManager)
                );

                // 选择ID=0的根节点（如果存在）
                var rootWithIdZero = actualRoots.FirstOrDefault(r => r.Id == 0);
                if (rootWithIdZero != null)
                {
                    LogManager.Info($"选择ID=0的根节点作为主要根节点", nameof(StoryTreeManager));

                    // 将其它根节点转换为普通实体
                    foreach (var root in actualRoots)
                    {
                        if (root.Id != 0)
                        {
                            LogManager.Info(
                                $"将实体 ID={root.Id} 从根节点转换为普通实体",
                                nameof(StoryTreeManager)
                            );

                            // 移除Root组件
                            root.RemoveComponent<Comp.Root>();

                            // 如果没有父组件，设置为主要根节点的子节点
                            if (!root.HasComponent<Parent>())
                            {
                                _ecsFramework.SetParent(root, rootWithIdZero);
                            }
                        }
                    }

                    _rootEntity = rootWithIdZero;
                    return rootWithIdZero;
                }

                // 没有ID=0的根节点，选择第一个根节点作为主要根节点
                LogManager.Info(
                    $"选择第一个根节点 (ID={actualRoots[0].Id}) 作为主要根节点",
                    nameof(StoryTreeManager)
                );
                var primaryRoot = actualRoots[0];

                // 将其它根节点转换为普通实体
                for (int i = 1; i < actualRoots.Count; i++)
                {
                    var root = actualRoots[i];
                    LogManager.Info(
                        $"将实体 ID={root.Id} 从根节点转换为普通实体",
                        nameof(StoryTreeManager)
                    );

                    // 移除Root组件
                    root.RemoveComponent<Comp.Root>();

                    // 如果没有父组件，设置为主要根节点的子节点
                    if (!root.HasComponent<Parent>())
                    {
                        _ecsFramework.SetParent(root, primaryRoot);
                    }
                }

                _rootEntity = primaryRoot;
                return primaryRoot;
            }

            // 情况3：只有一个根节点（正常情况）
            LogManager.Info($"找到唯一根节点: ID={actualRoots[0].Id}", nameof(StoryTreeManager));
            _rootEntity = actualRoots[0];
            return actualRoots[0];
        }

        /// <summary>
        /// 创建根节点实体
        /// </summary>
        /// <param name="title"></param>
        /// <param name="overwrite">开启时强制抛弃现有 <see cref="_rootEntity"/> 对象</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Entity CreateRoot(string title, bool overwrite = false)
        {
            if (_rootEntity != null && !overwrite)
            {
                throw new InvalidOperationException($"根节点已存在 (ID : {_rootEntity.Id} )");
            }

            // 检查是否已存在根节点实体
            var existingRoots = FindActualRootEntities(false);

            if (existingRoots.Count > 0 && !overwrite)
            {
                var existingRootIds = string.Join(", ", existingRoots.Select(r => r.Id));
                throw new InvalidOperationException($"已存在根节点实体 (IDs: {existingRootIds})");
            }

            // 如果允许覆盖，先移除现有根节点（包括异常情况下多个 Root 节点）
            if (overwrite)
            {
                if (_rootEntity != null)
                {
                    _ecsFramework.RemoveEntity(_rootEntity.Id);
                    _rootEntity = null;
                }

                foreach (var existingRoot in existingRoots)
                {
                    _ecsFramework.RemoveEntity(existingRoot.Id);
                }
            }

            _rootEntity = new Entity(0);

            _rootEntity.AddComponent(
                new Root
                {
                    RootName = string.IsNullOrEmpty(title)
                        ? "This should not appear in Json"
                        : title,
                }
            );

            _rootEntity.AddComponent(new IdManager(true));

            // 确保根节点没有父组件

            if (_rootEntity.HasComponent<Parent>())
            {
                _rootEntity.RemoveComponent<Parent>();
            }

            _ecsFramework.ForceAddEntity(_rootEntity, true);

            return _rootEntity;
        }
    }
}
