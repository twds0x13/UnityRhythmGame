using System;
using System.Collections.Generic;
using System.Linq;
using static ECS.Comp;
using static ECS.Comp.Localization;

namespace ECS
{
    public partial class StoryTreeManager
    {
        /// <summary>
        /// ��ȡʵ�����ʾ���ƣ�������־��
        /// </summary>
        private string GetEntityDisplayName(Entity entity)
        {
            if (entity == null)
                return "null";

            var order = entity.GetComponent<Order>();
            var localization = entity.GetComponent<Localization>();

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

        public StoryPointer CreatePointer(Entity startEntity = null)
        {
            if (_rootEntity == null)
            {
                _rootEntity = GetOrCreateRoot();
            }

            return new StoryPointer(_ecsFramework, startEntity ?? _rootEntity, true);
        }

        /// <summary>
        /// ��ȡĬ�Ͽ�ʼλ�ã���һ�µ�һС�ڵ�һ�У�
        /// </summary>
        /// <returns>ָ��Ĭ�Ͽ�ʼλ�õ�ָ�룬����Ҳ����򷵻�null</returns>
        public StoryPointer GetDefaultStartPointer()
        {
            try
            {
                var root = GetOrCreateRoot();
                if (root == null || !root.HasComponent<Children>())
                {
                    LogManager.Warning("���ڵ�û���ӽڵ㣬�޷��ҵ�Ĭ�Ͽ�ʼλ��");
                    return CreatePointer(root);
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
                return CreatePointer();
            }
        }
    }

    /// <summary>
    /// �������ָ�룬�ṩ�ڵ㵼�������ɨ�蹦�ܣ�֧���¼��ص�
    /// </summary>
    public class StoryPointer
    {
        private Entity _currentEntity;
        private readonly ECSFramework _ecsFramework;
        private readonly Stack<Entity> _history = new Stack<Entity>();

        // λ����Ϣ
        private NodeType _currentNodeType = NodeType.Null;
        private int _chapterNumber = 0;
        private int _episodeNumber = 0;
        private int _lineNumber = 0;

        // ѡ��ϵͳ״̬
        private bool _isChoiceBlocked = false;
        private Choice _currentChoice = null;

        // �¼��ص�ϵͳ
        public event Action<NodeType> OnNodeTypeChange; // ��ʱû���������ʹ��

        private event Action<int, int, int> _onPositionChange;
        private event Action _onChoiceEncounter;
        private event Action _onChoiceSelect;

        #region ��������

        public NodeType NodeType
        {
            get
            {
                var loc = _currentEntity?.GetComponent<Localization>();
                return loc?.Type ?? NodeType.Null;
            }
        }

        /// <summary>
        /// ��ǰָ���ʵ��
        /// </summary>
        public Entity Current => _currentEntity;

        /// <summary>
        /// ��ǰʵ��ID
        /// </summary>
        public int CurrentId => _currentEntity?.Id ?? -1;

        /// <summary>
        /// ��ǰ�ڵ�����
        /// </summary>
        public NodeType CurrentNodeType =>
            _currentEntity != null ? _currentNodeType : NodeType.Null;

        /// <summary>
        /// ��ǰ�½ڱ��
        /// </summary>
        public int ChapterNumber => _currentEntity != null ? _chapterNumber : 0;

        /// <summary>
        /// ��ǰ�������
        /// </summary>
        public int EpisodeNumber => _currentEntity != null ? _episodeNumber : 0;

        /// <summary>
        /// ��ǰ�������
        /// </summary>
        public int LineNumber => _currentEntity != null ? _lineNumber : 0;

        /// <summary>
        /// �Ƿ�ָ����Чʵ��
        /// </summary>
        public bool IsValid => _currentEntity != null;

        /// <summary>
        /// ��ǰ�Ƿ���ѡ������״̬
        /// </summary>
        public bool ChoiceBlocked => _isChoiceBlocked;

        /// <summary>
        /// ��ȡ��ǰ��ѡ�����������У�
        /// </summary>
        public List<ChoiceOption> ChoiceOptions => _currentChoice.Options;

        #endregion

        public StoryPointer(
            ECSFramework ecsFramework,
            Entity startEntity = null,
            bool output = false
        )
        {
            _ecsFramework = ecsFramework ?? throw new ArgumentNullException(nameof(ecsFramework));
            _currentEntity = startEntity;

            if (startEntity != null)
            {
                LogManager.Log(
                    $"ָ���ʼ������ʼʵ��ID: {startEntity.Id}",
                    nameof(StoryPointer),
                    output
                );

                UpdatePositionInfoAndNotify(output);
            }
        }

        #region ���ĵ�������

        /// <summary>
        /// ������һ�� - �Զ�������ת�߼���˳�򵼺�
        /// </summary>
        public bool Next()
        {
            if (_currentEntity == null)
                return false;

            // �������ѡ������״̬���������Զ�ǰ��
            if (_isChoiceBlocked)
            {
                LogManager.Warning("��ǰ����ѡ������״̬����������ѡ��");
                return false;
            }

            return TryNavigateByOrder();
        }

        /// <summary>
        /// ���ص���һ���ڵ�
        /// </summary>
        public bool Back()
        {
            if (_history.Count > 0)
            {
                _currentEntity = _history.Pop();

                ClearChoiceState();
                UpdatePositionInfoAndNotify();
                return true;
            }
            return false;
        }

        /// <summary>
        /// �ƶ���ָ��ʵ��
        /// </summary>
        public bool MoveTo(int entityId, bool output = true)
        {
            var target = _ecsFramework.GetEntitySafe(entityId);
            if (target != null)
            {
                _history.Push(_currentEntity);
                _currentEntity = target;
                ClearChoiceState();
                UpdatePositionInfoAndNotify(output);
                return true;
            }
            return false;
        }

        #endregion

        #region ѡ��ϵͳ

        /// <summary>
        /// ����ѡ����ת��Ŀ��ڵ�
        /// </summary>
        /// <param name="option">ѡ���ѡ��</param>
        /// <returns>�Ƿ�ɹ���ת</returns>
        public bool Choice(ChoiceOption option)
        {
            if (option == null)
            {
                LogManager.Error("ѡ��ѡ���Ϊnull");
                return false;
            }

            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("��ǰ����ѡ��״̬���޷�����ѡ��");
                return false;
            }

            // ��֤ѡ���Ƿ���Ч
            if (!_currentChoice.CanJumpTo(option.TargetId))
            {
                LogManager.Error($"��Ч��ѡ��ѡ�Ŀ��ID {option.TargetId} ����ѡ����");
                return false;
            }

            // ��ȡĿ��ʵ��
            var targetEntity = _ecsFramework.GetEntitySafe(option.TargetId);
            if (targetEntity == null)
            {
                LogManager.Error($"ѡ���Ŀ��ʵ�岻����: {option.TargetId}");
                return false;
            }

            LogManager.Log($"����ѡ��: {option.DisplayText} -> Ŀ�� {option.TargetId}");

            // ����ѡ������¼�
            TriggerChoiceSelected();

            // ��ת��Ŀ��ڵ�
            bool success = MoveToWithHistory(targetEntity);
            if (success)
            {
                // ���ѡ��״̬
                ClearChoiceState();
            }

            return success;
        }

        /// <summary>
        /// ͨ��ѡ����������ѡ��
        /// </summary>
        /// <param name="optionIndex">ѡ����������0��ʼ��</param>
        /// <returns>�Ƿ�ɹ���ת</returns>
        public bool Choice(int optionIndex)
        {
            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("��ǰ����ѡ��״̬���޷�����ѡ��");
                return false;
            }

            if (optionIndex < 0 || optionIndex >= _currentChoice.Options.Count)
            {
                LogManager.Error(
                    $"ѡ������Խ��: {optionIndex}����Ч��Χ [0, {_currentChoice.Options.Count - 1}]"
                );
                return false;
            }

            var option = _currentChoice.Options[optionIndex];
            return Choice(option);
        }

