using System;
using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    public float MaxHP { get; private set; } = 100;
    public float MaxMP { get; private set; } = 50;

    public float CurrentHP { get; private set; }
    public float CurrentMP { get; private set; }

    public event Action<float> OnHealthChanged;
    public event Action<float> OnMPChanged;
    public event Action<StatusEffectData[]> OnStatusEffectsChanged;

    private void Awake()
    {
        CurrentHP = MaxHP;
        CurrentMP = MaxMP;
    }

    public void TakeDamage(float amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHealthChanged?.Invoke(CurrentHP);
    }

    public bool UseMP(float amount)
    {
        if (CurrentMP < amount)
            return false;

        CurrentMP -= amount;
        OnMPChanged?.Invoke(CurrentMP);
        return true;
    }

    public void ApplyStatusEffects(StatusEffectData[] effects)
    {
        OnStatusEffectsChanged?.Invoke(effects);
    }
}