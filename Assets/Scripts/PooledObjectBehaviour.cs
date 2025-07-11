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

        public SpriteRenderer SpriteRenderer; // �ǵð�������Ҫ Object Pooling ��Ԥ������� SpriteRenderer

        public Sprite[] SpriteList; // ��Ҫ�ǿ�

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
