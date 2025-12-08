using UnityEngine;

/// <summary>
/// 能量数据模型
/// </summary>
public class EnergyModel
{
    private float _currentEnergy;
    private readonly float _maxEnergy;

    public float MaxEnergy => _maxEnergy;
    public float CurrentEnergy => _currentEnergy;
    public float EnergyPercentage => _currentEnergy / _maxEnergy;
    public bool IsEnergyFull => Mathf.Approximately(_currentEnergy, _maxEnergy);

    // 能量变化事件
    public System.Action<float> OnEnergyChanged;

    // 能量充满事件
    public System.Action OnEnergyFull;

    // 能量从充满状态改变事件
    public System.Action OnEnergyNoLongerFull;

    private bool _wasEnergyFull = false;

    public EnergyModel(float maxEnergy)
    {
        _maxEnergy = maxEnergy;
        _currentEnergy = 0f; // 初始能量为空
    }

    /// <summary>
    /// 改变能量值
    /// </summary>
    public void ChangeEnergy(float amount)
    {
        float previousEnergy = _currentEnergy;
        _currentEnergy = Mathf.Clamp(_currentEnergy + amount, 0, _maxEnergy);

        // 触发能量变化事件
        OnEnergyChanged?.Invoke(EnergyPercentage);

        // 检查能量充满状态变化
        bool isNowFull = IsEnergyFull;

        if (isNowFull && !_wasEnergyFull)
        {
            // 能量刚刚充满
            OnEnergyFull?.Invoke();
            _wasEnergyFull = true;
        }
        else if (!isNowFull && _wasEnergyFull)
        {
            // 能量从充满状态减少
            OnEnergyNoLongerFull?.Invoke();
            _wasEnergyFull = false;
        }
    }

    /// <summary>
    /// 重置能量（使用技能后清空）
    /// </summary>
    public void ResetEnergy()
    {
        ChangeEnergy(-_currentEnergy);
    }

    /// <summary>
    /// 直接设置能量百分比
    /// </summary>
    public void SetEnergyPercentage(float percentage)
    {
        float targetEnergy = Mathf.Clamp01(percentage) * _maxEnergy;
        ChangeEnergy(targetEnergy - _currentEnergy);
    }
}
