using System;
using UnityEngine;

namespace Singleton
{
    public static class GameSingletons { }

    /// <summary>
    /// 继承自 <see cref="MonoBehaviour"/> 的全局单例基类，在场景切换时不进行销毁
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        // #if UNITY_EDITOR
        // 在编辑器中，我们不使用 Lazy<T>，而是使用简单的静态字段
        private static T _instance;

        public static T Inst
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindOrCreateInstance();
                }
                return _instance;
            }
        }

        // 在每次播放模式开始时重置实例
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            _instance = null;
        }

        // #endif

        // 如果发布版本出了问题，就来这里改
        /*
#else


        private static Lazy<T> _inst = new(
            FindOrCreateInstance,
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        public static T Inst => _inst.Value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            _inst = new(FindOrCreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);
        }
#endif
        */

        /// <summary>
        /// 正常来讲，游戏应该能获取到场景中的 <see cref="GameSingletons"/> 对应物体 暂时不用管新建组件
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static T FindOrCreateInstance()
        {
            var existingInstance = FindObjectOfType<T>();

            if (existingInstance != null)
            {
                return existingInstance;
            }

            // 创建新实例
            var singletonObject = GameObject
                .FindGameObjectsWithTag(nameof(GameSingletons))
                .Length switch
            {
                0 => new GameObject(nameof(GameSingletons)) { tag = nameof(GameSingletons) },
                1 => GameObject.FindGameObjectWithTag(nameof(GameSingletons)),
                _ => throw new InvalidOperationException(
                    $"场景中发现多个 {nameof(GameSingletons)} 物体。"
                ),
            };

            var instance = singletonObject.AddComponent<T>();

            // 确保在场景切换时不销毁
            DontDestroyOnLoad(singletonObject);

            return instance;
        }

        private void Awake()
        {
            // #if UNITY_EDITOR
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            /*
                            #else
                        if (_inst.IsValueCreated && _inst.Value != null && _inst.Value != this)
                        {
                            Destroy(gameObject);
                            return;
                        }
            #endif
            */

            SingletonAwake();
        }

        protected virtual void OnDestroy()
        {
            SingletonDestroy();

            // #if UNITY_EDITOR
            if (_instance == this)
            {
                _instance = null;
                // LogManager.Info($"自动销毁 {typeof(T).Name} 实例", nameof(GameSingletons));
            }
            /*
#else
            if (_inst.IsValueCreated && _inst.Value == this)
            {
                Debug.LogWarning(
                    $"Singleton instance of {typeof(T).Name} is being destroyed. This may cause issues."
                );
            }
#endif
            */
        }

        /// <summary>
        /// 子类必须实现 Awake 方法
        /// </summary>
        protected abstract void SingletonAwake();

        /// <summary>
        /// 不强制要求
        /// </summary>
        protected virtual void SingletonDestroy() { }
    }
}
