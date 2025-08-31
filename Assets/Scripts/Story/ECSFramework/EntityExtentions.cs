using System.Collections.Generic;
using System.Linq;
using ECS;

namespace ECS
{
    public static class EntityExtensions
    {
        // ��ȡ������ʵ��
        public static IEnumerable<Entity> Children(this Entity entity, ECSFramework ecsManager)
        {
            if (entity == null || !entity.HasComponent<Comp.Children>())
                return Enumerable.Empty<Entity>();

            var childrenComp = entity.GetComponent<Comp.Children>();
            return childrenComp
                .ChildrenIds.Select(id => ecsManager.GetEntity(id))
                .Where(e => e != null);
        }

        // ��ȡ��һ����ʵ��
        public static Entity FirstChild(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).FirstOrDefault();
        }

        // ��ȡ���һ����ʵ��
        public static Entity LastChild(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).LastOrDefault();
        }

        // ����Ż�ȡ��ʵ��
        public static Entity ChildAt(this Entity entity, ECSFramework ecsManager, int index)
        {
            return entity.Children(ecsManager).ElementAtOrDefault(index);
        }

        // ����Ƿ�����ʵ��
        public static bool HasChildren(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).Any();
        }

        // ��ȡ��ʵ������
        public static int ChildCount(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).Count();
        }
    }
}
