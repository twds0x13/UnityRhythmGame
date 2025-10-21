using System;
using System.Collections.Generic;
using System.Linq;
using static ECS.Comp;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// ��ȡָ��ʵ��������ֵܽڵ㣨��Order����
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
        /// ��ȡʵ���Orderֵ
        /// </summary>
        public int GetEntityOrder(Entity entity)
        {
            if (entity.HasComponent<Comp.Order>())
            {
                return entity.GetComponent<Comp.Order>().Number;
            }
            return entity.Id; // ���û��Order�����ʹ��ID��Ϊ��
        }

        public int GetNextOrderNumber(Entity parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            return GetNextOrderNumber(parent.Id);
        }

        /// <summary>
        /// ��Order˳���ȡ��һ���ӽڵ�
        /// </summary>
        private Entity GetFirstChildByOrder(Entity parent)
        {
            if (parent?.HasComponent<Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Children>();
            if (childrenComp.ChildrenEntities.Count == 0)
                return null;

            // ��Order���򲢷��ص�һ��
            return childrenComp
                .ChildrenEntities.Where(child => child.HasComponent<Order>())
                .OrderBy(child => child.GetComponent<Order>().Number)
                .FirstOrDefault();
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
    }
}
