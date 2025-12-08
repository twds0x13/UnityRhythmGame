using System;

public interface ITrackInputProvider
{
    void Enable();

    void Disable();

    bool IsRegistered(int trackNumber);

    bool IsPressing(int trackNumber);

    void Register(int trackNumber, Action onPressed, Action onReleased);

    void Unregister(int trackNumber, Action onPressed, Action onReleased);

    void RegisterToGameStart();

    void UnregisterFromGameStart();
}

// 自动播放控制器接口
public interface IAutoPlayController
{
    event Action<int> OnTrackShouldPress;
    event Action<int> OnTrackShouldRelease;

    void RegisterToGameStart();

    void UnregisterFromGameStart();
}
