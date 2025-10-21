using System.Collections.Generic;
using System.Security.Policy;
using AudioNS;
using AudioRegistry;
using Cysharp.Threading.Tasks;
using Singleton;
using UnityEngine;
using Audio = AudioNS.AudioManager;

public class StoryActorManager : Singleton<StoryActorManager>
{
    [Ext.ReadOnlyInGame]
    public List<BaseActorBehaviour> Actors;

    private readonly Dictionary<string, BaseActorBehaviour> _registeredActors = new();

    protected override void SingletonAwake()
    {
        RegisterActors();
        UniTask.Void(Test);
    }

    private void RegisterActors()
    {
        foreach (var actor in Actors)
        {
            if (actor != null && !_registeredActors.ContainsKey(actor.name))
            {
                _registeredActors.Add(actor.name, actor);
            }
        }
    }

    private async UniTaskVoid Test()
    {
        await UniTask.WaitForSeconds(1);
        // LogManager.Log("±Èµ°......", nameof(StoryActorManager));
    }
}
