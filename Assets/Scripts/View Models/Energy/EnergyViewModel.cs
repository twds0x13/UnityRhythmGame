using UnityEngine;

/// <summary>
/// 简化版能量视图模型
/// 功能：只管理能量值，技能消耗100%能量
/// </summary>
public class EnergyViewModel : MonoBehaviour
{
    // 能量模型
    private EnergyModel _energyModel;

    // 供View绑定的属性
    public float EnergyPercent => _energyModel?.EnergyPercentage ?? 0;
    public bool CanUseSkill => _energyModel?.IsEnergyFull ?? false;

    // 能量变化事件
    public System.Action<float> OnEnergyChanged;

    // 能量充满事件
    public System.Action OnEnergyFull;

    // 技能可用状态变化事件
    public System.Action<bool> OnSkillAvailabilityChanged;

    [Header("能量配置")]
    [SerializeField]
    private float _maxEnergy = 100f;

    void Start()
    {
        // 初始化能量模型
        _energyModel = new EnergyModel(_maxEnergy);

        // 绑定Model事件
        _energyModel.OnEnergyChanged += (percent) =>
        {
            OnEnergyChanged?.Invoke(percent);
        };

        _energyModel.OnEnergyFull += () =>
        {
            OnEnergyFull?.Invoke();
            OnSkillAvailabilityChanged?.Invoke(true);
        };

        _energyModel.OnEnergyNoLongerFull += () =>
        {
            OnSkillAvailabilityChanged?.Invoke(false);
        };

        // 触发初始更新
        OnEnergyChanged?.Invoke(EnergyPercent);
        OnSkillAvailabilityChanged?.Invoke(CanUseSkill);
    }

    /// <summary>
    /// 增加能量
    /// </summary>
    public void AddEnergy(float amount)
    {
        _energyModel?.ChangeEnergy(amount);
    }

    /// <summary>
    /// 尝试使用技能（消耗100%能量）
    /// </summary>
    public bool TryUseSkill()
    {
        if (CanUseSkill)
        {
            // 使用技能，清空能量
            _energyModel?.ResetEnergy();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 直接设置能量百分比
    /// </summary>
    public void SetEnergyPercentage(float percentage)
    {
        _energyModel?.SetEnergyPercentage(percentage);
    }

    /// <summary>
    /// 重置能量到0
    /// </summary>
    public void ResetEnergy()
    {
        _energyModel?.ResetEnergy();
    }

    /// <summary>
    /// 获取当前能量值（可选）
    /// </summary>
    public float GetCurrentEnergy()
    {
        return _energyModel?.CurrentEnergy ?? 0;
    }

    /// <summary>
    /// 获取最大能量值（可选）
    /// </summary>
    public float GetMaxEnergy()
    {
        return _energyModel?.MaxEnergy ?? 0;
    }
}
