using System;
using System.Collections.Generic;
using System.Linq;
using static ECS.Comp;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public int GetNextOrderNumber(Entity parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            return GetNextOrderNumber(parent.Id);
        }

        /// <summary>
        /// 获取同级实体的下一个序号
        /// </summary>
        public int GetNextOrderNumber(int parentId)
        {
            var children = _ecsFramework.GetChildren(parentId);

            // 如果没有子节点，从1开始
            if (children.Count == 0)
                return 1;

            // 获取所有子节点的最大序号
            int maxOrder = 0;
            foreach (var child in children)
            {
                int order = 0;

                // 优先使用 Order 组件的序号
                if (child.HasComponent<Order>())
                {
                    order = child.GetComponent<Order>().Number;
                }
                // 如果没有 Order 组件，使用 Localization 组件的序号
                else if (child.HasComponent<Localization>())
                {
                    order = child.GetComponent<Localization>().Number;
                }

                if (order > maxOrder)
                {
                    maxOrder = order;
                }
            }

            return maxOrder + 1;
        }

        /// <summary>
        /// 为实体添加 Order 组件
        /// </summary>
        public void AddOrderComponent(Entity entity, int orderNumber, string label = "")
        {
            if (entity.HasComponent<Order>())
            {
                // 如果已有 Order 组件，更新它
                var orderComp = entity.GetComponent<Order>();
                orderComp.Number = orderNumber;
                orderComp.Label = label;
            }
            else
            {
                // 如果没有 Order 组件，添加一个新的
                entity.AddComponent(new Order(orderNumber, label));
            }
        }

        /// <summary>
        /// 获取实体的序号
        /// </summary>
        public int GetEntityOrder(Entity entity)
        {
            // 优先使用 Order 组件的序号
            if (entity.HasComponent<Order>())
            {
                return entity.GetComponent<Order>().Number;
            }

            // 如果没有 Order 组件，使用 Localization 组件的序号
            if (entity.HasComponent<Localization>())
            {
                return entity.GetComponent<Localization>().Number;
            }

            // 如果都没有，返回0
            return 0;
        }

        /// <summary>
        /// 获取同级实体，按序号排序
        /// </summary>
        public List<Entity> GetSiblingsOrdered(int entityId)
        {
            var entity = _ecsFramework.GetEntitySafe(entityId);
            if (entity == null || !entity.HasComponent<Parent>())
                return new List<Entity>();

            var parentComp = entity.GetComponent<Parent>();
            if (!parentComp.ParentId.HasValue)
                return new List<Entity>();

            var siblings = _ecsFramework.GetChildren(parentComp.ParentId.Value);

            // 按序号排序
            return siblings.OrderBy(e => GetEntityOrder(e)).ToList();
        }

        // ... 其他代码 ...
    }
}