        /// <summary>
        /// ͨ��Ŀ��ID����ѡ��
        /// </summary>
        /// <param name="targetId">Ŀ��ʵ��ID</param>
        /// <returns>�Ƿ�ɹ���ת</returns>
        public bool ChoiceByTargetId(int targetId)
        {
            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("��ǰ����ѡ��״̬���޷�����ѡ��");
                return false;
            }

            var option = _currentChoice.Options.FirstOrDefault(o => o.TargetId == targetId);
            if (option == null)
            {
                LogManager.Error($"δ�ҵ�Ŀ��IDΪ {targetId} ��ѡ��");
                return false;
            }

            return Choice(option);
        }

        /// <summary>
        /// �Զ�ѡ���һ��ѡ����ڲ��Ի�Ĭ����Ϊ��
        /// </summary>
        /// <returns>�Ƿ�ɹ���ת</returns>
        public bool AutoChoiceFirst()
        {
            if (!_isChoiceBlocked || _currentChoice == null)
            {
                LogManager.Warning("��ǰ����ѡ��״̬���޷��Զ�ѡ��");
                return false;
            }

            if (_currentChoice.Options.Count == 0)
            {
                LogManager.Error("û�п��õ�ѡ��");
                return false;
            }

            LogManager.Log("�Զ�ѡ���һ��ѡ��");
            return Choice(0);
        }

