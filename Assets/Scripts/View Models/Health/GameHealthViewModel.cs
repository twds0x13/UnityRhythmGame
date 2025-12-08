using UnityEngine;

public class GameHealthViewModel : MonoBehaviour
{
    // 两个单位的血量模型
    private HealthModel _playerHealth;
    private HealthModel _enemyHealth;

    // 供View绑定的属性
    public float PlayerHealthPercent => _playerHealth?.HealthPercentage ?? 0;
    public float EnemyHealthPercent => _enemyHealth?.HealthPercentage ?? 0;

    // 血量变化事件
    public System.Action<float> OnPlayerHealthChanged;
    public System.Action<float> OnEnemyHealthChanged;

    void Start()
    {
        // 初始化血量（可根据游戏难度调整）
        _playerHealth = new HealthModel(100f);
        _enemyHealth = new HealthModel(500f);

        // 绑定Model变化事件
        _playerHealth.OnHealthChanged += (percent) => OnPlayerHealthChanged?.Invoke(percent);
        _enemyHealth.OnHealthChanged += (percent) => OnEnemyHealthChanged?.Invoke(percent);

        // 触发初始更新
        OnPlayerHealthChanged?.Invoke(PlayerHealthPercent);
        OnEnemyHealthChanged?.Invoke(EnemyHealthPercent);
    }

    // 公共方法供外部调用（游戏逻辑）
    public void PlayerTakeDamage(float damage) => _playerHealth.ChangeHealth(-damage);

    public void EnemyTakeDamage(float damage) => _enemyHealth.ChangeHealth(-damage);

    public void PlayerHeal(float amount) => _playerHealth.ChangeHealth(amount);

    public void EnemyHeal(float amount) => _enemyHealth.ChangeHealth(amount);
}
