using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoBehaviour
{
    [Header("玩家血量UI")]
    [SerializeField]
    private Slider playerHealthSlider;

    [Header("敌人血量UI")]
    [SerializeField]
    private Slider enemyHealthSlider;

    private GameHealthViewModel _viewModel;

    void Start()
    {
        _viewModel = GetComponent<GameHealthViewModel>();

        if (_viewModel == null)
        {
            _viewModel = gameObject.AddComponent<GameHealthViewModel>();
        }

        InitializeUI();
        BindEvents();
    }

    void InitializeUI()
    {
        // 初始化滑块
        playerHealthSlider.minValue = 0;
        playerHealthSlider.maxValue = 1;
        enemyHealthSlider.minValue = 0;
        enemyHealthSlider.maxValue = 1;
    }

    void BindEvents()
    {
        // 绑定ViewModel事件
        _viewModel.OnPlayerHealthChanged += UpdatePlayerHealth;
        _viewModel.OnEnemyHealthChanged += UpdateEnemyHealth;

        // 初始更新
        UpdatePlayerHealth(_viewModel.PlayerHealthPercent);
        UpdateEnemyHealth(_viewModel.EnemyHealthPercent);
    }

    void UpdatePlayerHealth(float percent)
    {
        playerHealthSlider.value = percent;
    }

    void UpdateEnemyHealth(float percent)
    {
        enemyHealthSlider.value = percent;
    }

    void OnDestroy()
    {
        // 清理事件绑定
        if (_viewModel != null)
        {
            _viewModel.OnPlayerHealthChanged -= UpdatePlayerHealth;
            _viewModel.OnEnemyHealthChanged -= UpdateEnemyHealth;
        }
    }
}
