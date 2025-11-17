using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UnityTrackInputProvider : ITrackInputProvider
{
    private readonly InputActionAsset _inputActions;
    private readonly Dictionary<int, TrackInputHandlers> _trackHandlers;
    private bool _isEnabled = false;

    private class TrackInputHandlers
    {
        public InputAction Action { get; set; }
        public Action OnPressed { get; set; }
        public Action OnReleased { get; set; }
        public Action<InputAction.CallbackContext> PressedHandler { get; set; }
        public Action<InputAction.CallbackContext> ReleasedHandler { get; set; }
    }

    public UnityTrackInputProvider(InputActionAsset inputActions)
    {
        _inputActions = inputActions;
        _trackHandlers = new Dictionary<int, TrackInputHandlers>();
    }

    public void Enable()
    {
        if (_isEnabled)
            return;

        foreach (var handler in _trackHandlers.Values)
        {
            handler.Action.Enable();
        }
        _isEnabled = true;
    }

    public void Disable()
    {
        if (!_isEnabled)
            return;

        foreach (var handler in _trackHandlers.Values)
        {
            handler.Action.Disable();
        }
        _isEnabled = false;
    }

    public void Register(int number, Action onPressed, Action onReleased)
    {
        var actionName = $"Track {number}";
        var trackAction = _inputActions.FindAction(actionName);

        if (trackAction == null)
        {
            LogManager.Error($"Input action {actionName} not found!");
            return;
        }

        // 如果已经注册过，先取消注册
        if (_trackHandlers.ContainsKey(number))
        {
            UnregisterInternal(number);
        }

        // 创建具体的处理方法（不是匿名函数）
        void PressedHandler(InputAction.CallbackContext ctx) => onPressed?.Invoke();
        void ReleasedHandler(InputAction.CallbackContext ctx) => onReleased?.Invoke();

        // 注册事件
        trackAction.started += PressedHandler;
        trackAction.canceled += ReleasedHandler;

        // 保存处理器引用
        _trackHandlers[number] = new TrackInputHandlers
        {
            Action = trackAction,
            OnPressed = onPressed,
            OnReleased = onReleased,
            PressedHandler = PressedHandler,
            ReleasedHandler = ReleasedHandler,
        };

        if (_isEnabled)
        {
            trackAction.Enable();
        }
    }

    public bool IsRegistered(int number) => _trackHandlers.ContainsKey(number);

    public void Unregister(int number, Action onPressed, Action onReleased)
    {
        if (_trackHandlers.ContainsKey(number))
        {
            UnregisterInternal(number);
        }
    }

    private void UnregisterInternal(int number)
    {
        var handlers = _trackHandlers[number];

        // 使用保存的处理器引用来解除绑定
        handlers.Action.started -= handlers.PressedHandler;
        handlers.Action.canceled -= handlers.ReleasedHandler;
        handlers.Action.Disable();

        _trackHandlers.Remove(number);
    }

    public void RegisterToGameStart() { }

    public void UnregisterFromGameStart() { }
}
