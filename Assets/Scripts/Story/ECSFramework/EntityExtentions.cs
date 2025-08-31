using System.Collections.Generic;
using System.Linq;
using ECS;

namespace ECS
{
    public static class EntityExtensions
    {
        // 获取所有子实体
        public static IEnumerable<Entity> Children(this Entity entity, ECSFramework ecsManager)
        {
            if (entity == null || !entity.HasComponent<Comp.Children>())
                return Enumerable.Empty<Entity>();

            var childrenComp = entity.GetComponent<Comp.Children>();
            return childrenComp
                .ChildrenIds.Select(id => ecsManager.GetEntity(id))
                .Where(e => e != null);
        }

        // 获取第一个子实体
        public static Entity FirstChild(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).FirstOrDefault();
        }

        // 获取最后一个子实体
        public static Entity LastChild(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).LastOrDefault();
        }

        // 按序号获取子实体
        public static Entity ChildAt(this Entity entity, ECSFramework ecsManager, int index)
        {
            return entity.Children(ecsManager).ElementAtOrDefault(index);
        }

        // 检查是否有子实体
        public static bool HasChildren(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).Any();
        }

        // 获取子实体数量
        public static int ChildCount(this Entity entity, ECSFramework ecsManager)
        {
            return entity.Children(ecsManager).Count();
        }
    }
}