        /// <summary>
        /// ���ѡ��״̬
        /// </summary>
        private void ClearChoiceState()
        {
            _isChoiceBlocked = false;
            _currentChoice = null;
        }

        /// <summary>
        /// ��鲢����ѡ��״̬
        /// </summary>
        private void CheckAndSetChoiceState()
        {
            if (_currentEntity?.HasComponent<Choice>() == true)
            {
                var choice = _currentEntity.GetComponent<Choice>();
                if (choice != null && choice.Options.Count > 0)
                {
                    _isChoiceBlocked = true;
                    _currentChoice = choice;
                    LogManager.Log($"����ѡ������״̬��ѡ������: {choice.Options.Count}");

                    // ����ѡ�������¼�
                    TriggerChoiceEncountered(choice);
                }
            }
            else
            {
                ClearChoiceState();
            }
        }

        #endregion

        #region �¼�����ϵͳ

        /// <summary>
        /// ����λ�ñ仯�¼�
        /// </summary>
        private void TriggerPositionChanged()
        {
            try
            {
                _onPositionChange?.Invoke(_chapterNumber, _episodeNumber, _lineNumber);
            }
            catch (Exception ex)
            {
                LogManager.Error($"����λ�ñ仯�¼�ʱ����: {ex.Message}", nameof(StoryPointer));
            }
        }

        /// <summary>
        /// ����ѡ������¼�
        /// </summary>
        private void TriggerChoiceEncountered(Choice choice)
        {
            _onChoiceEncounter?.Invoke();
            LogManager.Info($"��⵽ѡ�����", nameof(StoryPointer));
        }

        /// <summary>
        ///
        /// </summary>
        private void TriggerChoiceSelected()
        {
            _onChoiceSelect?.Invoke();
            LogManager.Info($"ѡ�������", nameof(StoryPointer));
        }

        /// <summary>
        /// �����ڵ����ͱ仯�¼�
        /// </summary>
        private void TriggerNodeTypeChanged()
        {
            try
            {
                OnNodeTypeChange?.Invoke(_currentNodeType);
            }
            catch (Exception ex)
            {
                LogManager.Error($"�����ڵ����ͱ仯�¼�ʱ����: {ex.Message}", nameof(StoryPointer));
            }
        }

        /// <summary>
        /// �ֶ�������ǰλ���¼��������ⲿǿ�Ƹ��£�
        /// </summary>
        public void TriggerCurrentEvents()
        {
            UpdatePositionInfoAndNotify();
        }

        /// <summary>
        /// ��ȫ�����λ�ñ仯�������������ظ����ģ�
        /// </summary>
        public void PositionChange(Action<int, int, int> handler)
        {
            _onPositionChange -= handler;
            _onPositionChange += handler;
        }

        /// <summary>
        /// ��ȫ�����ѡ������������������ظ����ģ�
        /// </summary>
        public void ChoiceEncounter(Action handler)
        {
            _onChoiceEncounter -= handler;
            _onChoiceEncounter += handler;
        }

        /// <summary>
        /// ��ȫ�����ѡ����ɼ������������ظ����ģ�
        /// </summary>
        public void ChoiceSelect(Action handler)
        {
            _onChoiceSelect -= handler;
            _onChoiceSelect += handler;
        }

        /// <summary>
        /// ��������¼�����
        /// </summary>
        public void ClearAllEvents()
        {
            _onPositionChange = null;
            _onChoiceEncounter = null;
            _onChoiceSelect = null;
            OnNodeTypeChange = null;
        }

        #endregion

        #region ʵ�÷���

        /// <summary>
        /// �����ʷ��¼
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>
        /// ���õ�Ĭ�Ͽ�ʼλ�ã������ʷ��
        /// </summary>
        public void ResetToDefaultStart()
        {
            var defaultStart = StoryTreeManager.Inst.GetDefaultStartPointer();
            if (defaultStart != null && defaultStart.IsValid)
            {
                ClearHistory();
                _currentEntity = defaultStart.Current;
                UpdatePositionInfoAndNotify();
                LogManager.Log($"�����õ�Ĭ�Ͽ�ʼλ��: {GetFormattedPath()}");
            }
        }

