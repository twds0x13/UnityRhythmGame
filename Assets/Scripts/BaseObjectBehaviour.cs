using System;
using System.Collections.Generic;
using GBehaviour;
using IUtils;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using Vectors;

namespace BaseObject
{
    public class BaseObjectBehaviour : MonoBehaviour, IDev
    {
        public UnityEvent DestroyEvent = new UnityEvent();

        public SpriteRenderer SpriteRenderer;

        public Sprite[] SpriteList;

        public bool isDestroy;

        public Queue<VectorPair> AnimeQueue;

        public void OnEnable()
        {
            isDestroy = false;
        }

        private void OnDestroy()
        {
            if (!isDestroy)
            {
                isDestroy = true;

                DestroyEvent?.Invoke();
            }
        }

        public void DevLog()
        {
            Debug.Log("Using Base Object : \r\n");
        }
    }
}
