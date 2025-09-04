using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    public static class EntityExtensions
    {
        // �� Linq ���޵���

        // ����ӵ���ض������ʵ��
        public static IEnumerable<Entity> WithComponent<T>(this IEnumerable<Entity> source)
            where T : IComponent
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (var entity in source)
            {
                if (entity.HasComponent<T>())
                {
                    yield return entity;
                }
            }
        }

        // ����ӵ���ض����������������ʵ��
        public static IEnumerable<Entity> WithComponent<T>(
            this IEnumerable<Entity> source,
            Func<T, bool> predicate
        )
            where T : IComponent
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var entity in source)
            {
                var component = entity.GetComponent<T>();
                if (component != null && predicate(component))
                {
                    yield return entity;
                }
            }
        }

        // ��ȡ����ʵ����ض����
        public static IEnumerable<T> SelectComponent<T>(this IEnumerable<Entity> source)
            where T : IComponent
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (var entity in source)
            {
                var component = entity.GetComponent<T>();
                if (component != null)
                {
                    yield return component;
                }
            }
        }

        public static Entity Parent(this Entity entity)
        {
            if (entity == null || !entity.HasComponent<Comp.Parent>())
                return null;

            var parentComp = entity.GetComponent<Comp.Parent>();

            return ECSFramework.Inst.GetEntitySafe(parentComp.ParentId);
        }

        // �����ʵ��
        public static void AddChild(this Entity parent, Entity child)
        {
            if (parent == null || child == null)
                return;

            if (!parent.HasComponent<Comp.Children>())
            {
                parent.AddComponent(new Comp.Children());
            }

            var childrenComp = parent.GetComponent<Comp.Children>();
            if (!childrenComp.ChildrenIds.Contains(child.Id))
            {
                childrenComp.ChildrenIds.Add(child.Id);
            }
        }

        // ��ȡ������ʵ��
        public static IEnumerable<Entity> Children(this Entity entity)
        {
            if (entity == null || !entity.HasComponent<Comp.Children>())
                return Enumerable.Empty<Entity>();

            var childrenComp = entity.GetComponent<Comp.Children>();
            return childrenComp
                .ChildrenIds.Select(id => ECSFramework.Inst.GetEntitySafe(id))
                .Where(e => e != null);
        }

        // ��ȡ��һ����ʵ��
        public static Entity FirstChild(this Entity entity)
        {
            return entity.Children().FirstOrDefault();
        }

        // ��ȡ���һ����ʵ��
        public static Entity LastChild(this Entity entity)
        {
            return entity.Children().LastOrDefault();
        }

        // ����Ż�ȡ��ʵ��
        public static Entity ChildAt(this Entity entity, int index)
        {
            return entity.Children().ElementAtOrDefault(index);
        }

        // ����Ƿ�����ʵ��
        public static bool HasChildren(this Entity entity)
        {
            return entity.Children().Any();
        }

        // ��ȡ��ʵ������
        public static int ChildCount(this Entity entity)
        {
            return entity.Children().Count();
        }
    }
}
