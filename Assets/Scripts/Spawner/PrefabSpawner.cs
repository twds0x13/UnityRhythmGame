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
        static readonly string PagePath =
            "Assets/GameObjects/UI Elements/Prefabs/Page/SamplePage.prefab";

        static readonly string TextPath =
            "Assets/GameObjects/UI Elements/Prefabs/Text/SampleText.prefab";

        static readonly string ButtonPath =
            "Assets/GameObjects/UI Elements/Prefabs/Button/SampleButton.prefab";

        enum PageType
        {
            Normal,
            Hover,
        }

        /// <summary>
        /// 因为每个页面都应该继承自不同的类，所以记得要手动更换脚本
        /// </summary>
        [MenuItem("Quick Spawn/Sample Page")]
        private static void CreateSamplePage()
        {
            CreateObjectFromPath(PagePath)
                .SetParent(GameObject.FindGameObjectWithTag("PageContainer"))
                .RegisterNum(PageType.Normal)
                .Select();
        }

        /// <summary>
        /// 因为每个页面都应该继承自不同的类，所以记得要手动更换脚本
        /// </summary>
        [MenuItem("Quick Spawn/Sample Hover Page")]
        private static void CreateSampleHoverPage()
        {
            CreateObjectFromPath(PagePath)
                .SetParent(GameObject.FindGameObjectWithTag("HoverPageContainer"))
                .RegisterNum(PageType.Hover)
                .Select();
        }

        /// <summary>
        /// 不能用来批量生成字段，只会匹配 <see cref="Selection.activeGameObject"/> 的最后一个
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
        /// 只对含有 <see cref="BaseUIPage"/> 组件的对象生效，用来添加继承自接口 <see cref="IPageComponent"/> 的对象
        /// </summary>
        /// <param name="Object"></param>
        /// <returns></returns>
        private static GameObject RegisterDisplay<T>(this GameObject Object)
            where T : IPageComponent
        {
            // 神回

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
        /// 只对含有 <see cref="SelectableDisplay"/> 组件的 <see cref="Selectable"/> 对象生效
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

        private static GameObject RegisterNum(this GameObject Object, PageType type)
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

                switch (type)
                {
                    case PageType.Normal:
                        GameObject
                            .FindGameObjectWithTag("PageSingletons")
                            .GetComponent<PageManager>()
                            .AddPageObject(Object.GetComponent<BaseUIPage>());
                        break;
                    case PageType.Hover:
                        GameObject
                            .FindGameObjectWithTag("PageSingletons")
                            .GetComponent<PageManager>()
                            .AddHoverPageObject(Object.GetComponent<BaseUIPage>());
                        break;
                }
            }

            return Object;
        }
    }
}

#endif
