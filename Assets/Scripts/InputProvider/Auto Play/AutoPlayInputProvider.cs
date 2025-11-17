using System;
using System.Collections.Generic;

public class AutoPlayTrackInputProvider : ITrackInputProvider
{
    private readonly Dictionary<int, TrackInputHandlers> _trackHandlers;
    private bool _isEnabled = false;
    private readonly IAutoPlayController _autoPlayController;

    private class TrackInputHandlers
    {
        public Action OnPressed { get; set; }
        public Action OnReleased { get; set; }
        public bool IsPressed { get; set; }
    }

    public AutoPlayTrackInputProvider(ChartManager chartManager)
    {
        _autoPlayController = new AutoPlayController(chartManager);
        _trackHandlers = new Dictionary<int, TrackInputHandlers>();
    }

    public void Enable()
    {
        if (_isEnabled)
            return;

        _isEnabled = true;

        // 注册到自动播放控制器
        if (_autoPlayController != null)
        {
            _autoPlayController.OnTrackShouldPress += HandleAutoPlayPress;
            _autoPlayController.OnTrackShouldRelease += HandleAutoPlayRelease;
        }
    }

    public void Disable()
    {
        if (!_isEnabled)
            return;

        _isEnabled = false;

        // 从自动播放控制器取消注册
        if (_autoPlayController != null)
        {
            _autoPlayController.OnTrackShouldPress -= HandleAutoPlayPress;
            _autoPlayController.OnTrackShouldRelease -= HandleAutoPlayRelease;
        }
    }

    public void Register(int trackNumber, Action onPressed, Action onReleased)
    {
        // 如果已经注册过，先取消注册
        if (_trackHandlers.ContainsKey(trackNumber))
        {
            UnregisterInternal(trackNumber);
        }

        _trackHandlers[trackNumber] = new TrackInputHandlers
        {
            OnPressed = onPressed,
            OnReleased = onReleased,
            IsPressed = false,
        };
    }

    public bool IsRegistered(int number) => _trackHandlers.ContainsKey(number);

    public void Unregister(int trackNumber, Action onPressed, Action onReleased)
    {
        if (_trackHandlers.ContainsKey(trackNumber))
        {
            UnregisterInternal(trackNumber);
        }
    }

    private void UnregisterInternal(int trackNumber)
    {
        _trackHandlers.Remove(trackNumber);
    }

    private void HandleAutoPlayPress(int trackNumber)
    {
        if (!_isEnabled || !_trackHandlers.ContainsKey(trackNumber))
            return;

        var handler = _trackHandlers[trackNumber];
        if (!handler.IsPressed)
        {
            handler.IsPressed = true;
            handler.OnPressed?.Invoke();
        }
    }

    private void HandleAutoPlayRelease(int trackNumber)
    {
        if (!_isEnabled || !_trackHandlers.ContainsKey(trackNumber))
            return;

        var handler = _trackHandlers[trackNumber];
        if (handler.IsPressed)
        {
            handler.IsPressed = false;
            handler.OnReleased?.Invoke();
        }
    }

    public void RegisterToGameStart()
    {
        _autoPlayController?.RegisterToGameStart();
    }

    public void UnregisterFromGameStart()
    {
        _autoPlayController?.UnregisterFromGameStart();
    }
}
