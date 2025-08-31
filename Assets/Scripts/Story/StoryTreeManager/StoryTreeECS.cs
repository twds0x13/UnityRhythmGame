using System.Collections.Generic;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public List<Entity> GetChildren(Entity entity)
        {
            return _ecsFramework.GetChildren(entity);
        }

        public List<Entity> GetChildren(int entityId)
        {
            return _ecsFramework.GetChildren(entityId);
        }
    }
}
