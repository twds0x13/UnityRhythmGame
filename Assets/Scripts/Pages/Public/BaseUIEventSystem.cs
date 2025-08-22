using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PageNS;
using UIManagerNS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UIEventSystemNS
{
    [RequireComponent(typeof(BaseUIPage))]
    public class BaseUIEventSystem : MonoBehaviour, IPageControlled
    {
        [Header("Parent Page Reference")]
        [SerializeField]
        protected BaseUIPage ParentPage;

        [Header("Selectable Objects")]
        public List<Selectable> Selectables = new();

        [SerializeField]
        protected Selectable FirstSelected;

        [SerializeField]
        protected Selectable LastSelected;

        [Header("Input Actions")]
        [SerializeField]
        protected InputActionReference NavigateReference;

        protected Dictionary<Selectable, Vector3> SelectableScales = new();

        [Header("Enable Scale Anime")]
        [SerializeField]
        protected bool IsScaleEnabled = true;

        [Header("Anime Scale")]
        [SerializeField]
        protected float AnimeScale = 1.5f;

        protected float SelectScaleDuration = 0.5f;
        protected float DeselectScaleDuration = 0.5f;

        protected Tween SelectTween;
        protected Tween DeselectTween;

#if UNITY_EDITOR

        public void RegisterSelectable(Selectable Selectable)
        {
            Selectables.Add(Selectable);
        }
#endif

        public void OnAwake()
        {
            // SelectScaleDuration = ParentPage.PageOpenAnimeDuration;

            // DeselectScaleDuration = ParentPage.PageCloseAnimeDuration;

            foreach (Selectable Selectable in Selectables)
            {
                AddSelectionListeners(Selectable);
                SelectableScales.Add(Selectable, Selectable.transform.localScale);
            }
        }

        public void OnOpenPage()
        {
            NavigateReference.action.performed += OnNavigate;

            for (int i = 0; i < SelectableScales.Count; i++)
            {
                Selectables[i].transform.localScale = SelectableScales[Selectables[i]];
            }

            if (Selectables.Count > 0)
            {
                UniTask.Void(SelectFirstAfterOneFrame);
            }

            ActivateSelecables();
        }

        public void OnClosePage()
        {
            NavigateReference.action.performed -= OnNavigate;

            InactivateSelectables();

            SelectTween.Kill(false);
            DeselectTween.Kill(false);
        }

        public void ActivateSelecables()
        {
            for (int i = 0; i < Selectables.Count; i++)
            {
                Selectables[i].gameObject.SetActive(true);

                Selectables[i].interactable = true;
            }
        }

        public void InactivateSelectables()
        {
            foreach (Selectable Selectable in Selectables)
            {
                Selectable.interactable = false;
            }
        }

        protected void AddSelectionListeners(Selectable Selectable)
        {
            // 获取事件触发器组件

            EventTrigger Trigger = Selectable.gameObject.GetComponent<EventTrigger>();

            if (Trigger == null)
            {
                Trigger = Selectable.gameObject.AddComponent<EventTrigger>();
            }

            // 添加选中事件触发器

            EventTrigger.Entry Select = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Select,
            };
            Select.callback.AddListener(OnSelect);
            Trigger.triggers.Add(Select);

            // 添加取消选中事件触发器

            EventTrigger.Entry Deselect = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Deselect,
            };
            Deselect.callback.AddListener(OnDeselect);
            Trigger.triggers.Add(Deselect);

            // 添加光标选中事件触发器

            EventTrigger.Entry PointerEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter,
            };
            PointerEnter.callback.AddListener(OnPointerEnter);
            Trigger.triggers.Add(PointerEnter);

            // 添加光标取消选中事件触发器

            EventTrigger.Entry PointerExit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit,
            };
            PointerExit.callback.AddListener(OnPointerExit);
            Trigger.triggers.Add(PointerExit);
        }

        public async UniTaskVoid SelectFirstAfterOneFrame()
        {
            await UniTask.NextFrame();

            EventSystem.current.SetSelectedGameObject(FirstSelected.gameObject);
        }

        public void OnSelect(BaseEventData EventData)
        {
            LastSelected = EventData.selectedObject.GetComponent<Selectable>();

            if (!IsScaleEnabled)
                return;

            Vector3 NewScale = SelectableScales[LastSelected] * AnimeScale;

            SelectTween = EventData
                .selectedObject.transform.DOScale(NewScale, SelectScaleDuration)
                .SetEase(Ease.OutQuad);
        }

        public void OnDeselect(BaseEventData EventData)
        {
            Selectable Object = EventData.selectedObject.GetComponent<Selectable>();

            if (!IsScaleEnabled)
                return;

            DeselectTween = EventData
                .selectedObject.transform.DOScale(SelectableScales[Object], SelectScaleDuration)
                .SetEase(Ease.OutQuad);
        }

        public void OnPointerEnter(BaseEventData EventData)
        {
            PointerEventData PointerEventData = EventData as PointerEventData;

            if (PointerEventData != null)
            {
                Selectable Selectable =
                    PointerEventData.pointerEnter.GetComponentInParent<Selectable>();

                if (Selectable == null)
                {
                    Selectable = PointerEventData.pointerEnter.GetComponentInChildren<Selectable>();
                }

                PointerEventData.selectedObject = Selectable.gameObject;
            }
        }

        public void OnPointerExit(BaseEventData EventData)
        {
            PointerEventData PointerEventData = EventData as PointerEventData;

            if (PointerEventData != null)
            {
                PointerEventData.selectedObject = null;
            }
        }

        protected virtual void OnNavigate(InputAction.CallbackContext Ctx)
        {
            if (EventSystem.current.currentSelectedGameObject == null && LastSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(LastSelected.gameObject);
            }
        }
    }
}
