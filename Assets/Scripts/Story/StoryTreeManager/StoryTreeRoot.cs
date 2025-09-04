using System;
using System.Collections.Generic;
using System.Linq;
using static ECS.Comp;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public bool HasRoot()
        {
            return FindActualRootEntities(false).Any();
        }

        /// <summary>
        /// �������п��ܳ�Ϊ���ڵ��ʵ�壨û�и��ڵ�򸸽ڵ㲻���ڵ�ʵ�壩
        /// ���ȿ��� ID=0 ��ʵ��
        /// </summary>
        /// <returns>Ǳ�ڸ��ڵ�ʵ����б������ȼ�����</returns>
        public List<Entity> FindPotentialRootEntities(bool enableLog = true)
        {
            var potentialRoots = new List<Entity>();
            var allEntities = _ecsFramework.GetAllEntities();

            // ���Ȳ��� ID=0 ��ʵ��
            var entityWithIdZero = allEntities.FirstOrDefault(e => e.Id == 0);
            if (entityWithIdZero != null)
            {
                // ID=0 ��ʵ����������ȼ�
                potentialRoots.Add(entityWithIdZero);

                LogManager.Log(
                    $"������ ID=0 ��ʵ��: ID={entityWithIdZero.Id}",
                    nameof(StoryTreeManager),
                    enableLog
                );
            }

            foreach (var entity in allEntities)
            {
                // ���� ID=0 ��ʵ�壬��Ϊ�Ѿ��������
                if (entity.Id == 0)
                    continue;

                // ���ʵ���Ƿ��и����
                if (!entity.HasComponent<Parent>())
                {
                    // û�и�����������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogManager.Log(
                        $"������Ǳ�ڸ��ڵ� (�޸����): ID={entity.Id}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                    continue;
                }

                var parentComp = entity.GetComponent<Parent>();

                // ��鸸����Ƿ��и�ID
                if (!parentComp.ParentId.HasValue)
                {
                    // �и������û�и�ID�������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogManager.Log(
                        $"������Ǳ�ڸ��ڵ� (�޸�ID): ID={entity.Id}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                    continue;
                }

                // ��鸸ID��Ӧ��ʵ���Ƿ����
                var parentEntity = _ecsFramework.GetEntitySafe(parentComp.ParentId.Value);
                if (parentEntity == null)
                {
                    // ��ʵ�岻���ڣ������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogManager.Log(
                        $"�ҵ�Ǳ�ڸ��ڵ� (��ʵ�岻����): ID={entity.Id}, ��ID={parentComp.ParentId.Value}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                }
            }

            LogManager.Info(
                $"�������� {potentialRoots.Count} ��Ǳ�ڵĸ��ڵ�ʵ��",
                nameof(StoryTreeManager)
            );
            return potentialRoots;
        }

        public Entity GetOrCreateRoot()
        {
            if (_rootEntity != null)
            {
                LogManager.Info(
                    $"�����ڵ��Ƿ�ǿ� : {_rootEntity != null}",
                    nameof(StoryTreeManager),
                    false
                );

                return _rootEntity;
            }

            // ʹ�ù����������Ҹ��ڵ�
            _rootEntity = _ecsFramework.FindRootEntity();

            if (_rootEntity != null)
            {
                // ȷ�����ڵ��� Root ���
                if (!_rootEntity.HasComponent<Root>())
                {
                    _rootEntity.AddComponent(new Root { RootName = RootName });

                    LogManager.Warning($"�Զ�Ϊ Root ʵ�� {_rootEntity.Id} ��� Root ���");
                }

                // ȷ�����ڵ��� ID �������
                if (!_rootEntity.HasComponent<IdManager>())
                {
                    _rootEntity.AddComponent(new IdManager(true));

                    LogManager.Warning(
                        $"�Զ�Ϊ Root ʵ�� {_rootEntity.Id} ��� IdManager �������ʼֵ�趨Ϊ {0}"
                    );
                }

                if (_rootEntity.HasComponent<Parent>())
                {
                    _rootEntity.RemoveComponent<Parent>();

                    LogManager.Warning($"�Զ��Ƴ� Root ʵ�� {_rootEntity.Id} �ϵ� Parent ���");
                }

                return _rootEntity;
            }
            else
            {
                // �����µĸ��ڵ� (ֻ����û���ҵ����ڵ�ʱ��ִ��)

                return CreateRoot(RootName, true);
            }
        }

        /// <summary>
        /// ��֤�������еĸ��ڵ�ṹ
        /// </summary>
        /// <returns>��֤����������Ϣ</returns>
        public (bool isValid, string message) ValidateRootStructure()
        {
            var actualRoots = FindActualRootEntities(false);

            if (actualRoots.Count == 0)
            {
                return (false, "û���ҵ����ڵ�ʵ��");
            }

            if (actualRoots.Count > 1)
            {
                var rootIds = string.Join(", ", actualRoots.Select(r => r.Id));
                return (false, $"�ҵ�������ڵ�ʵ��: {rootIds}");
            }

            var root = actualRoots[0];

            // �����ڵ��Ƿ��и��ڵ㣨��Ӧ���У�
            if (root.HasComponent<Parent>())
            {
                var parentComp = root.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    return (
                        false,
                        $"���ڵ� (ID={root.Id}) �и��ڵ� (ID={parentComp.ParentId.Value})"
                    );
                }
            }

            // �����ڵ��Ƿ���IdManager���
            if (!root.HasComponent<IdManager>())
            {
                return (false, $"���ڵ� (ID={root.Id}) û��IdManager���");
            }

            return (true, $"���ڵ�ṹ����: ID={root.Id}");
        }

        /// <summary>
        /// ���Ҳ��������������ĸ��ڵ�ʵ�壨 ��Root��� û����Ч���ڵ� IdΪ0 ��ʵ�壩
        /// </summary>
        /// <returns>�����ĸ��ڵ�ʵ����б�</returns>
        public List<Entity> FindActualRootEntities(bool enableLog = true)
        {
            var potentialRoots = FindPotentialRootEntities(enableLog);
            var actualRoots = new List<Entity>();

            foreach (var entity in potentialRoots)
            {
                if (entity.HasComponent<Root>())
                {
                    // ��Root�����ȷ���Ǹ��ڵ�
                    actualRoots.Add(entity);
                    LogManager.Log(
                        $"ȷ�ϸ��ڵ�: ID={entity.Id}",
                        nameof(StoryTreeManager),
                        enableLog
                    );
                }
            }

            LogManager.Info(
                $"�������� {actualRoots.Count} �������ĸ��ڵ�ʵ��",
                nameof(StoryTreeManager)
            );
            return actualRoots;
        }

        /// <summary>
        /// ���Ҳ��޸����ڵ����⣨������ڵ��û�и��ڵ㣩
        /// </summary>
        /// <returns>�޸���ĸ��ڵ�ʵ�壬����޸�ʧ�ܷ���null</returns>
        public Entity FindAndFixRootIssues()
        {
            var actualRoots = FindActualRootEntities();

            // ���1��û�и��ڵ�
            if (actualRoots.Count == 0)
            {
                LogManager.Warning("û���ҵ����ڵ�ʵ��", nameof(StoryTreeManager));

                var potentialRoots = FindPotentialRootEntities();

                if (potentialRoots.Count > 0)
                {
                    // ѡ���һ��Ǳ�ڸ��ڵ㲢���Root���
                    var selectedRoot = potentialRoots[0];
                    LogManager.Info(
                        $"��ʵ�� ID={selectedRoot.Id} ת��Ϊ���ڵ�",
                        nameof(StoryTreeManager)
                    );

                    selectedRoot.AddComponent(new Root { RootName = "�޸��ĸ��ڵ�" });

                    // ȷ��û�и����
                    if (selectedRoot.HasComponent<Parent>())
                    {
                        selectedRoot.RemoveComponent<Parent>();
                    }

                    // ȷ����IdManager���
                    if (!selectedRoot.HasComponent<IdManager>())
                    {
                        selectedRoot.AddComponent(new IdManager(true));
                    }

                    _rootEntity = selectedRoot;
                    return selectedRoot;
                }
                else
                {
                    // û��Ǳ�ڸ��ڵ㣬�����µĸ��ڵ�
                    LogManager.Info("�����µĸ��ڵ�", nameof(StoryTreeManager));
                    return CreateRoot("�½����ڵ�");
                }
            }

            // ���2���ж�����ڵ�
            if (actualRoots.Count > 1)
            {
                LogManager.Warning(
                    $"�ҵ� {actualRoots.Count} �����ڵ�ʵ��",
                    nameof(StoryTreeManager)
                );

                // ѡ��ID=0�ĸ��ڵ㣨������ڣ�
                var rootWithIdZero = actualRoots.FirstOrDefault(r => r.Id == 0);
                if (rootWithIdZero != null)
                {
                    LogManager.Info($"ѡ��ID=0�ĸ��ڵ���Ϊ��Ҫ���ڵ�", nameof(StoryTreeManager));

                    // ���������ڵ�ת��Ϊ��ͨʵ��
                    foreach (var root in actualRoots)
                    {
                        if (root.Id != 0)
                        {
                            LogManager.Info(
                                $"��ʵ�� ID={root.Id} �Ӹ��ڵ�ת��Ϊ��ͨʵ��",
                                nameof(StoryTreeManager)
                            );

                            // �Ƴ�Root���
                            root.RemoveComponent<Comp.Root>();

                            // ���û�и����������Ϊ��Ҫ���ڵ���ӽڵ�
                            if (!root.HasComponent<Parent>())
                            {
                                _ecsFramework.SetParent(root, rootWithIdZero);
                            }
                        }
                    }

                    _rootEntity = rootWithIdZero;
                    return rootWithIdZero;
                }

                // û��ID=0�ĸ��ڵ㣬ѡ���һ�����ڵ���Ϊ��Ҫ���ڵ�
                LogManager.Info(
                    $"ѡ���һ�����ڵ� (ID={actualRoots[0].Id}) ��Ϊ��Ҫ���ڵ�",
                    nameof(StoryTreeManager)
                );
                var primaryRoot = actualRoots[0];

                // ���������ڵ�ת��Ϊ��ͨʵ��
                for (int i = 1; i < actualRoots.Count; i++)
                {
                    var root = actualRoots[i];
                    LogManager.Info(
                        $"��ʵ�� ID={root.Id} �Ӹ��ڵ�ת��Ϊ��ͨʵ��",
                        nameof(StoryTreeManager)
                    );

                    // �Ƴ�Root���
                    root.RemoveComponent<Comp.Root>();

                    // ���û�и����������Ϊ��Ҫ���ڵ���ӽڵ�
                    if (!root.HasComponent<Parent>())
                    {
                        _ecsFramework.SetParent(root, primaryRoot);
                    }
                }

                _rootEntity = primaryRoot;
                return primaryRoot;
            }

            // ���3��ֻ��һ�����ڵ㣨���������
            LogManager.Info($"�ҵ�Ψһ���ڵ�: ID={actualRoots[0].Id}", nameof(StoryTreeManager));
            _rootEntity = actualRoots[0];
            return actualRoots[0];
        }

        /// <summary>
        /// �������ڵ�ʵ��
        /// </summary>
        /// <param name="title"></param>
        /// <param name="overwrite">����ʱǿ���������� <see cref="_rootEntity"/> ����</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Entity CreateRoot(string title, bool overwrite = false)
        {
            if (_rootEntity != null && !overwrite)
            {
                throw new InvalidOperationException($"���ڵ��Ѵ��� (ID : {_rootEntity.Id} )");
            }

            // ����Ƿ��Ѵ��ڸ��ڵ�ʵ��
            var existingRoots = FindActualRootEntities(false);

            if (existingRoots.Count > 0 && !overwrite)
            {
                var existingRootIds = string.Join(", ", existingRoots.Select(r => r.Id));
                throw new InvalidOperationException($"�Ѵ��ڸ��ڵ�ʵ�� (IDs: {existingRootIds})");
            }

            // ��������ǣ����Ƴ����и��ڵ㣨�����쳣����¶�� Root �ڵ㣩
            if (overwrite)
            {
                if (_rootEntity != null)
                {
                    _ecsFramework.RemoveEntity(_rootEntity.Id);
                    _rootEntity = null;
                }

                foreach (var existingRoot in existingRoots)
                {
                    _ecsFramework.RemoveEntity(existingRoot.Id);
                }
            }

            _rootEntity = new Entity(0);

            _rootEntity.AddComponent(
                new Root
                {
                    RootName = string.IsNullOrEmpty(title)
                        ? "This should not appear in Json"
                        : title,
                }
            );

            _rootEntity.AddComponent(new IdManager(true));

            // ȷ�����ڵ�û�и����

            if (_rootEntity.HasComponent<Parent>())
            {
                _rootEntity.RemoveComponent<Parent>();
            }

            _ecsFramework.ForceAddEntity(_rootEntity, true);

            return _rootEntity;
        }
    }
}
