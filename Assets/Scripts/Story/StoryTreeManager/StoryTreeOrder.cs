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
        /// ��ȡͬ��ʵ�����һ�����
        /// </summary>
        public int GetNextOrderNumber(int parentId)
        {
            var children = _ecsFramework.GetChildren(parentId);

            // ���û���ӽڵ㣬��1��ʼ
            if (children.Count == 0)
                return 1;

            // ��ȡ�����ӽڵ��������
            int maxOrder = 0;
            foreach (var child in children)
            {
                int order = 0;

                // ����ʹ�� Order ��������
                if (child.HasComponent<Order>())
                {
                    order = child.GetComponent<Order>().Number;
                }
                // ���û�� Order �����ʹ�� Localization ��������
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
        /// Ϊʵ����� Order ���
        /// </summary>
        public void AddOrderComponent(Entity entity, int orderNumber, string label = "")
        {
            if (entity.HasComponent<Order>())
            {
                // ������� Order �����������
                var orderComp = entity.GetComponent<Order>();
                orderComp.Number = orderNumber;
                orderComp.Label = label;
            }
            else
            {
                // ���û�� Order ��������һ���µ�
                entity.AddComponent(new Order(orderNumber, label));
            }
        }

        /// <summary>
        /// ��ȡʵ������
        /// </summary>
        public int GetEntityOrder(Entity entity)
        {
            // ����ʹ�� Order ��������
            if (entity.HasComponent<Order>())
            {
                return entity.GetComponent<Order>().Number;
            }

            // ���û�� Order �����ʹ�� Localization ��������
            if (entity.HasComponent<Localization>())
            {
                return entity.GetComponent<Localization>().Number;
            }

            // �����û�У�����0
            return 0;
        }

        /// <summary>
        /// ��ȡͬ��ʵ�壬���������
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

            // ���������
            return siblings.OrderBy(e => GetEntityOrder(e)).ToList();
        }

        // ... �������� ...
    }
}
