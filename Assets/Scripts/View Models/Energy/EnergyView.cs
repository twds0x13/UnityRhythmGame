using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简化版能量视图
/// 移除了颜色调整和动画相关代码
/// </summary>
public class EnergyView : MonoBehaviour
{
    [Header("能量条UI组件")]
    [SerializeField]
    private Slider energySlider;

    [Header("能量充满提示")]
    [SerializeField]
    private GameObject energyFullIndicator;

    [SerializeField]
    private TextMeshProUGUI skillReadyText;

    private EnergyViewModel _viewModel;

    void Start()
    {
        _viewModel = GetComponent<EnergyViewModel>();

        if (_viewModel == null)
        {
            _viewModel = gameObject.AddComponent<EnergyViewModel>();
        }

        InitializeUI();
        BindEvents();
    }

    void InitializeUI()
    {
        // 初始化滑块
        if (energySlider != null)
        {
            energySlider.minValue = 0;
            energySlider.maxValue = 1;
        }

        // 隐藏充满提示
        if (energyFullIndicator != null)
        {
            energyFullIndicator.SetActive(false);
        }

        if (skillReadyText != null)
        {
            skillReadyText.gameObject.SetActive(false);
        }
    }

    void BindEvents()
    {
        // 绑定ViewModel事件
        _viewModel.OnEnergyChanged += UpdateEnergyDisplay;
        _viewModel.OnEnergyFull += OnEnergyFull;
        _viewModel.OnSkillAvailabilityChanged += OnSkillAvailabilityChanged;

        // 初始更新
        UpdateEnergyDisplay(_viewModel.EnergyPercent);
        OnSkillAvailabilityChanged(_viewModel.CanUseSkill);
    }

    void UpdateEnergyDisplay(float percent)
    {
        // 更新滑块
        if (energySlider != null)
        {
            energySlider.value = percent;
        }
    }

    void OnEnergyFull()
    {
        // 能量充满时显示提示
        if (energyFullIndicator != null)
        {
            energyFullIndicator.SetActive(true);
        }

        if (skillReadyText != null)
        {
            skillReadyText.gameObject.SetActive(true);
        }
    }

    void OnSkillAvailabilityChanged(bool isAvailable)
    {
        // 技能可用状态变化
        if (!isAvailable)
        {
            // 隐藏充满提示
            if (energyFullIndicator != null)
            {
                energyFullIndicator.SetActive(false);
            }

            if (skillReadyText != null)
            {
                skillReadyText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 外部调用：增加能量
    /// </summary>
    public void AddEnergy(float amount)
    {
        _viewModel.AddEnergy(amount);
    }

    /// <summary>
    /// 外部调用：尝试使用技能
    /// </summary>
    public bool TryUseSkill()
    {
        return _viewModel.TryUseSkill();
    }

    /// <summary>
    /// 外部调用：设置能量百分比
    /// </summary>
    public void SetEnergyPercentage(float percentage)
    {
        _viewModel.SetEnergyPercentage(percentage);
    }

    /// <summary>
    /// 获取是否可以使用技能
    /// </summary>
    public bool CanUseSkill()
    {
        return _viewModel.CanUseSkill;
    }

    /// <summary>
    /// 重置能量到0
    /// </summary>
    public void ResetEnergy()
    {
        _viewModel.ResetEnergy();
    }

    void OnDestroy()
    {
        // 清理事件绑定
        if (_viewModel != null)
        {
            _viewModel.OnEnergyChanged -= UpdateEnergyDisplay;
            _viewModel.OnEnergyFull -= OnEnergyFull;
            _viewModel.OnSkillAvailabilityChanged -= OnSkillAvailabilityChanged;
        }
    }
}
