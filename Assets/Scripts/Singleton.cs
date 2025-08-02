using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Singleton
{
    /// <summary>
    /// �̳��� <see cref="MonoBehaviour"/> ��ȫ�ֵ������࣬Ҫ���ڳ����л�ʱ��������
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
        /// �������ʵ�� Awake ����
        /// </summary>
        protected abstract void SingletonAwake();
    }
}
