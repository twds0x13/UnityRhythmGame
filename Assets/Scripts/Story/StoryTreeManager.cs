using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    public class StoryTreeManager
    {
        private bool _isDeveloperMode = true;

        private Entity _rootEntity;

        private readonly ECSManager _ecsManager;

        public StoryTreeManager(ECSManager ecsManager)
        {
            _ecsManager = ecsManager ?? throw new ArgumentNullException(nameof(ecsManager));
        }

        public void SetDeveloperMode(bool enabled)
        {
            _isDeveloperMode = enabled;
        }

        // 获取或创建根节点（不依赖 _entities）
        public Entity GetOrCreateRoot()
        {
            if (_rootEntity != null)
                return _rootEntity;

            // 使用公共方法查找根节点
            _rootEntity = _ecsManager.FindRootEntity();

            if (_rootEntity != null)
            {
                // 确保根节点有 Localization 组件
                if (!_rootEntity.HasComponent<Comp.Root>())
                {
                    _rootEntity.AddComponent(Comp.Localization.CreateChapter(0, "Apple"));
                }
            }

            // 创建新的根节点
            _rootEntity = _ecsManager.CreateEntity();
            _rootEntity.AddComponent(Comp.Localization.CreateChapter(0, "Android"));
            return _rootEntity;
        }

        // 创建章节节点
        public Entity CreateChapter(int number = 0, string title = "")
        {
            var chapter = _ecsManager.CreateEntity();
            var locComp = Comp.Localization.CreateChapter(number, title);
            chapter.AddComponent(locComp);

            _ecsManager.SetParent(chapter, GetOrCreateRoot());

            locComp.GenerateLocalizationKey(chapter, _ecsManager);

            return chapter;
        }

        // 创建小节节点
        public Entity CreateEpisode(Entity chapter, int number = 0, string title = "")
        {
            if (chapter == null || !chapter.HasComponent<Comp.Localization>())
                throw new ArgumentException("必须提供有效的章节实体");

            var episode = _ecsManager.CreateEntity();
            var locComp = Comp.Localization.CreateEpisode(number, title);
            episode.AddComponent(locComp);

            _ecsManager.SetParent(episode, chapter);

            // 仅在开发者模式下生成本地化键
            if (_isDeveloperMode)
            {
                locComp.GenerateLocalizationKey(episode, _ecsManager);
            }

            return episode;
        }

        // 创建对话行节点
        public Entity CreateLine(
            Entity episode,
            int number = 0,
            string dialogue = "",
            string speaker = ""
        )
        {
            if (episode == null || !episode.HasComponent<Comp.Localization>())
                throw new ArgumentException("必须提供有效的小节实体");

            var line = _ecsManager.CreateEntity();
            var locComp = Comp.Localization.CreateLine(number, dialogue, speaker);
            line.AddComponent(locComp);

            _ecsManager.SetParent(line, episode);

            // 仅在开发者模式下生成本地化键
            if (_isDeveloperMode)
            {
                locComp.GenerateLocalizationKey(line, _ecsManager);
            }

            return line;
        }
    }
}
