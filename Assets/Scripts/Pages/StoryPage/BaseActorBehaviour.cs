using System.Collections.Generic;
using NavigatorNS;
using UnityEngine;

[RequireComponent(typeof(UINavigator))]
public class BaseActorBehaviour : MonoBehaviour
{
    [Ext.ReadOnlyInGame]
    List<Sprite> Sprites;

    protected enum Array { }

    private Dictionary<string, Sprite> _registeredSprites;
}
