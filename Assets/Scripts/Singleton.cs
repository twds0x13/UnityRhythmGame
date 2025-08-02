using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Singleton
{
    /// <summary>
    /// 继承自 <see cref="MonoBehaviour"/> 的全局单例基类，要求在场景切换时不能销毁
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : MonoBehaviour, ISingleton
        where T : MonoBehaviour
    {
        private static T _inst;
        public static T Inst
        {
            get
            {
                if (_inst == null)
                {
                    throw new MissingReferenceException($"No instance of {typeof(T).Name} found.");
                }

                return _inst;
            }
        }

        private void Awake()
        {
            if (_inst != null)
            {
                throw new InvalidOperationException(
                    $"An instance of {typeof(T).Name} already exists."
                );
            }

            _inst = this as T;

            DontDestroyOnLoad(gameObject);

            SingletonAwake();
        }

        /// <summary>
        /// 子类必须实现 Awake 方法
        /// </summary>
        protected abstract void SingletonAwake();
    }
}
