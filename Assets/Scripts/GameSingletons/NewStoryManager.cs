using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ECS;
using Singleton;
using UnityEngine;

namespace StoryNS
{
    public class StoryManager : Singleton<StoryManager>
    {
        private ECSManager _storyManager;

        public ECSManager ECSManager
        {
            get
            {
                if (_storyManager == null)
                {
                    _storyManager = new ECSManager();
                }
                return _storyManager;
            }
        }

        public void GetNextLine()
        {
            // Debug.Log("哈！");
        }

        protected override void SingletonAwake()
        {
            UniTask.Void(Test);
        }

        public async UniTaskVoid Test()
        {
            await UniTask.Delay(1000);
            Debug.Log(ECSManager);
            return;
        }
    }
}
