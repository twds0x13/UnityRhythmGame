#if UNITY_EDITOR

using PageNS;
using UIEventSystemNS;
using UIManagerNS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PrefabSpawner
{
    public static class PrefabSpawner
    {
        static string PagePath = "Assets/GameObjects/UI Elements/Page/SamplePage.prefab";

        static string TextPath = "Assets/GameObjects/UI Elements/Text/SampleText.prefab";

        static string ButtonPath = "Assets/GameObjects/UI Elements/Button/SampleButton.prefab";

        /// <summary>
        /// ��Ϊÿ��ҳ�涼Ӧ�ü̳��Բ�ͬ���࣬���Լǵ�Ҫ�ֶ������ű�
        /// </summary>
        [MenuItem("Quick Spawn/Sample Page")]
        private static void CreateSamplePage()
        {
            CreateObjectFromPath(PagePath)
                .SetParent(GameObject.FindGameObjectWithTag("PageContainer"))
                .RegisterNum()
                .Select();
        }

        /// <summary>
        /// �����������������ֶΣ�ֻ��ƥ�� <see cref="Selection.activeGameObject"/> �����һ��
        /// </summary>
        [MenuItem("Quick Spawn/Sample Text")]
        private static void CreateSampleText()
        {
            if (Selection.activeGameObject.HasComponent<BaseUIPage>())
            {
                CreateObjectFromPath(TextPath).SetParent().RegisterDisplay<TextDisplay>().Select();
            }
        }

        [MenuItem("Quick Spawn/Sample Button")]
        private static void CreateSampleButton()
        {
            if (Selection.activeGameObject.HasComponent<BaseUIPage>())
            {
                CreateObjectFromPath(ButtonPath)
                    .SetParent()
                    .RegisterDisplay<SelectableDisplay>()
                    .RegisterEvent()
                    .Select();
            }
        }

        private static GameObject CreateObjectFromPath(string ObjectPath)
        {
            GameObject PagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ObjectPath);

            if (PagePrefab == null)
            {
                Debug.LogError($"Prefab not found at path: {ObjectPath}");

                return null;
            }

            GameObject Object = PrefabUtility.InstantiatePrefab(PagePrefab) as GameObject;

            PrefabUtility.UnpackPrefabInstance(
                Object,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction
            );

            Object.name = PagePrefab.name;

            return Object;
        }

        private static void Select(this GameObject Object)
        {
            Selection.activeGameObject = Object;
        }

        private static void ResetLocalPosition(GameObject Object)
        {
            Object.transform.localPosition = Vector3.zero;
            Object.transform.localScale = Vector3.one;
        }

        private static GameObject SetParent(this GameObject Object)
        {
            Undo.SetTransformParent(
                Object.transform,
                Selection.activeGameObject.transform,
                "Set " + Object.name + " as child of " + Selection.activeGameObject.name
            );

            ResetLocalPosition(Object);

            return Object;
        }

        private static GameObject SetParent(this GameObject Object, GameObject Parent)
        {
            Undo.SetTransformParent(
                Object.transform,
                Parent.transform,
                "Set " + Object.name + " as child of " + Parent.name
            );

            ResetLocalPosition(Object);

            return Object;
        }

        private static bool HasComponent<T>(this GameObject Object)
            where T : Component
        {
            return Object != null && Object.TryGetComponent<T>(out _);
        }

        /// <summary>
        /// ֻ�Ժ��� <see cref="BaseUIPage"/> ����Ķ�����Ч��������Ӽ̳��Խӿ� <see cref="IPageComponent"/> �Ķ���
        /// </summary>
        /// <param name="Object"></param>
        /// <returns></returns>
        private static GameObject RegisterDisplay<T>(this GameObject Object)
            where T : IPageComponent
        {
            // ���

            if (
                Selection.activeGameObject.HasComponent<BaseUIPage>()
                && Object.GetComponent<T>() != null
            )
            {
                Selection
                    .activeGameObject.GetComponent<BaseUIPage>()
                    .RegisterComponent(Object.GetComponent<T>());
            }

            return Object;
        }

        /// <summary>
        /// ֻ�Ժ��� <see cref="SelectableDisplay"/> ����� <see cref="Selectable"/> ������Ч
        /// </summary>
        /// <param name="Object"></param>
        /// <returns></returns>
        private static GameObject RegisterEvent(this GameObject Object)
        {
            if (
                Selection.activeGameObject.HasComponent<BaseUIPage>()
                && Object.GetComponent<SelectableDisplay>() != null
            )
            {
                Selection
                    .activeGameObject.GetComponent<BaseUIEventSystem>()
                    .RegisterSelectable(Object.GetComponent<Selectable>());
            }
            return Object;
        }

        private static GameObject RegisterNum(this GameObject Object)
        {
            if (Object.HasComponent<BaseUIPage>())
            {
                Object.GetComponent<Canvas>().worldCamera = Camera.main;

                Object
                    .GetComponent<BaseUIPage>()
                    .SetResizeDetector(
                        GameObject
                            .FindGameObjectWithTag("ResizeDetector")
                            .GetComponent<ResizeDetector>()
                    );

                GameObject
                    .FindGameObjectWithTag("GameSingletons")
                    .GetComponent<PageController>()
                    .AddPageObject(Object.GetComponent<BaseUIPage>());
            }

            return Object;
        }
    }
}

#endif
