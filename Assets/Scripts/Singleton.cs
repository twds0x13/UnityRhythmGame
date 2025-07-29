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
                    Debug.Log(nameof(_inst) + "Null on Get");
                }

                return _inst;
            }
        }

        private void Awake()
        {
            if (_inst != null)
            {
                Debug.Log(nameof(_inst) + "Not Null when Awake");
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