        /// <summary>
        /// ��ʼ�µľ���Ự�����õ���ʼλ�ò������ʷ��
        /// </summary>
        public void StartNewSession()
        {
            ResetToDefaultStart();
            ClearHistory();
        }

        /// <summary>
        /// ��ȡ��ǰ�ڵ��˳����Ϣ
        /// </summary>
        public Comp.Order GetOrderInfo()
        {
            return _currentEntity?.GetComponent<Order>();
        }

        /// <summary>
        /// ��ȡ��ʽ��·�����磺��1�� / ��2�� / 3.��
        /// </summary>
        public string GetFormattedPath()
        {
            var path = new List<string>();
            var tempPointer = new StoryPointer(_ecsFramework, _currentEntity);

            while (tempPointer.IsValid)
            {
                var order = tempPointer.GetOrderInfo();
                if (order != null)
                {
                    path.Insert(0, order.FormattedLabel);
                }

                if (!tempPointer.MoveToParent(false))
                    break;
            }

            return string.Join(" / ", path);
        }

        /// <summary>
        /// ��鵱ǰ�ڵ��Ƿ���episode�����һ��line
        /// </summary>
        public bool IsLastLineInEpisode()
        {
            if (_currentEntity?.HasComponent<Parent>() != true)
                return false;

            var parentComp = _currentEntity.GetComponent<Parent>();
            if (!parentComp.ParentId.HasValue)
                return false;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Children>() != true)
                return false;

