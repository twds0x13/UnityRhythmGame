using System;
using System.Linq;

namespace ECS
{
    public partial class StoryTreeManager
    {
        // 更新 ID 管理器以反映当前状态
        private void UpdateIdManager()
        {
            if (_rootEntity == null || !_rootEntity.HasComponent<Comp.IdManager>())
                return;

            var idManager = _rootEntity.GetComponent<Comp.IdManager>();
            var allEntities = _ecsFramework.GetAllEntities();

            // 重置ID管理器
            idManager.Reset();

            // 注册所有现有ID
            foreach (var entity in allEntities)
            {
                try
                {
                    idManager.RegisterId(entity.Id);
                }
                catch (ArgumentException ex)
                {
                    LogFile.Warning(
                        $"注册ID时发生冲突 (ID: {entity.Id}): {ex.Message}",
                        "StoryTreeManager"
                    );
                }
            }

            LogFile.Log($"ID管理器已更新，已注册 {allEntities.Count()} 个实体", "StoryTreeManager");
        }
    }
}
