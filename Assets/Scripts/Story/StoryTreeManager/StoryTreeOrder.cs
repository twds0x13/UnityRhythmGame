using System;
using System.Collections.Generic;
using System.Linq;
using static ECS.Comp;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// 获取指定实体的所有兄弟节点（按Order排序）
        /// </summary>
        public List<Entity> GetSiblingsOrdered(int entityId)
        {
            var entity = _ecsFramework.GetEntitySafe(entityId);
            if (entity == null)
                return new List<Entity>();

            var parent = entity.Parent();
            if (parent == null || !parent.HasComponent<Comp.Children>())
                return new List<Entity>();

            var siblings = parent
                .GetComponent<Comp.Children>()
                .ChildrenEntities.OrderBy(sibling => GetEntityOrder(sibling))
                .ToList();

            return siblings;
        }

        /// <summary>
        /// 获取实体的Order值
        /// </summary>
        public int GetEntityOrder(Entity entity)
        {
            if (entity.HasComponent<Comp.Order>())
            {
                return entity.GetComponent<Comp.Order>().Number;
            }
            return entity.Id; // 如果没有Order组件，使用ID作为后备
        }

        public int GetNextOrderNumber(Entity parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            return GetNextOrderNumber(parent.Id);
        }

        /// <summary>
        /// 按Order顺序获取第一个子节点
        /// </summary>
        private Entity GetFirstChildByOrder(Entity parent)
        {
            if (parent?.HasComponent<Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Children>();
            if (childrenComp.ChildrenEntities.Count == 0)
                return null;

            // 按Order排序并返回第一个
            return childrenComp
                .ChildrenEntities.Where(child => child.HasComponent<Order>())
                .OrderBy(child => child.GetComponent<Order>().Number)
                .FirstOrDefault();
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
    }
}
