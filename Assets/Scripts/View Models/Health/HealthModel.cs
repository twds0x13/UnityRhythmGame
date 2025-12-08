using UnityEngine;

public class HealthModel
{
    private float _currentHealth;

    private readonly float _maxHealth;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    public float HealthPercentage => _currentHealth / _maxHealth;

    public System.Action<float> OnHealthChanged; // 血量变化事件

    public HealthModel(float maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
    }

    public void ChangeHealth(float amount)
    {
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0, _maxHealth);
        OnHealthChanged?.Invoke(HealthPercentage);
    }
}
