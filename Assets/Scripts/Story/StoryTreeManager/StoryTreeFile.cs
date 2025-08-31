using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using static ECS.Comp;
using Json = JsonLoader.JsonManager;

namespace ECS
{
    public partial class StoryTreeManager
    {
        public static class TreeSerializerHelper
        {
            private static JsonSerializerSettings _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            public static string Serialize(object obj)
            {
                return JsonConvert.SerializeObject(obj, _settings);
            }

            public static T Deserialize<T>(string json)
            {
                return JsonConvert.DeserializeObject<T>(json, _settings);
            }

            public static object Deserialize(string json, Type type)
            {
                return JsonConvert.DeserializeObject(json, type, _settings);
            }
        }

        // ������������ļ�
        public void SaveStoryTree(string fileName)
        {
            var entities = _ecsFramework.GetAllEntities().ToList();

            Json.TrySaveToZip(fileName, TreeSerializerHelper.Serialize(entities));
        }

        public void ClearStoryTree()
        {
            _ecsFramework.ClearAllEntities();
            _rootEntity = null;
        }

        // ���ļ����ع�����
        public bool LoadStoryTree(string fileName, bool createNewIfMissing = true)
        {
            // ����ļ��Ƿ����
            if (!File.Exists(Path.Join(Application.persistentDataPath, fileName)))
            {
                string errorMessage = $"�������ļ�������: {fileName}";

                // ��¼������־
                LogFile.Error(errorMessage, "StoryTreeManager");

                if (createNewIfMissing)
                {
                    LogFile.Info("���Դ����µĹ�����", "StoryTreeManager");

                    // ��յ�ǰ������
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // �����µĸ��ڵ�
                    try
                    {
                        CreateRoot("�½�������");
                        LogFile.Info("�Ѵ����µĹ��������ڵ�", "StoryTreeManager");

                        // ��ѡ�����������´����Ĺ�����
                        SaveStoryTree(fileName);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogFile.Exception(ex, "StoryTreeManager");
                        Debug.LogError($"�����¹�����ʧ��: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError(errorMessage);
                    return false;
                }
            }

            try
            {
                // ���Դ��ļ�����ʵ��
                List<Entity> entities;
                bool loadSuccess = Json.TryLoadJsonWithDebug(fileName, out entities);

                if (!loadSuccess || entities == null || entities.Count == 0)
                {
                    string errorMessage = $"�������ļ��𻵻��ʽ����ȷ: {fileName}";
                    LogFile.Error(errorMessage, "StoryTreeManager");

                    if (createNewIfMissing)
                    {
                        LogFile.Info("���Դ����µĹ�����", "StoryTreeManager");

                        // ��յ�ǰ������
                        _ecsFramework.ClearAllEntities();
                        _rootEntity = null;

                        // �����µĸ��ڵ�
                        CreateRoot("�½�������");
                        LogFile.Info("�Ѵ����µĹ��������ڵ�", "StoryTreeManager");
                        return true;
                    }
                    else
                    {
                        Debug.LogError(errorMessage);
                        return false;
                    }
                }

                // ��յ�ǰ������
                _ecsFramework.ClearAllEntities();
                _rootEntity = null;

                // �����ҵ����ڵ�ʵ��
                Entity rootEntity = entities.FirstOrDefault(e =>
                    e.HasComponent<Root>() && e.GetComponent<Root>() != null
                );

                if (rootEntity == null)
                {
                    LogFile.Error("�Ҳ������ڵ�ʵ��", "StoryTreeManager");
                    if (createNewIfMissing)
                    {
                        // �����µĸ��ڵ�
                        CreateRoot("�½�������");
                        return true;
                    }
                    return false;
                }

                // �������ʵ�壨�ų����ڵ㣩
                int successCount = 0;
                int errorCount = 0;

                try
                {
                    _ecsFramework.ForceAddEntity(rootEntity, true);
                    _rootEntity = rootEntity;
                    successCount++;
                    LogFile.Info($"����Ӹ��ڵ�ʵ�� (ID: {rootEntity.Id})", "StoryTreeManager");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LogFile.Exception(ex, "StoryTreeManager");
                    LogFile.Error($"��Ӹ��ڵ�ʵ��ʧ��: {ex.Message}", "StoryTreeManager");
                }

                foreach (var entity in entities)
                {
                    // �����Ѿ���ӵĸ��ڵ�ʵ��
                    if (entity.Id == rootEntity.Id)
                        continue;

                    try
                    {
                        _ecsFramework.AddEntity(entity);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogFile.Error(
                            $"���ʵ��ʧ�� (ID: {entity?.Id}): {ex.Message}",
                            "StoryTreeManager"
                        );
                        errorCount++;
                    }
                }

                // �ؽ����ù�ϵ
                _ecsFramework.RebuildReferences();

                // ���¸��ڵ�����
                _rootEntity = _ecsFramework.FindRootEntity();

                // ����ID������
                UpdateIdManager();

                string logMessage =
                    $"�������Ѵ� {fileName} ����. �ɹ�: {successCount}, ʧ��: {errorCount}";

                if (errorCount > 0)
                {
                    LogFile.Warning(logMessage, "StoryTreeManager");
                }
                else
                {
                    LogFile.Info(logMessage, "StoryTreeManager");
                }

                Debug.Log(logMessage);
                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = $"���ع�����ʧ��: {fileName}. ����: {ex.Message}";
                LogFile.Exception(ex, "StoryTreeManager");
                Debug.LogError(errorMessage);

                if (createNewIfMissing)
                {
                    LogFile.Info("���Դ����µĹ�����", "StoryTreeManager");

                    // ��յ�ǰ������
                    _ecsFramework.ClearAllEntities();
                    _rootEntity = null;

                    // �����µĸ��ڵ�
                    CreateRoot("�½�������");
                    LogFile.Info("�Ѵ����µĹ��������ڵ�", "StoryTreeManager");
                    return true;
                }

                return false;
            }
        }

        // ��֤�������ṹ
        public bool ValidateStoryTree()
        {
            // ����Ƿ��и��ڵ�
            if (!HasRoot())
            {
                Debug.Log("����: û�и��ڵ�");
                return false;
            }

            var root = GetOrCreateRoot();

            // �����ڵ��Ƿ��� Root ���
            if (!root.HasComponent<Root>())
            {
                Debug.Log("����: ���ڵ�û�� Root ���");
                return false;
            }

            // �����ڵ��Ƿ��и��ڵ㣨��Ӧ���У�
            if (root.HasComponent<Parent>())
            {
                var parentComp = root.GetComponent<Parent>();
                if (parentComp.ParentId.HasValue)
                {
                    Debug.Log("����: ���ڵ��и��ڵ�");
                    return false;
                }
            }

            // ��������½��Ƿ�����ȷ�ı��ػ���
            var chapters = GetChapters();
            foreach (var chapter in chapters)
            {
                if (!chapter.HasComponent<Localization>())
                {
                    Debug.Log($"����: �½� {chapter.Id} û�� Localization ���");
                    return false;
                }

                var locComp = chapter.GetComponent<Localization>();
                if (string.IsNullOrEmpty(locComp.ContextKey) || !locComp.ContextKey.StartsWith("C"))
                {
                    Debug.Log($"����: �½� {chapter.Id} �ı��ػ�������ȷ: {locComp.ContextKey}");
                    return false;
                }
            }

            // ������Ӹ�����֤�߼�...

            return true;
        }
    }
}