            var childrenComp = parent.GetComponent<Children>();
            var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);
            return sortedChildren.Count > 0 && sortedChildren[^1].Id == _currentEntity.Id;
        }

        #endregion

        #region �ڲ�����ʵ��

        /// <summary>
        /// ��˳�򵼺�����һ���ڵ�
        /// </summary>
        private bool TryNavigateByOrder()
        {
            var nextEntity = GetNextByOrder();
            if (nextEntity != null)
            {
                return MoveToWithHistory(nextEntity);
            }
            return false;
        }

        /// <summary>
        /// �ƶ���ָ��ʵ�壨����ʷ��¼��
        /// </summary>
        private bool MoveToWithHistory(Entity entity)
        {
            if (entity != null)
            {
                _history.Push(_currentEntity);
                _currentEntity = entity;
                UpdatePositionInfoAndNotify();
                return true;
            }
            return false;
        }

        /// <summary>
        /// �ƶ������ڵ�
        /// </summary>
        private bool MoveToParent(bool output = true)
        {
            if (_currentEntity?.HasComponent<Parent>() == true)
            {
                var parentComp = _currentEntity.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    return MoveTo(parentComp.ParentId.Value, output);
                }
            }
            return false;
        }

        /// <summary>
        /// ��ȡ��ǰ�ڵ����һ���ڵ㣨��Order˳��
        /// </summary>
        private Entity GetNextByOrder()
        {
            if (_currentEntity == null)
                return null;

            // ���Ի�ȡ��һ���ֵܽڵ�
            var nextSibling = GetNextSiblingByOrder();
            if (nextSibling != null)
                return nextSibling;

            // �ڸ�����νṹ�в�����һ���ڵ�
            return GetNextInParentHierarchy();
        }

        /// <summary>
        /// ��Order˳���ȡ��һ���ֵܽڵ�
        /// </summary>
        private Entity GetNextSiblingByOrder()
        {
            if (_currentEntity?.HasComponent<Parent>() != true)
                return null;

            var parentComp = _currentEntity.GetComponent<Parent>();
            if (!parentComp.ParentId.HasValue)
                return null;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Children>();
            var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);

            int currentIndex = -1;
            for (int i = 0; i < sortedChildren.Count; i++)
            {
                if (sortedChildren[i].Id == _currentEntity.Id)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex >= 0 && currentIndex < sortedChildren.Count - 1)
            {
                return sortedChildren[currentIndex + 1];
            }

            return null;
        }

        /// <summary>
        /// �ڸ�����νṹ�в�����һ���ڵ�
        /// </summary>
        private Entity GetNextInParentHierarchy()
        {
            var current = _currentEntity;
            var visited = new HashSet<int> { current.Id };

            while (current != null)
            {
                if (!current.HasComponent<Parent>())
                    break;

                var parentComp = current.GetComponent<Parent>();
                if (!parentComp.ParentId.HasValue)
                    break;

                var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
                if (parent == null)
                    break;

                var nextSibling = GetNextSiblingForEntity(parent);
                if (nextSibling != null && !visited.Contains(nextSibling.Id))
                {
                    return GetFirstLeafNode(nextSibling);
                }

                current = parent;
                visited.Add(current.Id);
            }

            return null;
        }

        /// <summary>
        /// ��ȡָ��ʵ�����һ���ֵܽڵ�
        /// </summary>
        private Entity GetNextSiblingForEntity(Entity entity)
        {
            if (entity?.HasComponent<Parent>() != true)
                return null;

            var parentComp = entity.GetComponent<Parent>();
            if (!parentComp.ParentId.HasValue)
                return null;

            var parent = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
            if (parent?.HasComponent<Children>() != true)
                return null;

            var childrenComp = parent.GetComponent<Children>();
            var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);

            int currentIndex = -1;
            for (int i = 0; i < sortedChildren.Count; i++)
            {
                if (sortedChildren[i].Id == entity.Id)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex >= 0 && currentIndex < sortedChildren.Count - 1)
            {
                return sortedChildren[currentIndex + 1];
            }

            return null;
        }

        /// <summary>
        /// ��ȡָ���ڵ�ĵ�һ��Ҷ�ӽڵ�
        /// </summary>
        private Entity GetFirstLeafNode(Entity node)
        {
            if (node == null)
                return null;

            var current = node;
            while (current.HasComponent<Children>())
            {
                var childrenComp = current.GetComponent<Children>();
                var sortedChildren = GetSortedChildrenByOrder(childrenComp.ChildrenEntities);
                if (sortedChildren.Count == 0)
                    break;
                current = sortedChildren[0];
            }

            return current;
        }

        /// <summary>
        /// ��Order������ӽڵ�����
        /// </summary>
        private List<Entity> GetSortedChildrenByOrder(List<Entity> children)
        {
            return children
                .Where(child => child.HasComponent<Order>())
                .OrderBy(child => child.GetComponent<Order>().Number)
                .ToList();
        }

        #endregion

        #region λ����Ϣ����

        /// <summary>
        /// ����λ����Ϣ��֪ͨ�����¼�������
        /// </summary>
        private void UpdatePositionInfoAndNotify(bool output = true)
        {
            var oldNodeType = _currentNodeType;
            var oldChapter = _chapterNumber;
            var oldEpisode = _episodeNumber;
            var oldLine = _lineNumber;

            UpdatePositionInfo();

            // ����Ƿ���Ҫ�����¼�
            bool positionChanged =
                oldChapter != _chapterNumber
                || oldEpisode != _episodeNumber
                || oldLine != _lineNumber;

            bool nodeTypeChanged = oldNodeType != _currentNodeType;

            if (positionChanged)
            {
                TriggerPositionChanged();
            }

            if (nodeTypeChanged)
            {
                TriggerNodeTypeChanged();
            }

            // ���Ǽ��ѡ�����
            CheckAndSetChoiceState();

            // ��¼������Ϣ
            if (positionChanged || nodeTypeChanged)
            {
                LogManager.Log(
                    $"ָ��λ�ø���: �½�={_chapterNumber}, ����={_episodeNumber}, ����={_lineNumber}, ����={_currentNodeType}",
                    nameof(StoryPointer),
                    output
                );
            }
        }

        /// <summary>
        /// ���µ�ǰ�ڵ��λ����Ϣ������
        /// </summary>
        private void UpdatePositionInfo()
        {
            _currentNodeType = NodeType.Null;

            if (_currentEntity == null)
                return;

            // ͨ��Localization���ȷ���ڵ�����
            if (_currentEntity.HasComponent<Localization>())
            {
                var loc = _currentEntity.GetComponent<Localization>();
                _currentNodeType = loc.Type;
            }

            // ��ȡ�½ڡ������������ı��
            UpdateHierarchyNumbers();
        }

        /// <summary>
        /// ���²㼶�����Ϣ
        /// </summary>
        private void UpdateHierarchyNumbers()
        {
            if (_currentEntity == null)
                return;

            var pathFromRoot = _ecsFramework
                .GetFullPathFromRoot(_currentEntity)
                .Where(e => e.Id != 0);

            foreach (var entity in pathFromRoot)
            {
                if (entity.HasComponent<Localization>())
                {
                    var loc = entity.GetComponent<Localization>();

                    switch (loc.Type)
                    {
                        case NodeType.Chapter:
                            _chapterNumber = loc.Number;
                            break;
                        case NodeType.Episode:
                            _episodeNumber = loc.Number;
                            break;
                        case NodeType.Line:
                            _lineNumber = loc.Number;
                            break;
                    }
                }
            }
        }

        #endregion
    }
}
