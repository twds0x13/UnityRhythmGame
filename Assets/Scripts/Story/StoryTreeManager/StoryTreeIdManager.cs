using System;
using System.Linq;

namespace ECS
{
    public partial class StoryTreeManager
    {
        // ���� ID �������Է�ӳ��ǰ״̬
        private void UpdateIdManager()
        {
            if (_rootEntity == null || !_rootEntity.HasComponent<Comp.IdManager>())
                return;

            var idManager = _rootEntity.GetComponent<Comp.IdManager>();
            var allEntities = _ecsFramework.GetAllEntities();

            // ����ID������
            idManager.Reset();

            // ע����������ID
            foreach (var entity in allEntities)
            {
                try
                {
                    idManager.RegisterId(entity.Id);
                }
                catch (ArgumentException ex)
                {
                    LogFile.Warning(
                        $"ע��IDʱ������ͻ (ID: {entity.Id}): {ex.Message}",
                        "StoryTreeManager"
                    );
                }
            }

            LogFile.Log($"ID�������Ѹ��£���ע�� {allEntities.Count()} ��ʵ��", "StoryTreeManager");
        }
    }
}
