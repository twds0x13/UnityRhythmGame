using System.Collections.Generic;
using NavigatorNS;
using UnityEngine;

[RequireComponent(typeof(UINavigator))]
public class BaseActorBehaviour : MonoBehaviour
{
    [Ext.ReadOnlyInGame]
    public List<Sprite> Sprites;

    private readonly Dictionary<string, Sprite> _registeredSprites = new();

    public void Awake() { }

    private void RegisterSprites()
    {
        foreach (var sprite in Sprites)
        {
            if (sprite != null && !_registeredSprites.ContainsKey(sprite.name))
            {
                _registeredSprites.Add(sprite.name, sprite);
            }
        }
    }
}
