using System;
using UnityEngine;

[Serializable]
public class CharacterData : MonoBehaviour
{
    public event Action<float> OnDamaged;
    public event Action<float> OnRecover;
    public event Action<float, float> OnHealthChanged;

    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float coin = 0f;

    public float Coin
    {
        get => coin;
        set => coin = value;
    }

    public float MaxHealth
    {
        get => maxHealth;
        set
        {
            maxHealth = Mathf.Max(1f, value);
            health = Mathf.Clamp(health, 0f, maxHealth);
            OnHealthChanged?.Invoke(health, maxHealth);
        }
    }

    public float Health
    {
        get => health;
        set
        {
            health = Mathf.Clamp(value, 0f, MaxHealth);
            OnHealthChanged?.Invoke(health, MaxHealth);
        }
    }

    public Vector3 Position => transform.position;

    public void TakeDamage(float amount)
    {
        Health -= amount;
        OnDamaged?.Invoke(amount);
    }

    public void Recover(float amount)
    {
        Health += amount;
        OnRecover?.Invoke(amount);
    }

    public CharacterSaveData ExportSaveData()
    {
        return new CharacterSaveData
        {
            health = Health,
            maxHealth = MaxHealth,
            coin = Coin,
            position = Position
        };
    }

    public void ApplySaveData(CharacterSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        MaxHealth = saveData.maxHealth;
        Coin = saveData.coin;
        transform.position = saveData.position;
        Health = saveData.health;
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        health = Mathf.Clamp(health, 0f, maxHealth);
    }
}
