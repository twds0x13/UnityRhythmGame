using System;
using System.Collections.Generic;
using System.Linq;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// ��ȡĬ�Ͽ�ʼλ�ã���һ�µ�һС�ڵ�һ�У�
        /// </summary>
        /// <returns>ָ��Ĭ�Ͽ�ʼλ�õ�ָ�룬����Ҳ����򷵻�null</returns>
        public StoryPointer GetDefaultStartPointer()
        {
            try
            {
                var root = GetOrCreateRoot();
                if (root == null || !root.HasComponent<Comp.Children>())
                {
                    LogManager.Warning("���ڵ�û���ӽڵ㣬�޷��ҵ�Ĭ�Ͽ�ʼλ��");
                    return CreatePointer(root); // ���ٷ��ظ��ڵ�ָ��
                }

                // ��ȡ��һ���½ڣ���Order����
                var firstChapter = GetFirstChildByOrder(root);
                if (firstChapter == null)
                {
                    LogManager.Warning("û���ҵ��κ��½�");
                    return CreatePointer(root);
                }

                // ��ȡ��һ��С��
                var firstEpisode = GetFirstChildByOrder(firstChapter);
                if (firstEpisode == null)
                {
                    LogManager.Warning($"�½� {GetEntityDisplayName(firstChapter)} û��С��");
                    return CreatePointer(firstChapter);
                }

                // ��ȡ��һ��
                var firstLine = GetFirstChildByOrder(firstEpisode);
                if (firstLine == null)
                {
                    LogManager.Warning($"С�� {GetEntityDisplayName(firstEpisode)} û�жԻ���");
                    return CreatePointer(firstEpisode);
                }

                var pointer = CreatePointer(firstLine);
                LogManager.Log($"Ĭ�Ͽ�ʼλ��: {pointer.GetFormattedPath()}");
                return pointer;
            }
            catch (Exception ex)
            {
                LogManager.Error($"��ȡĬ�Ͽ�ʼλ��ʱ����: {ex.Message}");
                return CreatePointer(); // ���ظ��ڵ�ָ����Ϊfallback
            }
        }

        /// <summary>
        /// ��Order˳���ȡ��һ���ӽڵ�
        /// </summary>
        private Entity GetFirstChildByOrder(Entity parent)
        {
            if (parent?.HasComponent<Comp.Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Comp.Children>();
            if (childrenComp.ChildrenEntities.Count == 0)
                return null;

            // ��Order���򲢷��ص�һ��
            return childrenComp
                .ChildrenEntities.Where(child => child.HasComponent<Comp.Order>())
                .OrderBy(child => child.GetComponent<Comp.Order>().Number)
                .FirstOrDefault();
        }

        /// <summary>
        /// ��ȡʵ�����ʾ���ƣ�������־��
        /// </summary>
        private string GetEntityDisplayName(Entity entity)
        {
            if (entity == null)
                return "null";

            var order = entity.GetComponent<Comp.Order>();
            var localization = entity.GetComponent<Comp.Localization>();

            if (order != null && !string.IsNullOrEmpty(order.FormattedLabel))
                return order.FormattedLabel;

            if (localization != null && !string.IsNullOrEmpty(localization.DefaultText))
                return localization.DefaultText.Length > 20
                    ? localization.DefaultText.Substring(0, 20) + "..."
                    : localization.DefaultText;

            return $"ʵ�� {entity.Id}";
        }

        /// <summary>
        /// ���ٿ�ʼ���� - ����ָ�벢�ƶ���Ĭ�Ͽ�ʼλ��
        /// </summary>
        public StoryPointer StartStory()
        {
            var pointer = GetDefaultStartPointer();
            if (pointer != null && pointer.IsValid)
            {
                LogManager.Log($"���鿪ʼ: {pointer.GetFormattedPath()}");
            }
            return pointer;
        }

        /// <summary>
        /// ����Ƿ������Ч�Ŀ�ʼλ��
        /// </summary>
        public bool HasValidStartPosition()
        {
            try
            {
                var root = GetOrCreateRoot();
                if (root?.HasComponent<Comp.Children>() != true)
                    return false;

                var firstChapter = GetFirstChildByOrder(root);
                if (firstChapter?.HasComponent<Comp.Children>() != true)
                    return false;

                var firstEpisode = GetFirstChildByOrder(firstChapter);
                if (firstEpisode?.HasComponent<Comp.Children>() != true)
                    return false;

                var firstLine = GetFirstChildByOrder(firstEpisode);
                return firstLine != null;
            }
            catch
            {
                return false;
            }
        }

        public StoryPointer CreatePointer(Entity startEntity = null)
        {
            if (_rootEntity == null)
            {
                _rootEntity = GetOrCreateRoot();
            }

            return new StoryPointer(_ecsFramework, startEntity ?? _rootEntity);
        }

        /// <summary>
        /// �Զ�Ϊ����episode�����һ��line������ת���
        /// </summary>
        public void AutoConfigureEpisodeJumps()
        {
            var allEpisodes = _ecsFramework
                .GetAllEntities()
                .Where(e =>
                    e.HasComponent<Comp.Localization>()
                    && e.GetComponent<Comp.Localization>().Type
                        == Comp.Localization.NodeType.Episode
                )
                .ToList();

            foreach (var episode in allEpisodes)
            {
                if (episode.HasComponent<Comp.Children>())
                {
                    var childrenComp = episode.GetComponent<Comp.Children>();
                    if (childrenComp.ChildrenEntities.Count > 0)
                    {
                        // ��ȡ���һ��line����Order����
                        var sortedLines = childrenComp
                            .ChildrenEntities.Where(child => child.HasComponent<Comp.Order>())
                            .OrderBy(child => child.GetComponent<Comp.Order>().Number)
                            .ToList();

                        if (sortedLines.Count > 0)
                        {
                            var lastLine = sortedLines[sortedLines.Count - 1];
                            var pointer = CreatePointer(lastLine);
                            pointer.ConfigureEpisodeJump();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ��֤��ת����
        /// </summary>
        public List<string> ValidateJumpConfigurations()
        {
            var issues = new List<string>();

            var allEntities = _ecsFramework.GetAllEntities();
            foreach (var entity in allEntities)
            {
                if (entity.HasComponent<Comp.Jump>())
                {
                    var jumpComp = entity.GetComponent<Comp.Jump>();
                    foreach (var targetId in jumpComp.AllowedTargetIds)
                    {
                        var target = _ecsFramework.GetEntitySafe(targetId);
                        if (target == null)
                        {
                            issues.Add($"ʵ�� {entity.Id} ����תĿ�� {targetId} ������");
                        }
                    }
                }
            }

            return issues;
        }
    }
}
