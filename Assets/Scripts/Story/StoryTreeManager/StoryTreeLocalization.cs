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
        /// ���������ػ�������
        /// </summary>
        public const string LOCALIZATION_TABLE = "GameStory";

        /// <summary>
        /// ������Ĭ�ϱ��ػ�������
        /// </summary>
        public const string LOCALIZATION_DEFAULT = "DEFAULT";

        // ��ȡ�����½�
        public List<Entity> GetChapters()
        {
            return GetOrCreateRoot()
                .Children()
                .Where(e => e.HasComponent<Comp.Localization>())
                .ToList();
        }

        // ��ȡ�½ڵ�����С��
        public List<Entity> GetEpisodes(Entity chapter)
        {
            if (chapter == null)
                return new List<Entity>();
            return chapter.Children().Where(e => e.HasComponent<Comp.Localization>()).ToList();
        }

        // ��ȡС�ڵ����жԻ���
        public List<Entity> GetLines(Entity episode)
        {
            if (episode == null)
                return new List<Entity>();
            return episode.Children().Where(e => e.HasComponent<Comp.Localization>()).ToList();
        }

        // ���������½�
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

        // ��������С��
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

        // ���������Ի���
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

        // ������Ų���ʵ��
        public Entity FindEntityByNumber(Entity parent, int number)
        {
            return parent.Children().FirstOrDefault(e => GetEntityOrder(e) == number);
        }

        // �����½�
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

        // ����С��
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

        // �����Ի���
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

        // �Զ���Ŵ�������
        public Entity CreateChapter(string title = "") =>
            CreateChapter(GetNextOrderNumber(GetOrCreateRoot()), title);

        public Entity CreateEpisode(Entity chapter, string title = "") =>
            CreateEpisode(chapter, GetNextOrderNumber(chapter), title);

        public Entity CreateLine(Entity episode, string dialogue = "", string speaker = "") =>
            CreateLine(episode, GetNextOrderNumber(episode), dialogue, speaker);

        // ��ȡ���ػ��ı�
        public string GetLocalizedText(Entity entity, bool fallbackToDefault = true)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return "";

            string text = QueryLocalizationTable(loc.ContextKey);
            return !string.IsNullOrEmpty(text)
                ? text
                : (fallbackToDefault ? loc.DefaultText ?? "" : "");
        }

        // ��ȡ��ʽ���Ի�
        public string GetFormattedDialogue(Entity lineEntity)
        {
            if (lineEntity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return "";

            string speaker = GetSpeakerText(lineEntity, false);
            string dialogue = GetLocalizedText(lineEntity);
            return string.IsNullOrEmpty(speaker) ? dialogue : $"{speaker}: {dialogue}";
        }

        // ��ȡ˵�����ı�
        public string GetSpeakerText(Entity entity, bool fallbackToDefault = true)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return "";

            string speaker = QueryLocalizationTable(loc.SpeakerKey);
            return !string.IsNullOrEmpty(speaker)
                ? speaker
                : (fallbackToDefault ? loc.SpeakerKey ?? "" : "");
        }

        // �޸��ı��ػ���ѯ����
        private string QueryLocalizationTable(string key)
        {
            if (string.IsNullOrEmpty(key) || LocalizationSettings.StringDatabase == null)
                return null;

            try
            {
                // ʹ����ȷ�Ĳ�������GetLocalizedString
                return LocalizationSettings.StringDatabase.GetLocalizedString(
                    LOCALIZATION_TABLE,
                    key
                );
            }
            catch (Exception ex)
            {
                LogManager.Warning($"��ѯ���ػ��� '{key}' ʱ����: {ex.Message}");
                return null;
            }
        }

        // ������ȡ�ı�
        public Dictionary<int, string> GetLocalizedTexts(
            List<Entity> entities,
            bool fallbackToDefault = true
        )
        {
            return entities
                .Where(e => e != null)
                .ToDictionary(e => e.Id, e => GetLocalizedText(e, fallbackToDefault));
        }

        // ��ȡʵ���Ӧ�� Localization Entry
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

        // ��ȡʵ��� SpeakerKey
        public string GetSpeakerKey(Entity entity)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return null;

            return loc.SpeakerKey;
        }

        // ���ݼ���ȡ Localization Entry
        public StringTableEntry GetLocalizationEntry(string key)
        {
            if (string.IsNullOrEmpty(key) || LocalizationSettings.StringDatabase == null)
                return null;

            try
            {
                // ��ȡ GameStory ��� Entry
                return LocalizationSettings
                    .StringDatabase.GetTableEntry(LOCALIZATION_TABLE, key)
                    .Entry;
            }
            catch (Exception ex)
            {
                LogManager.Warning($"��ȡ���ػ���Ŀ '{key}' ʱ����: {ex.Message}");
                return null;
            }
        }

        // ��ȡ˵���߶�Ӧ�� Localization Entry
        public StringTableEntry GetSpeakerEntry(Entity entity)
        {
            if (entity?.GetComponent<Comp.Localization>() is not Comp.Localization loc)
                return null;

            if (string.IsNullOrEmpty(loc.SpeakerKey))
                return null;

            return GetLocalizationEntry(loc.SpeakerKey);
        }

        // ��鱾�ػ���Ŀ�Ƿ����
        public bool HasLocalizationEntry(string key) =>
            !string.IsNullOrEmpty(QueryLocalizationTable(key));
    }
}
