using System;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public static readonly string RootName = "Root";

        private bool _developerMode = true;

        private Entity _rootEntity;

        private readonly ECSFramework _ecsFramework;

        private static StoryTreeManager _instance;

        public static StoryTreeManager Inst
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StoryTreeManager(ECSFramework.Inst);
                }
                return _instance;
            }
        }

        private StoryTreeManager(ECSFramework ecsManager)
        {
            _ecsFramework = ecsManager ?? throw new ArgumentNullException(nameof(ecsManager));
        }
    }
}
