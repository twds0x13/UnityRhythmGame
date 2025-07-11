using System;
using System.Collections.Generic;
using Anime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace PooledObject
{
    public class PooledObjectBehaviour : MonoBehaviour
    {
        public UnityEvent DestroyEvent = new();

        public System.Random Rand = new();

        public SpriteRenderer SpriteRenderer; // 记得把所有需要 Object Pooling 的预制体挂上 SpriteRenderer

        public Sprite[] SpriteList; // 需要非空

        public AnimeMachine Anime;

        public virtual void Init(AnimeMachine Machine)
        {
            Anime = Machine;
        }

        public virtual PooledObjectBehaviour GetBase()
        {
            return this;
        }
    }
}
