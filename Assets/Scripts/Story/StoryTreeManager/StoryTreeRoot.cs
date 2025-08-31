using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ECS.Comp;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public bool HasRoot()
        {
            return SearchActualRootEntities().Any();
        }

        /// <summary>
        /// �������п��ܳ�Ϊ���ڵ��ʵ�壨û�и��ڵ�򸸽ڵ㲻���ڵ�ʵ�壩
        /// ���ȿ��� ID=0 ��ʵ��
        /// </summary>
        /// <returns>Ǳ�ڸ��ڵ�ʵ����б������ȼ�����</returns>
        public List<Entity> FindPotentialRootEntities()
        {
            var potentialRoots = new List<Entity>();
            var allEntities = _ecsFramework.GetAllEntities();

            // ���Ȳ��� ID=0 ��ʵ��
            var entityWithIdZero = allEntities.FirstOrDefault(e => e.Id == 0);
            if (entityWithIdZero != null)
            {
                // ID=0 ��ʵ����������ȼ�
                potentialRoots.Add(entityWithIdZero);
                LogFile.Log($"�ҵ� ID=0 ��ʵ��: ID={entityWithIdZero.Id}", "StoryTreeManager");
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
                    LogFile.Log($"�ҵ�Ǳ�ڸ��ڵ� (�޸����): ID={entity.Id}", "StoryTreeManager");
                    continue;
                }

                var parentComp = entity.GetComponent<Parent>();

                // ��鸸����Ƿ��и�ID
                if (!parentComp.ParentId.HasValue)
                {
                    // �и������û�и�ID�������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogFile.Log($"�ҵ�Ǳ�ڸ��ڵ� (�޸�ID): ID={entity.Id}", "StoryTreeManager");
                    continue;
                }

                // ��鸸ID��Ӧ��ʵ���Ƿ����
                var parentEntity = _ecsFramework.GetEntity(parentComp.ParentId.Value);
                if (parentEntity == null)
                {
                    // ��ʵ�岻���ڣ������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogFile.Log(
                        $"�ҵ�Ǳ�ڸ��ڵ� (��ʵ�岻����): ID={entity.Id}, ��ID={parentComp.ParentId.Value}",
                        "StoryTreeManager"
                    );
                }
            }

            LogFile.Info($"�ҵ� {potentialRoots.Count} ��Ǳ�ڸ��ڵ�ʵ��", "StoryTreeManager");
            return potentialRoots;
        }

        public Entity GetOrCreateRoot()
        {
            if (_rootEntity != null)
                return _rootEntity;

            // ʹ�ù����������Ҹ��ڵ�
            _rootEntity = _ecsFramework.FindRootEntity();

            if (_rootEntity != null)
            {
                // ȷ�����ڵ��� Root ���
                if (!_rootEntity.HasComponent<Root>())
                {
                    _rootEntity.AddComponent(new Root { RootName = RootName });

                    Debug.LogWarning($"�Զ�Ϊ Root ʵ�� {_rootEntity.Id} ��� Root ���");
                }

                // ȷ�����ڵ��� ID �������
                if (!_rootEntity.HasComponent<IdManager>())
                {
                    _rootEntity.AddComponent(new IdManager(true));

                    Debug.LogWarning(
                        $"�Զ�Ϊ Root ʵ�� {_rootEntity.Id} ��� IdManager �������ʼֵ�趨Ϊ{0}"
                    );
                }

                if (_rootEntity.HasComponent<Parent>())
                {
                    _rootEntity.RemoveComponent<Parent>();

                    Debug.LogWarning($"�Զ��Ƴ� Root ʵ�� {_rootEntity.Id} �ϵ� Parent ���");
                }

                return _rootEntity;
            }

            // �����µĸ��ڵ� (ֻ����û���ҵ����ڵ�ʱ��ִ��)
            return CreateRoot(RootName);
        }

        /// <summary>
        /// ��֤�������еĸ��ڵ�ṹ
        /// </summary>
        /// <returns>��֤����������Ϣ</returns>
        public (bool isValid, string message) ValidateRootStructure()
        {
            var actualRoots = SearchActualRootEntities();

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
        /// �������п��ܳ�Ϊ���ڵ��ʵ�壨û�и��ڵ�򸸽ڵ㲻���ڵ�ʵ�壩
        /// </summary>
        /// <returns>Ǳ�ڸ��ڵ�ʵ����б�</returns>
        public List<Entity> SearchPotentialRootEntities()
        {
            var potentialRoots = new List<Entity>();
            var allEntities = _ecsFramework.GetAllEntities();

            foreach (var entity in allEntities)
            {
                // ���ʵ���Ƿ��и����
                if (!entity.HasComponent<Parent>())
                {
                    // û�и�����������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogFile.Log($"�ҵ�Ǳ�ڸ��ڵ� (�޸����): ID={entity.Id}", "StoryTreeManager");
                    continue;
                }

                var parentComp = entity.GetComponent<Parent>();

                // ��鸸����Ƿ��и�ID
                if (!parentComp.ParentId.HasValue)
                {
                    // �и������û�и�ID�������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogFile.Log($"�ҵ�Ǳ�ڸ��ڵ� (�޸�ID): ID={entity.Id}", "StoryTreeManager");
                    continue;
                }

                // ��鸸ID��Ӧ��ʵ���Ƿ����
                var parentEntity = _ecsFramework.GetEntity(parentComp.ParentId.Value);
                if (parentEntity == null)
                {
                    // ��ʵ�岻���ڣ������Ǹ��ڵ�
                    potentialRoots.Add(entity);
                    LogFile.Log(
                        $"�ҵ�Ǳ�ڸ��ڵ� (��ʵ�岻����): ID={entity.Id}, ��ID={parentComp.ParentId.Value}",
                        "StoryTreeManager"
                    );
                }
            }

            LogFile.Info($"�ҵ� {potentialRoots.Count} ��Ǳ�ڸ��ڵ�ʵ��", "StoryTreeManager");
            return potentialRoots;
        }

        /// <summary>
        /// ���Ҳ��������������ĸ��ڵ�ʵ�壨 ��Root��� û����Ч���ڵ� IdΪ0 ��ʵ�壩
        /// </summary>
        /// <returns>�����ĸ��ڵ�ʵ����б�</returns>
        public List<Entity> SearchActualRootEntities()
        {
            var potentialRoots = SearchPotentialRootEntities();
            var actualRoots = new List<Entity>();

            foreach (var entity in potentialRoots)
            {
                if (entity.HasComponent<Root>())
                {
                    // ��Root�����ȷ���Ǹ��ڵ�
                    actualRoots.Add(entity);
                    LogFile.Log($"ȷ�ϸ��ڵ�: ID={entity.Id}", "StoryTreeManager");
                }
            }

            LogFile.Info($"�ҵ� {actualRoots.Count} �������ĸ��ڵ�ʵ��", "StoryTreeManager");
            return actualRoots;
        }

        /// <summary>
        /// ���Ҳ��޸����ڵ����⣨������ڵ��û�и��ڵ㣩
        /// </summary>
        /// <returns>�޸���ĸ��ڵ�ʵ�壬����޸�ʧ�ܷ���null</returns>
        public Entity FindAndFixRootIssues()
        {
            var actualRoots = SearchActualRootEntities();

            // ���1��û�и��ڵ�
            if (actualRoots.Count == 0)
            {
                LogFile.Warning("û���ҵ����ڵ�ʵ��", "StoryTreeManager");

                var potentialRoots = SearchPotentialRootEntities();

                if (potentialRoots.Count > 0)
                {
                    // ѡ���һ��Ǳ�ڸ��ڵ㲢���Root���
                    var selectedRoot = potentialRoots[0];
                    LogFile.Info($"��ʵ�� ID={selectedRoot.Id} ת��Ϊ���ڵ�", "StoryTreeManager");

                    selectedRoot.AddComponent(new Comp.Root { RootName = "�޸��ĸ��ڵ�" });

                    // ȷ��û�и����
                    if (selectedRoot.HasComponent<Parent>())
                    {
                        selectedRoot.RemoveComponent<Parent>();
                    }

                    // ȷ����IdManager���
                    if (!selectedRoot.HasComponent<Comp.IdManager>())
                    {
                        selectedRoot.AddComponent(new Comp.IdManager(true));
                    }

                    _rootEntity = selectedRoot;
                    return selectedRoot;
                }
                else
                {
                    // û��Ǳ�ڸ��ڵ㣬�����µĸ��ڵ�
                    LogFile.Info("�����µĸ��ڵ�", "StoryTreeManager");
                    return CreateRoot("�½����ڵ�");
                }
            }

            // ���2���ж�����ڵ�
            if (actualRoots.Count > 1)
            {
                LogFile.Warning($"�ҵ� {actualRoots.Count} �����ڵ�ʵ��", "StoryTreeManager");

                // ѡ��ID=0�ĸ��ڵ㣨������ڣ�
                var rootWithIdZero = actualRoots.FirstOrDefault(r => r.Id == 0);
                if (rootWithIdZero != null)
                {
                    LogFile.Info($"ѡ��ID=0�ĸ��ڵ���Ϊ��Ҫ���ڵ�", "StoryTreeManager");

                    // ���������ڵ�ת��Ϊ��ͨʵ��
                    foreach (var root in actualRoots)
                    {
                        if (root.Id != 0)
                        {
                            LogFile.Info(
                                $"��ʵ�� ID={root.Id} �Ӹ��ڵ�ת��Ϊ��ͨʵ��",
                                "StoryTreeManager"
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
                LogFile.Info(
                    $"ѡ���һ�����ڵ� (ID={actualRoots[0].Id}) ��Ϊ��Ҫ���ڵ�",
                    "StoryTreeManager"
                );
                var primaryRoot = actualRoots[0];

                // ���������ڵ�ת��Ϊ��ͨʵ��
                for (int i = 1; i < actualRoots.Count; i++)
                {
                    var root = actualRoots[i];
                    LogFile.Info($"��ʵ�� ID={root.Id} �Ӹ��ڵ�ת��Ϊ��ͨʵ��", "StoryTreeManager");

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
            LogFile.Info($"�ҵ�Ψһ���ڵ�: ID={actualRoots[0].Id}", "StoryTreeManager");
            _rootEntity = actualRoots[0];
            return actualRoots[0];
        }

        /// <summary>
        /// +�������ڵ�ʵ��
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
            var existingRoots = SearchActualRootEntities();

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

            _rootEntity = new Entity(0); // ǿ������ Root �ڵ���Ϊ 0

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
