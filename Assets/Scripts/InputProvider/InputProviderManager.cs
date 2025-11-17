using System;
using System.Collections.Generic;
using Singleton;

public class InputProviderManager : Singleton<InputProviderManager>
{
    private readonly Dictionary<Type, ITrackInputProvider> _providers = new();
    private ITrackInputProvider _currentProvider;
    private bool _isRegisteredToGameStart = false;

    public event Action OnInputProviderChange;

    protected override void SingletonAwake() { }

    // 最简单的批量注册
    public void RegisterAllToGameStart()
    {
        if (_isRegisteredToGameStart)
            return;

        foreach (var provider in _providers.Values)
        {
            provider.RegisterToGameStart();
        }

        _isRegisteredToGameStart = true;
    }

    // 最简单的批量取消注册
    public void UnregisterAllFromGameStart()
    {
        if (!_isRegisteredToGameStart)
            return;

        foreach (var provider in _providers.Values)
        {
            provider.UnregisterFromGameStart();
        }

        _isRegisteredToGameStart = false;
    }

    // 添加提供者的便捷方法
    public void AddProvider<T>(T provider)
        where T : ITrackInputProvider
    {
        _providers[typeof(T)] = provider;
    }

    // 添加提供者的非泛型版本
    public void AddProvider(Type providerType, ITrackInputProvider provider)
    {
        _providers[providerType] = provider;
    }

    // 获取特定类型的提供者
    public T GetProvider<T>()
        where T : ITrackInputProvider
    {
        if (_providers.TryGetValue(typeof(T), out var provider))
        {
            return (T)provider;
        }
        return default;
    }

    // 非泛型版本 - 主要使用这个
    public ITrackInputProvider GetCurrentProvider()
    {
        return _currentProvider;
    }

    // 获取所有提供者
    public IEnumerable<ITrackInputProvider> GetAllProviders() => _providers.Values;

    // 切换当前激活的提供者
    public void SwitchToProvider<T>()
        where T : ITrackInputProvider
    {
        _currentProvider?.Disable();

        if (_providers.TryGetValue(typeof(T), out var newProvider))
        {
            _currentProvider = newProvider;

            _currentProvider.Enable();

            OnInputProviderChange?.Invoke();
        }
    }
}
