using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JsonLoader;
using Newtonsoft.Json;
using UnityEngine;
using static ECS.Comp;
using StoryJson = JsonLoader.StoryJsonManager;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public static class StorySerializerHelper
        {
            public static string Serialize(object obj)
            {
                return JsonConvert.SerializeObject(obj, BaseJsonLoader.Settings);
            }

            public static T Deserialize<T>(string json)
            {
                return JsonConvert.DeserializeObject<T>(json, BaseJsonLoader.Settings);
            }

            public static object Deserialize(string json, Type type)
            {
                return JsonConvert.DeserializeObject(json, type, BaseJsonLoader.Settings);
            }
        }

        #region Save To File


        public void SaveStoryTreeAsyncForget(string fileName, bool output = true)
        {
            // �����첽��������
            SaveStoryTreeAsync(fileName, output).Forget();
        }

        // �� StoryTreeManager ��ʹ���첽����
        public async UniTask<bool> SaveStoryTreeAsync(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            var entities = _ecsFramework.GetAllEntities().ToList();

            LogManager.Info(
                $"======Story.��ʼ�첽������������ļ�======",
                nameof(StoryTreeManager),
                output
            );

            // ʹ���첽���淽��
            bool saveSuccess = await StoryJson.TrySaveJsonToZipAsync(
                fileName,
                entities,
                BaseJsonLoader.Settings,
                output: output,
                cancellationToken: cancellationToken
            );

            if (saveSuccess)
            {
                LogManager.Info(
                    $"======Story.����첽������������ļ�======\n",
                    nameof(StoryTreeManager),
                    output
                );
            }
            else
            {
                LogManager.Error(
                    $"======Story.���������ʧ��======\n",
                    nameof(StoryTreeManager),
                    output
                );
            }

            return saveSuccess;
        }

        // ������������ļ�
        public void SaveStoryTree(string fileName, bool output = true)
        {
            var entities = _ecsFramework.GetAllEntities().ToList();

            LogManager.Info(
                $"======Story.��ʼ������������ļ�======",
                nameof(StoryTreeManager),
                output
            );

            StoryJson.TrySaveToZip(fileName, StorySerializerHelper.Serialize(entities));

            LogManager.Info(
                $"======Story.��ɱ�����������ļ�======\n",
                nameof(StoryTreeManager),
                output
            );
        }

        #endregion

        public void ClearStoryTree()
        {
            _ecsFramework.ClearAllEntities();
            _rootEntity = null;
        }

        #region Load From File

        public void LoadStoryTreeAsyncForget(
            string fileName,
            bool output = false,
            bool createNewIfMissing = true
        )
        {
            // �����첽��������
            LoadStoryTreeAsync(fileName, output, createNewIfMissing).Forget();
        }

        /// <summary>
        /// ���ļ����ع�����
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="output"></param>
        /// <param name="createNewIfMissing"></param>
        /// <returns></returns>
        public async UniTask<bool> LoadStoryTreeAsync(
            string fileName,
            bool output = false,
            bool createNewIfMissing = true
        )
        {
            LogManager.Info(
                $"======Story.��ʼ�첽���ع�����======\n",
                nameof(StoryTreeManager),
                true
            );

            // ����ļ��Ƿ����
            string filePath = Path.Join(Application.persistentDataPath, fileName);
            if (!File.Exists(filePath))
            {
                string errorMessage = $"�������ļ�������: {fileName}";

                // ��¼������־
                LogManager.Error(errorMessage, nameof(StoryTreeManager), output);

                if (createNewIfMissing)
                {
                    LogManager.Info("���Դ����µĹ�����", nameof(StoryTreeManager), output);

                    // ��յ�ǰ������
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // �����µĸ��ڵ�
                    try
                    {
                        CreateRoot("Root", true);

                        LogManager.Info("�Ѵ����µĹ��������ڵ�", nameof(StoryTreeManager), output);

                        // ��ѡ�����������´����Ĺ�����
                        SaveStoryTree(fileName, false);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Exception(ex, nameof(StoryTreeManager), output);
                        LogManager.Error($"�����¹�����ʧ��: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    LogManager.Error(errorMessage);
                    return false;
                }
            }

            try
            {
                var (loadSuccess, loadedEntities) = await StoryJson.TryLoadEntitiyListAsync<Entity>(
                    fileName,
                    output
                );

                if (!loadSuccess || loadedEntities == null || loadedEntities.Count == 0)
                {
                    string errorMessage = $"�������ļ��𻵻��ʽ����ȷ: {fileName}";
                    LogManager.Error(errorMessage, nameof(StoryTreeManager), output);

                    if (createNewIfMissing)
                    {
                        LogManager.Info("���Դ����µĹ�����", nameof(StoryTreeManager), output);

                        // ��յ�ǰ������
                        _ecsFramework.ClearAllEntities();
                        _rootEntity = null;

                        // �����µĸ��ڵ�
                        CreateRoot("Root", true);

                        LogManager.Info("�Ѵ����µĹ��������ڵ�", nameof(StoryTreeManager), output);

                        SaveStoryTree(fileName, false);

                        return true;
                    }
                    else
                    {
                        LogManager.Error(errorMessage);
                        return false;
                    }
                }

                // ��յ�ǰ������
                _ecsFramework.ClearAllEntities();
                _rootEntity = null;

                // �����ҵ����ڵ�ʵ��
                Entity rootEntity = loadedEntities.FirstOrDefault(e => e.HasComponent<Root>());

                LogManager.Info(
                    $"���ҵ� {loadedEntities.Count} �����½ڵ�ʵ��, ���ڵ�ʵ��: {(rootEntity != null ? " Name : " + rootEntity.GetComponent<Root>().RootName : "δ�ҵ�")}",
                    nameof(StoryTreeManager),
                    false
                );

                if (rootEntity == null)
                {
                    LogManager.Error("�Ҳ������ڵ�ʵ��", nameof(StoryTreeManager), output);
                    if (createNewIfMissing)
                    {
                        // �����µĸ��ڵ�
                        CreateRoot("Root", true);
                        return true;
                    }
                    return false;
                }

                // ���ʵ��
                int successCount = 0;
                int errorCount = 0;

                // ������Ӹ��ڵ�
                try
                {
                    _ecsFramework.ForceAddEntity(rootEntity, true);
                    _rootEntity = rootEntity;
                    successCount++;
                    LogManager.Info(
                        $"���ڵ�״̬���� (ID: {rootEntity.Id})\n",
                        nameof(StoryTreeManager),
                        output
                    );

                    LogManager.Info(
                        $"���ڵ��Ƿ�ӵ�� IdManager : {rootEntity.HasComponent<IdManager>()}",
                        nameof(StoryTreeManager),
                        false
                    );
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LogManager.Exception(ex, nameof(StoryTreeManager), output);
                    LogManager.Error(
                        $"��Ӹ��ڵ�ʵ��ʧ��: {ex.Message}",
                        nameof(StoryTreeManager),
                        output
                    );

                    // ������ڵ����ʧ�ܣ����Դ����µĹ�����
                    if (createNewIfMissing)
                    {
                        LogManager.Info("���Դ����µĹ�����", nameof(StoryTreeManager), output);
                        CreateRoot("Root", true);
                        return true;
                    }
                    return false;
                }

                // Ȼ���������ʵ��
                foreach (var entity in loadedEntities)
                {
                    if (entity.Id == rootEntity.Id)
                        continue;

                    try
                    {
                        _ecsFramework.AddEntity(entity, true); // ǿ�����
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error(
                            $"���ʵ��ʧ�� (ID: {entity?.Id}): {ex.Message}",
                            nameof(StoryTreeManager),
                            output
                        );
                        errorCount++;
                    }
                }

                // �ؽ����ù�ϵ
                _ecsFramework.RebuildEntityReferences();

                // ���¸��ڵ�����
                _rootEntity = _ecsFramework.FindRootEntity();

                // ����ID������
                UpdateIdManager();

                string logMessage =
                    $"�������Ѵ� {fileName} ����. �ɹ�: {successCount}, ʧ��: {errorCount}";

                if (errorCount > 0)
                {
                    LogManager.Warning(logMessage, nameof(StoryTreeManager), output);
                }
                else
                {
                    LogManager.Info(logMessage, nameof(StoryTreeManager), output);
                }

                LogManager.Info(
                    $"======Story.����첽���ع�����======\n",
                    nameof(StoryTreeManager)
                );

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = $"���ع�����ʧ��: {fileName}. ����: {ex.Message}";
                LogManager.Exception(ex, nameof(StoryTreeManager), output);
                LogManager.Error(errorMessage);

                if (createNewIfMissing)
                {
                    LogManager.Info("���Դ����µĹ�����", nameof(StoryTreeManager), output);

                    // ��յ�ǰ������
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // �����µĸ��ڵ�
                    CreateRoot("Root", true);

                    LogManager.Info("�Ѵ����µĹ��������ڵ�", nameof(StoryTreeManager), output);

                    LogManager.Info(
                        $"======Story.����첽���ع�����======\n",
                        nameof(StoryTreeManager)
                    );

                    return true;
                }

                return false;
            }
        }

        #endregion

        // ��֤�������ṹ
        public bool ValidateStoryTree(bool output = true)
        {
            LogManager.Info("======Story.��ʼ��֤���ṹ======", nameof(StoryTreeManager), output);

            // ����Ƿ��и��ڵ�
            if (!HasRoot())
            {
                LogManager.Log("����: û�и��ڵ�");
                return false;
            }

            var root = GetOrCreateRoot();

            // �����ڵ��Ƿ��� Root ���
            if (!root.HasComponent<Root>())
            {
                LogManager.Log("����: ���ڵ�û�� Root ���", nameof(StoryTreeManager));
                return false;
            }

            // �����ڵ��Ƿ��и��ڵ㣨��Ӧ���У�
            if (root.HasComponent<Parent>())
            {
                var parentComp = root.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    LogManager.Log("����: ���ڵ��и��ڵ�", nameof(StoryTreeManager));
                    return false;
                }
            }

            // ��������½��Ƿ�����ȷ�ı��ػ���
            var chapters = GetChapters();
            foreach (var chapter in chapters)
            {
                if (!chapter.HasComponent<Localization>())
                {
                    LogManager.Log($"����: �½� {chapter.Id} û�� Localization ���");
                    return false;
                }

                var locComp = chapter.GetComponent<Localization>();
                if (string.IsNullOrEmpty(locComp.ContextKey) || !locComp.ContextKey.StartsWith("C"))
                {
                    LogManager.Log(
                        $"����: �½� {chapter.Id} �ı��ػ�������ȷ: {locComp.ContextKey}"
                    );
                    return false;
                }
            }

            // ������Ӹ�����֤�߼�...

            LogManager.Info("======Story.�����֤���ṹ======\n", nameof(StoryTreeManager), output);

            return true;
        }
    }
}
