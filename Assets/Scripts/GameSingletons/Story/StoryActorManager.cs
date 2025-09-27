using System.Collections.Generic;
using AudioNS;
using AudioRegistry;
using Cysharp.Threading.Tasks;
using Singleton;
using UnityEngine;
using Audio = AudioNS.AudioManager;

public class StoryActorManager : Singleton<StoryActorManager>
{
    [Ext.ReadOnlyInGame]
    List<BaseActorBehaviour> Actors;

    private Dictionary<string, BaseActorBehaviour> _registeredActors;

    protected override void SingletonAwake()
    {
        RegisterActors();
        UniTask.Void(Test);
    }

    private void RegisterActors() { }

    private async UniTaskVoid Test()
    {
        await UniTask.WaitForSeconds(1);
        LogManager.Log("±Èµ°......", nameof(StoryActorManager));
    }
}
