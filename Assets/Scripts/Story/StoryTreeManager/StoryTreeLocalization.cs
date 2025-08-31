using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ECS
{
    public partial class StoryTreeManager
    {
        // 获取所有章节
        public List<Entity> GetChapters()
        {
            var root = GetOrCreateRoot();
            return _ecsFramework
                .GetChildren(root)
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type
                        == Comp.Localization.NodeType.Chapter
                )
                .ToList();
        }

        // 获取该章节的所有小节
        public List<Entity> GetEpisodes(Entity chapter)
        {
            if (chapter == null || !chapter.HasComponent<Comp.Localization>())
                return new List<Entity>();

            return _ecsFramework
                .GetChildren(chapter)
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type
                        == Comp.Localization.NodeType.Episode
                )
                .ToList();
        }

        // 获取该小节的所有对话行
        public List<Entity> GetLines(Entity episode)
        {
            if (episode == null || !episode.HasComponent<Comp.Localization>())
                return new List<Entity>();

            return _ecsFramework
                .GetChildren(episode)
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type == Comp.Localization.NodeType.Line
                )
                .ToList();
        }

        // 批量创建章节
        public List<Entity> CreateChapters(params string[] titles)
        {
            var chapters = new List<Entity>();

            for (int i = 0; i < titles.Length; i++)
            {
                var chapter = CreateChapter(i + 1, titles[i]);
                chapters.Add(chapter);
            }

            return chapters;
        }

        // 批量创建小节
        public List<Entity> CreateEpisodes(Entity chapter, params string[] titles)
        {
            if (chapter == null)
                throw new ArgumentException("必须提供有效的章节实体");

            var episodes = new List<Entity>();

            for (int i = 0; i < titles.Length; i++)
            {
                var episode = CreateEpisode(chapter, i + 1, titles[i]);
                episodes.Add(episode);
            }

            return episodes;
        }

        // 批量创建对话行
        public List<Entity> CreateLines(Entity episode, params string[] dialogues)
        {
            if (episode == null)
                throw new ArgumentException("必须提供有效的小节实体");

            var lines = new List<Entity>();

            for (int i = 0; i < dialogues.Length; i++)
            {
                var line = CreateLine(episode, i + 1, dialogues[i]);
                lines.Add(line);
            }

            return lines;
        }

        // 根据序号查找章节
        public Entity FindChapterByNumber(int number)
        {
            var root = GetOrCreateRoot();
            var chapters = GetChildren(root.Id);

            foreach (var chapter in chapters)
            {
                int chapterNumber = GetEntityOrder(chapter);
                if (chapterNumber == number)
                {
                    return chapter;
                }
            }

            return null;
        }

        // 根据序号查找小节
        public Entity FindEpisodeByNumber(Entity chapter, int number)
        {
            if (chapter == null)
                return null;

            var episodes = GetChildren(chapter.Id);

            foreach (var episode in episodes)
            {
                int episodeNumber = GetEntityOrder(episode);
                if (episodeNumber == number)
                {
                    return episode;
                }
            }

            return null;
        }

        // 根据序号查找对白行
        public Entity FindLineByNumber(Entity episode, int number)
        {
            if (episode == null)
                return null;

            var lines = GetChildren(episode.Id);

            foreach (var line in lines)
            {
                int lineNumber = GetEntityOrder(line);
                if (lineNumber == number)
                {
                    return line;
                }
            }

            return null;
        }

        // 创建章节节点（自动分配序号）
        public Entity CreateChapter(string title = "")
        {
            var root = GetOrCreateRoot();
            int nextNumber = GetNextOrderNumber(root.Id);
            return CreateChapter(nextNumber, title);
        }

        // 创建章节节点（添加重复检查和覆盖功能）
        public Entity CreateChapter(int number = 0, string title = "", bool overwrite = false)
        {
            // 检查是否已存在相同序号的章节
            var existingChapter = FindChapterByNumber(number);
            if (existingChapter != null && !overwrite)
            {
                Debug.Log($"已存在序号为 {number} 的章节，跳过创建");
                return existingChapter;
            }

            // 如果存在且允许覆盖，先删除现有章节
            if (existingChapter != null && overwrite)
            {
                Debug.Log($"覆盖序号为 {number} 的现有章节");
                _ecsFramework.RemoveEntity(existingChapter.Id);
            }

            var chapter = _ecsFramework.CreateEntity();

            // 添加 Localization 组件
            var locComp = Comp.Localization.CreateChapter(number, title);
            chapter.AddComponent(locComp);

            // 添加 Order 组件
            var orderComp = Comp.Order.CreateChapterOrder(number);
            chapter.AddComponent(orderComp);

            _ecsFramework.SetParent(chapter, GetOrCreateRoot());

            // 仅在开发者模式下生成本地化键
            if (_developerMode)
            {
                locComp.GenerateLocalizationKey(chapter, _ecsFramework);
            }

            return chapter;
        }

        // 创建小节节点（添加重复检查和覆盖功能）
        public Entity CreateEpisode(
            Entity chapter,
            int number = 0,
            string title = "",
            bool overwrite = false
        )
        {
            if (chapter == null)
                throw new ArgumentException("必须提供有效的章节实体");

            // 检查章节是否有 Localization 组件
            if (!chapter.HasComponent<Comp.Localization>())
                throw new ArgumentException("章节实体未拥有本地化标签");

            // 检查是否已存在相同序号的小节
            var existingEpisode = FindEpisodeByNumber(chapter, number);
            if (existingEpisode != null && !overwrite)
            {
                Debug.Log($"在章节 {chapter.Id} 中已存在序号为 {number} 的小节，跳过创建");
                return existingEpisode;
            }

            // 如果存在且允许覆盖，先删除现有小节
            if (existingEpisode != null && overwrite)
            {
                Debug.Log($"覆盖章节 {chapter.Id} 中序号为 {number} 的现有小节");
                _ecsFramework.RemoveEntity(existingEpisode.Id);
            }

            var episode = _ecsFramework.CreateEntity();

            // 添加 Localization 组件
            var locComp = Comp.Localization.CreateEpisode(number, title);
            episode.AddComponent(locComp);

            // 添加 Order 组件
            var orderComp = Comp.Order.CreateEpisodeOrder(number);
            episode.AddComponent(orderComp);

            _ecsFramework.SetParent(episode, chapter);

            // 仅在开发者模式下生成本地化键
            if (_developerMode)
            {
                locComp.GenerateLocalizationKey(episode, _ecsFramework);
            }

            return episode;
        }

        // 创建对话行节点（添加重复检查和覆盖功能）
        public Entity CreateLine(
            Entity episode,
            int number = 0,
            string dialogue = "",
            string speaker = "",
            bool overwrite = false
        )
        {
            if (episode == null)
                throw new ArgumentException("必须提供有效的小节实体");

            if (!episode.HasComponent<Comp.Localization>())
                throw new ArgumentException("小节实体未拥有本地化标签");

            // 检查是否已存在相同序号的对白行
            var existingLine = FindLineByNumber(episode, number);
            if (existingLine != null && !overwrite)
            {
                Debug.Log($"在小节 {episode.Id} 中已存在序号为 {number} 的对白行，跳过创建");
                return existingLine;
            }

            // 如果存在且允许覆盖，先删除现有对白行
            if (existingLine != null && overwrite)
            {
                Debug.Log($"覆盖小节 {episode.Id} 中序号为 {number} 的现有对白行");
                _ecsFramework.RemoveEntity(existingLine.Id);
            }

            var line = _ecsFramework.CreateEntity();
            var locComp = Comp.Localization.CreateLine(number, dialogue, speaker);
            line.AddComponent(locComp);

            // 添加 Order 组件
            var orderComp = Comp.Order.CreateLineOrder(number);
            line.AddComponent(orderComp);

            _ecsFramework.SetParent(line, episode);

            // 仅在开发者模式下生成本地化键
            if (_developerMode)
            {
                locComp.GenerateLocalizationKey(line, _ecsFramework);
            }

            return line;
        }

        // 创建小节节点（自动分配序号）
        public Entity CreateEpisode(Entity chapter, string title = "")
        {
            if (chapter == null)
                throw new ArgumentException("必须提供有效的章节实体");

            int nextNumber = GetNextOrderNumber(chapter.Id);
            return CreateEpisode(chapter, nextNumber, title);
        }

        // 创建对话行节点（自动分配序号）
        public Entity CreateLine(Entity episode, string dialogue = "", string speaker = "")
        {
            if (episode == null)
                throw new ArgumentException("必须提供有效的小节实体");

            int nextNumber = GetNextOrderNumber(episode.Id);
            return CreateLine(episode, nextNumber, dialogue, speaker);
        }

        // 获取节点的本地化键
        public string GetLocalizationKey(Entity entity)
        {
            if (entity == null || !entity.HasComponent<Comp.Localization>())
                return "";

            var locComp = entity.GetComponent<Comp.Localization>();
            return locComp.ContextKey;
        }

        // 更新节点后重新生成本地化键（仅在开发者模式下）
        public void UpdateLocalizationKey(Entity entity)
        {
            if (!_developerMode)
                return;
            if (entity == null || !entity.HasComponent<Comp.Localization>())
                return;

            var locComp = entity.GetComponent<Comp.Localization>();
            locComp.GenerateLocalizationKey(entity, _ecsFramework);
        }

        // 更新所有本地化键
        public void UpdateAllLocalizationKeys(Action<Entity, Comp.Localization> updateAction = null)
        {
            foreach (var entity in _ecsFramework.GetAllEntities())
            {
                if (entity.HasComponent<Comp.Localization>())
                {
                    var locComp = entity.GetComponent<Comp.Localization>();
                    updateAction?.Invoke(entity, locComp);
                }
            }
        }
    }
}
