namespace ECS
{
    public partial class StoryTreeManager
    {
        public void SwapComponents(Entity entity1, Entity entity2)
        {
            ECSFramework.Inst.SwapComponents(entity1, entity2);
        }

        public void SwapEntities(Entity entity1, Entity entity2)
        {
            ECSFramework.Inst.SwapEntities(entity1, entity2);
        }
    }
}
