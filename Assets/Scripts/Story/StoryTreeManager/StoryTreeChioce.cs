using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// 获取可用的目标实体列表（排除不合适的实体）
        /// </summary>
        public List<Entity> GetAvailableTargetEntities(Entity currentEntity)
        {
            if (currentEntity == null)
                return new List<Entity>();

            var allEntities = _ecsFramework.GetAllEntities();

            return allEntities
                .Where(e => e.Id != currentEntity.Id) // 排除自身
                .Where(e => !_ecsFramework.IsDescendantOf(e, currentEntity)) // 排除后代节点
                .Where(e => e.HasComponent<Comp.Localization>()) // 必须有本地化组件
                .OrderBy(e => _ecsFramework.GetEntityPath(e)) // 按路径排序
                .ToList();
        }
    }
}
