using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// 常量：本地化表名称
        /// </summary>
        public const string LOCALIZATION_TABLE = "GameStory";

        /// <summary>
        /// 常量：默认本地化上下文
        /// </summary>
        public const string LOCALIZATION_DEFAULT = "DEFAULT";

        // 获取所有章节
        public List<Entity> GetChapters()
        {
            return GetOrCreateRoot()
                .Children()
                .Where(e => e.HasComponent<Comp.Localization>())
                .ToList();
        }

        // 获取章节的所有小节
        public List<Entity> GetEpisodes(Entity chapter)
        {
            if (chapter == null)
                return new List<Entity>();
            return chapter.Children().Where(e => e.HasComponent<Comp.Localization>()).ToList();
        }

        // 获取小节的所有对话行
        public List<Entity> GetLines(Entity episode)
        {
            if (episode == null)
                return new List<Entity>();
            return episode.Children().Where(e => e.HasComponent<Comp.Localization>()).ToList();
        }

        // 批量创建章节
        public List<Entity> CreateChapters(params string[] titles)
        {
            var root = GetOrCreateRoot();
            var chapters = new List<Entity>();
            int startOrder = GetNextOrderNumber(root);

            for (int i = 0; i < titles.Length; i++)
            {
                chapters.Add(CreateChapter(startOrder + i, titles[i]));
            }
            return chapters;
        }

        // 批量创建小节
        public List<Entity> CreateEpisodes(Entity chapter, params string[] titles)
        {
            var episodes = new List<Entity>();
            int startOrder = GetNextOrderNumber(chapter);

            for (int i = 0; i < titles.Length; i++)
            {
                episodes.Add(CreateEpisode(chapter, startOrder + i, titles[i]));
            }
            return episodes;
        }

        // 批量创建对话行
        public List<Entity> CreateLines(Entity episode, params string[] dialogues)
        {
            var lines = new List<Entity>();
            int startOrder = GetNextOrderNumber(episode);

            for (int i = 0; i < dialogues.Length; i++)
            {
                lines.Add(CreateLine(episode, startOrder + i, dialogues[i]));
            }
            return lines;
        }

        // 根据序号查找实体
        public Entity FindEntityByNumber(Entity parent, int number)
        {
            return parent.Children().FirstOrDefault(e => GetEntityOrder(e) == number);
        }

        // 创建章节
        public Entity CreateChapter(int number = 0, string title = null, bool overwrite = false)
        {
            var existing = FindEntityByNumber(GetOrCreateRoot(), number);
            if (existing != null && !overwrite)
                return existing;
            if (existing != null && overwrite)
                _ecsFramework.RemoveEntity(existing.Id);

            var chapter = _ecsFramework.CreateEntity();
            chapter.AddComponent(Comp.Localization.CreateChapter(number, title));
            chapter.AddComponent(Comp.Order.CreateChapterOrder(number));
            _ecsFramework.SetParent(chapter, GetOrCreateRoot());
            return chapter;
        }

        // 创建小节
        public Entity CreateEpisode(
            Entity chapter,
            int number = 0,
            string title = null,
            bool overwrite = false
        )
        {
            var existing = FindEntityByNumber(chapter, number);
            if (existing != null && !overwrite)
                return existing;
            if (existing != null && overwrite)
                _ecsFramework.RemoveEntity(existing.Id);

            var episode = _ecsFramework.CreateEntity();
            episode.AddComponent(Comp.Localization.CreateEpisode(number, title));
            episode.AddComponent(Comp.Order.CreateEpisodeOrder(number));
            _ecsFramework.SetParent(episode, chapter);
            return episode;
        }

        // 创建对话行
        public Entity CreateLine(
            Entity episode,
            int number = 0,
            string dialogue = null,
            string speaker = null,
            bool overwrite = false
        )
        {
            var existing = FindEntityByNumber(episode, number);
            if (existing != null && !overwrite)
                return existing;
            if (existing != null && overwrite)
                _ecsFramework.RemoveEntity(existing.Id);

            var line = _ecsFramework.CreateEntity();
            line.AddComponent(Comp.Localization.CreateLine(number, dialogue, speaker));
            line.AddComponent(Comp.Order.CreateLineOrder(number));
            _ecsFramework.SetParent(line, episode);
            return line;
        }

        // 自动序号创建方法
        public Entity CreateChapter(string title = "") =>
            CreateChapter(GetNextOrderNumber(GetOrCreateRoot()), title);

        public Entity CreateEpisode(Entity chapter, string title = "") =>
            CreateEpisode(chapter, GetNextOrderNumber(chapter), title);

        public Entity CreateLine(Entity episode, string dialogue = "", string speaker = "") =>
            CreateLine(episode, GetNextOrderNumber(episode), dialogue, speaker);

        // 获取本地化文本
        public string GetLocalizedText(Entity entity, bool fallbackToDefault = true)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return "";

            string text = QueryLocalizationTable(loc.ContextKey);
            return !string.IsNullOrEmpty(text)
                ? text
                : (fallbackToDefault ? loc.DefaultText ?? "" : "");
        }

        // 获取格式化对话
        public string GetFormattedDialogue(Entity lineEntity)
        {
            if (lineEntity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return "";

            string speaker = GetSpeakerText(lineEntity, false);
            string dialogue = GetLocalizedText(lineEntity);
            return string.IsNullOrEmpty(speaker) ? dialogue : $"{speaker}: {dialogue}";
        }

        // 获取说话者文本
        public string GetSpeakerText(Entity entity, bool fallbackToDefault = true)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return "";

            string speaker = QueryLocalizationTable(loc.SpeakerKey);
            return !string.IsNullOrEmpty(speaker)
                ? speaker
                : (fallbackToDefault ? loc.SpeakerKey ?? "" : "");
        }

        // 修复的本地化查询方法
        private string QueryLocalizationTable(string key)
        {
            if (string.IsNullOrEmpty(key) || LocalizationSettings.StringDatabase == null)
                return null;

            try
            {
                // 使用正确的参数调用GetLocalizedString
                return LocalizationSettings.StringDatabase.GetLocalizedString(
                    LOCALIZATION_TABLE,
                    key
                );
            }
            catch (Exception ex)
            {
                LogManager.Warning($"查询本地化键 '{key}' 时出错: {ex.Message}");
                return null;
            }
        }

        // 批量获取文本
        public Dictionary<int, string> GetLocalizedTexts(
            List<Entity> entities,
            bool fallbackToDefault = true
        )
        {
            return entities
                .Where(e => e != null)
                .ToDictionary(e => e.Id, e => GetLocalizedText(e, fallbackToDefault));
        }

        // 获取实体对应的 Localization Entry
        public StringTableEntry GetLocalizationEntry(Entity entity)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return null;

            return GetLocalizationEntry(loc.ContextKey);
        }

        public string GetContextKey(Entity entity)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return null;

            return loc.ContextKey;
        }

        // 获取实体的 SpeakerKey
        public string GetSpeakerKey(Entity entity)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return null;

            return loc.SpeakerKey;
        }

        // 根据键获取 Localization Entry
        public StringTableEntry GetLocalizationEntry(string key)
        {
            if (string.IsNullOrEmpty(key) || LocalizationSettings.StringDatabase == null)
                return null;

            try
            {
                // 获取 GameStory 表的 Entry
                return LocalizationSettings
                    .StringDatabase.GetTableEntry(LOCALIZATION_TABLE, key)
                    .Entry;
            }
            catch (Exception ex)
            {
                LogManager.Warning($"获取本地化条目 '{key}' 时出错: {ex.Message}");
                return null;
            }
        }

        // 获取说话者对应的 Localization Entry
        public StringTableEntry GetSpeakerEntry(Entity entity)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return null;

            if (string.IsNullOrEmpty(loc.SpeakerKey))
                return null;

            return GetLocalizationEntry(loc.SpeakerKey);
        }

        // 检查本地化条目是否存在
        public bool HasLocalizationEntry(string key) =>
            !string.IsNullOrEmpty(QueryLocalizationTable(key));
    }
}
