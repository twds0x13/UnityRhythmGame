using System;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

namespace Singleton
{
    /// <summary>
    /// 继承自 <see cref="MonoBehaviour"/> 的全局单例基类，要求在场景切换时不能销毁
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static T _inst;
        public static T Inst
        {
            get
            {
                if (_inst == null)
                {
                    // 获取调用堆栈
                    StackTrace stackTrace = new StackTrace(1, true); // 跳过1帧（当前get_Inst方法）

                    StackFrame callerFrame = stackTrace.GetFrame(0);

                    string callerInfo =
                        $"Called from: {callerFrame.GetFileName()}:{callerFrame.GetFileLineNumber()}";

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
