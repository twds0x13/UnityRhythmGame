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
            return GetOrCreateRoot()
                .Children()
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

            return chapter
                .Children()
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

            return episode
                .Children()
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type == Comp.Localization.NodeType.Line
                )
                .ToList();
        }

        /// <summary>
        /// ���������½ڣ�ʹ��Orderϵͳ�������
        /// </summary>
        /// <param name="titles">�½ڱ�������</param>
        /// <returns>�������½�ʵ���б�</returns>
        public List<Entity> CreateChapters(params string[] titles)
        {
            var root = GetOrCreateRoot();
            if (root == null)
                throw new InvalidOperationException("�޷������½ڣ���Ϊ���ڵ㲻����");

            var chapters = new List<Entity>();

            // ��ȡ��ʼ���
            int startOrder = GetNextOrderNumber(root);

            for (int i = 0; i < titles.Length; i++)
            {
                // ʹ���Զ���������
                var chapter = CreateChapter(startOrder + i, titles[i]);
                chapters.Add(chapter);
            }

            return chapters;
        }

        /// <summary>
        /// �ӵ�ǰ�����ſ�ʼ��������С�ڣ���˳��������
        /// </summary>
        /// <param name="chapter">���½�ʵ��</param>
        /// <param name="titles">С�ڱ�������</param>
        /// <returns>������С��ʵ���б�</returns>
        public List<Entity> CreateEpisodes(Entity chapter, params string[] titles)
        {
            if (chapter == null)
                throw new ArgumentException("�����ṩ��Ч���½�ʵ��");

            var episodes = new List<Entity>();

            // ��ȡ��ʼ��� - ʹ���µ����ط���
            int startOrder = GetNextOrderNumber(chapter);

            for (int i = 0; i < titles.Length; i++)
            {
                // ʹ���Զ���������
                var episode = CreateEpisode(chapter, startOrder + i, titles[i]);
                episodes.Add(episode);
            }

            return episodes;
        }

        /// <summary>
        /// �ӵ�ǰ�����ſ�ʼ���������Ի��У���˳��������
        /// </summary>
        /// <param name="episode">��С��ʵ��</param>
        /// <param name="dialogues">�Ի���������</param>
        /// <returns>�����ĶԻ���ʵ���б�</returns>
        public List<Entity> CreateLines(Entity episode, params string[] dialogues)
        {
            if (episode == null)
                throw new ArgumentException("�����ṩ��Ч��С��ʵ��");

            var lines = new List<Entity>();

            // ��ȡ��ʼ��� - ʹ���µ����ط���
            int startOrder = GetNextOrderNumber(episode);

            for (int i = 0; i < dialogues.Length; i++)
            {
                // ʹ���Զ���������
                var line = CreateLine(episode, startOrder + i, dialogues[i]);
                lines.Add(line);
            }

            return lines;
        }

        // ������Ų����½�
        public Entity FindChapterByNumber(int number)
        {
            foreach (var chapter in GetOrCreateRoot().Children())
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

        /// <summary>
        /// ע�⣡<paramref name="overwrite"/> ֻ���ֶ�ָ�� <paramref name="number"/> �½����ʱ�Ż���Ч��
        /// ������ <paramref name="overwrite"/> ʱ����ǿ��Ҫ�� <paramref name="number"/> ��Ϊ 0��
        /// </summary>
        /// <param name="number"></param>
        /// <param name="title"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public Entity CreateChapter(int number = 0, string title = null, bool overwrite = false)
        {
            // ����Ƿ��Ѵ�����ͬ��ŵ��½�
            var existingChapter = FindChapterByNumber(number);
            if (existingChapter != null && !overwrite && number > 0)
            {
                LogManager.Log($"�Ѵ������Ϊ {number} ���½ڣ���������");
                return existingChapter;
            }

            // ��������������ǣ���ɾ�������½�
            if (existingChapter != null && overwrite && number > 0)
            {
                LogManager.Log($"�������Ϊ {number} �������½�");
                _ecsFramework.RemoveEntity(existingChapter.Id);
            }

            if (number <= 0 && overwrite)
            {
                throw new ArgumentOutOfRangeException("���ø���ʱ���½���ű������ 0");
            }

            var chapter = _ecsFramework.CreateEntity();

            // ��� Localization ���
            var locComp = Comp.Localization.CreateChapter(number, title);
            chapter.AddComponent(locComp);

            // ��� Order ���
            var orderComp = Comp.Order.CreateChapterOrder(number);
            chapter.AddComponent(orderComp);

            _ecsFramework.SetParent(chapter, GetOrCreateRoot());

            // ���ɱ��ػ���
            locComp.GenerateLocalizationKey(chapter, _ecsFramework);

            return chapter;
        }

        // ����С�ڽڵ㣨����ظ����͸��ǹ��ܣ�
        public Entity CreateEpisode(
            Entity chapter,
            int number = 0,
            string title = null,
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
                LogManager.Log($"���½� {chapter.Id} ���Ѵ������Ϊ {number} ��С�ڣ���������");
                return existingEpisode;
            }

            // ��������������ǣ���ɾ������С��
            if (existingEpisode != null && overwrite)
            {
                LogManager.Log($"�����½� {chapter.Id} �����Ϊ {number} ������С��");
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

            // ���ɱ��ػ���
            locComp.GenerateLocalizationKey(episode, _ecsFramework);

            return episode;
        }

        // �����Ի��нڵ㣨����ظ����͸��ǹ��ܣ�
        public Entity CreateLine(
            Entity episode,
            int number = 0,
            string dialogue = null,
            string speaker = null,
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
                LogManager.Log($"��С�� {episode.Id} ���Ѵ������Ϊ {number} �Ķ԰��У���������");
                return existingLine;
            }

            // ��������������ǣ���ɾ�����ж԰���
            if (existingLine != null && overwrite)
            {
                LogManager.Log($"����С�� {episode.Id} �����Ϊ {number} �����ж԰���");
                _ecsFramework.RemoveEntity(existingLine.Id);
            }

            var line = _ecsFramework.CreateEntity();
            var locComp = Comp.Localization.CreateLine(number, dialogue, speaker);
            line.AddComponent(locComp);

            // ��� Order ���
            var orderComp = Comp.Order.CreateLineOrder(number);
            line.AddComponent(orderComp);

            _ecsFramework.SetParent(line, episode);

            // ���ɱ��ػ���
            locComp.GenerateLocalizationKey(line, _ecsFramework);

            return line;
        }

        /// <summary>
        /// �Զ������½����ʱ��������ظ����͸��ǡ�
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        // �����½ڽڵ㣨�Զ�������ţ�
        public Entity CreateChapter(string title = "")
        {
            var root = GetOrCreateRoot();

            if (root == null)
                throw new InvalidOperationException("�޷������½ڣ���Ϊ���ڵ㲻����");

            int nextNumber = GetNextOrderNumber(root.Id);
            return CreateChapter(nextNumber, title);
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
