using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ECS
{
    public partial class StoryTreeManager
    {
        // ��ȡ�����½�
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

        // ��ȡ���½ڵ�����С��
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

        // ��ȡ��С�ڵ����жԻ���
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

        // ���������½�
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

        // ��������С��
        public List<Entity> CreateEpisodes(Entity chapter, params string[] titles)
        {
            if (chapter == null)
                throw new ArgumentException("�����ṩ��Ч���½�ʵ��");

            var episodes = new List<Entity>();

            for (int i = 0; i < titles.Length; i++)
            {
                var episode = CreateEpisode(chapter, i + 1, titles[i]);
                episodes.Add(episode);
            }

            return episodes;
        }

        // ���������Ի���
        public List<Entity> CreateLines(Entity episode, params string[] dialogues)
        {
            if (episode == null)
                throw new ArgumentException("�����ṩ��Ч��С��ʵ��");

            var lines = new List<Entity>();

            for (int i = 0; i < dialogues.Length; i++)
            {
                var line = CreateLine(episode, i + 1, dialogues[i]);
                lines.Add(line);
            }

            return lines;
        }

        // ������Ų����½�
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

        // ������Ų���С��
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

        // ������Ų��Ҷ԰���
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

        // �����½ڽڵ㣨�Զ�������ţ�
        public Entity CreateChapter(string title = "")
        {
            var root = GetOrCreateRoot();
            int nextNumber = GetNextOrderNumber(root.Id);
            return CreateChapter(nextNumber, title);
        }

        // �����½ڽڵ㣨����ظ����͸��ǹ��ܣ�
        public Entity CreateChapter(int number = 0, string title = "", bool overwrite = false)
        {
            // ����Ƿ��Ѵ�����ͬ��ŵ��½�
            var existingChapter = FindChapterByNumber(number);
            if (existingChapter != null && !overwrite)
            {
                Debug.Log($"�Ѵ������Ϊ {number} ���½ڣ���������");
                return existingChapter;
            }

            // ��������������ǣ���ɾ�������½�
            if (existingChapter != null && overwrite)
            {
                Debug.Log($"�������Ϊ {number} �������½�");
                _ecsFramework.RemoveEntity(existingChapter.Id);
            }

            var chapter = _ecsFramework.CreateEntity();

            // ��� Localization ���
            var locComp = Comp.Localization.CreateChapter(number, title);
            chapter.AddComponent(locComp);

            // ��� Order ���
            var orderComp = Comp.Order.CreateChapterOrder(number);
            chapter.AddComponent(orderComp);

            _ecsFramework.SetParent(chapter, GetOrCreateRoot());

            // ���ڿ�����ģʽ�����ɱ��ػ���
            if (_developerMode)
            {
                locComp.GenerateLocalizationKey(chapter, _ecsFramework);
            }

            return chapter;
        }

        // ����С�ڽڵ㣨����ظ����͸��ǹ��ܣ�
        public Entity CreateEpisode(
            Entity chapter,
            int number = 0,
            string title = "",
            bool overwrite = false
        )
        {
            if (chapter == null)
                throw new ArgumentException("�����ṩ��Ч���½�ʵ��");

            // ����½��Ƿ��� Localization ���
            if (!chapter.HasComponent<Comp.Localization>())
                throw new ArgumentException("�½�ʵ��δӵ�б��ػ���ǩ");

            // ����Ƿ��Ѵ�����ͬ��ŵ�С��
            var existingEpisode = FindEpisodeByNumber(chapter, number);
            if (existingEpisode != null && !overwrite)
            {
                Debug.Log($"���½� {chapter.Id} ���Ѵ������Ϊ {number} ��С�ڣ���������");
                return existingEpisode;
            }

            // ��������������ǣ���ɾ������С��
            if (existingEpisode != null && overwrite)
            {
                Debug.Log($"�����½� {chapter.Id} �����Ϊ {number} ������С��");
                _ecsFramework.RemoveEntity(existingEpisode.Id);
            }

            var episode = _ecsFramework.CreateEntity();

            // ��� Localization ���
            var locComp = Comp.Localization.CreateEpisode(number, title);
            episode.AddComponent(locComp);

            // ��� Order ���
            var orderComp = Comp.Order.CreateEpisodeOrder(number);
            episode.AddComponent(orderComp);

            _ecsFramework.SetParent(episode, chapter);

            // ���ڿ�����ģʽ�����ɱ��ػ���
            if (_developerMode)
            {
                locComp.GenerateLocalizationKey(episode, _ecsFramework);
            }

            return episode;
        }

        // �����Ի��нڵ㣨����ظ����͸��ǹ��ܣ�
        public Entity CreateLine(
            Entity episode,
            int number = 0,
            string dialogue = "",
            string speaker = "",
            bool overwrite = false
        )
        {
            if (episode == null)
                throw new ArgumentException("�����ṩ��Ч��С��ʵ��");

            if (!episode.HasComponent<Comp.Localization>())
                throw new ArgumentException("С��ʵ��δӵ�б��ػ���ǩ");

            // ����Ƿ��Ѵ�����ͬ��ŵĶ԰���
            var existingLine = FindLineByNumber(episode, number);
            if (existingLine != null && !overwrite)
            {
                Debug.Log($"��С�� {episode.Id} ���Ѵ������Ϊ {number} �Ķ԰��У���������");
                return existingLine;
            }

            // ��������������ǣ���ɾ�����ж԰���
            if (existingLine != null && overwrite)
            {
                Debug.Log($"����С�� {episode.Id} �����Ϊ {number} �����ж԰���");
                _ecsFramework.RemoveEntity(existingLine.Id);
            }

            var line = _ecsFramework.CreateEntity();
            var locComp = Comp.Localization.CreateLine(number, dialogue, speaker);
            line.AddComponent(locComp);

            // ��� Order ���
            var orderComp = Comp.Order.CreateLineOrder(number);
            line.AddComponent(orderComp);

            _ecsFramework.SetParent(line, episode);

            // ���ڿ�����ģʽ�����ɱ��ػ���
            if (_developerMode)
            {
                locComp.GenerateLocalizationKey(line, _ecsFramework);
            }

            return line;
        }

        // ����С�ڽڵ㣨�Զ�������ţ�
        public Entity CreateEpisode(Entity chapter, string title = "")
        {
            if (chapter == null)
                throw new ArgumentException("�����ṩ��Ч���½�ʵ��");

            int nextNumber = GetNextOrderNumber(chapter.Id);
            return CreateEpisode(chapter, nextNumber, title);
        }

        // �����Ի��нڵ㣨�Զ�������ţ�
        public Entity CreateLine(Entity episode, string dialogue = "", string speaker = "")
        {
            if (episode == null)
                throw new ArgumentException("�����ṩ��Ч��С��ʵ��");

            int nextNumber = GetNextOrderNumber(episode.Id);
            return CreateLine(episode, nextNumber, dialogue, speaker);
        }

        // ��ȡ�ڵ�ı��ػ���
        public string GetLocalizationKey(Entity entity)
        {
            if (entity == null || !entity.HasComponent<Comp.Localization>())
                return "";

            var locComp = entity.GetComponent<Comp.Localization>();
            return locComp.ContextKey;
        }

        // ���½ڵ���������ɱ��ػ��������ڿ�����ģʽ�£�
        public void UpdateLocalizationKey(Entity entity)
        {
            if (!_developerMode)
                return;
            if (entity == null || !entity.HasComponent<Comp.Localization>())
                return;

            var locComp = entity.GetComponent<Comp.Localization>();
            locComp.GenerateLocalizationKey(entity, _ecsFramework);
        }

        // �������б��ػ���
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
