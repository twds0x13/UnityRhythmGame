using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// ��ȡ���õ�Ŀ��ʵ���б��ų������ʵ�ʵ�壩
        /// </summary>
        public List<Entity> GetAvailableTargetEntities(Entity currentEntity)
        {
            if (currentEntity == null)
                return new List<Entity>();

            var allEntities = _ecsFramework.GetAllEntities();

            return allEntities
                .Where(e => e.Id != currentEntity.Id) // �ų�����
                .Where(e => !_ecsFramework.IsDescendantOf(e, currentEntity)) // �ų�����ڵ�
                .Where(e => e.HasComponent<Comp.Localization>()) // �����б��ػ����
                .OrderBy(e => _ecsFramework.GetEntityPath(e)) // ��·������
                .ToList();
        }
    }
}
