using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    public enum CharacterRole { Damage, Tank, Support, Healer }
    public enum ElementType { Neutral, Fire, Water, Earth, Air, Light, Dark, Nature }

    public class CombatCharacter : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private CharacterRole role = CharacterRole.Damage;

        [Header("Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxMP = 20f;
        [SerializeField] private float attack = 10f;
        [SerializeField] private float defense = 5f;
        [SerializeField] private int speed = 50;
        [SerializeField] private ElementType primaryElement = ElementType.Neutral;

        [Header("Runtime")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float currentMP;
        [SerializeField] private bool isAlive = true;
        [SerializeField] private List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

        public System.Action<float, float> OnHealthChanged;
        public System.Action<float, float> OnMPChanged;
        public System.Action<StatusEffect> OnStatusEffectApplied;
        public System.Action OnCharacterDefeated;

        public string CharacterName => characterName;
        public CharacterRole Role => role;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentMP => currentMP;
        public float MaxMP => maxMP;
        public float Attack => attack;
        public float Defense => defense;
        public int Speed => speed;
        public ElementType PrimaryElement => primaryElement;
        public bool IsAlive => isAlive;
        public List<StatusEffect> ActiveStatusEffects => activeStatusEffects;

        private void Awake()
        {
            currentHealth = maxHealth;
            currentMP = maxMP;
            Debug.Log($"[{characterName}] Initialized - HP: {currentHealth}/{maxHealth}, MP: {currentMP}/{maxMP}");
        }

        public void TakeDamage(float damage, ElementType damageElement = ElementType.Neutral)
        {
            float finalDamage = Mathf.Max(0, damage - defense);
            currentHealth = Mathf.Max(0, currentHealth - finalDamage);
            Debug.Log($"[{characterName}] TakeDamage: {finalDamage} damage. HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0 && isAlive)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float actualHeal = currentHealth - previousHealth;
            Debug.Log($"[{characterName}] Healed: +{actualHeal}. HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            isAlive = false;
            Debug.Log($"[{characterName}] has been defeated!");
            OnCharacterDefeated?.Invoke();
        }

        public void Revive(float healthPercent = 0.5f)
        {
            if (isAlive) return;
            isAlive = true;
            currentHealth = maxHealth * Mathf.Clamp01(healthPercent);
            Debug.Log($"[{characterName}] Revived with {currentHealth}/{maxHealth} HP");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void RestoreMP(float amount)
        {
            float previousMP = currentMP;
            currentMP = Mathf.Min(maxMP, currentMP + amount);
            float actualRestore = currentMP - previousMP;
            Debug.Log($"[{characterName}] RestoreMP: +{actualRestore}. MP: {currentMP}/{maxMP}");
            OnMPChanged?.Invoke(currentMP, maxMP);
        }

        public bool ConsumeMP(float amount)
        {
            if (currentMP < amount)
            {
                Debug.Log($"[{characterName}] Not enough MP! Has {currentMP}, needs {amount}");
                return false;
            }
            currentMP -= amount;
            Debug.Log($"[{characterName}] ConsumeMP: -{amount}. MP: {currentMP}/{maxMP}");
            OnMPChanged?.Invoke(currentMP, maxMP);
            return true;
        }

        public float GetMPCost(float percent)
        {
            return maxMP * (percent / 100f);
        }

        public bool HasEnoughMP(float percent)
        {
            return currentMP >= GetMPCost(percent);
        }

        public bool ConsumeMPPercent(float percent)
        {
            return ConsumeMP(GetMPCost(percent));
        }

        public void RestoreMPPercent(float percent)
        {
            RestoreMP(GetMPCost(percent));
        }

        public float GetModifiedAttack()
        {
            float mod = attack;
            foreach (var effect in activeStatusEffects)
                mod *= effect.GetStatModifier("attack");
            return mod;
        }

        public float GetModifiedDefense()
        {
            float mod = defense;
            foreach (var effect in activeStatusEffects)
                mod *= effect.GetStatModifier("defense");
            return mod;
        }

        public int GetModifiedSpeed()
        {
            float mod = speed;
            foreach (var effect in activeStatusEffects)
                mod *= effect.GetStatModifier("speed");
            return Mathf.RoundToInt(mod);
        }

        public void ApplyStatusEffect(StatusEffect effect)
        {
            activeStatusEffects.Add(effect);
            Debug.Log($"[{characterName}] Status applied: {effect.EffectName}");
            OnStatusEffectApplied?.Invoke(effect);
        }

        public void RemoveStatusEffect(StatusEffect effect)
        {
            if (activeStatusEffects.Remove(effect))
            {
                Debug.Log($"[{characterName}] Status removed: {effect.EffectName}");
            }
        }

        public void ClearAllStatusEffects()
        {
            activeStatusEffects.Clear();
            Debug.Log($"[{characterName}] All status effects cleared");
        }

        public void ProcessStatusEffects()
        {
            for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeStatusEffects[i];
                effect.ProcessEffect(this);
                if (effect.IsExpired)
                {
                    activeStatusEffects.RemoveAt(i);
                    Debug.Log($"[{characterName}] Status expired: {effect.EffectName}");
                }
            }
        }

        public bool HasStatusEffect(string effectName)
        {
            return activeStatusEffects.Exists(e => e.EffectName == effectName);
        }

        public void PrintStats()
        {
            Debug.Log($"=== {characterName} ===");
            Debug.Log($"HP: {currentHealth}/{maxHealth}");
            Debug.Log($"MP: {currentMP}/{maxMP}");
            Debug.Log($"ATK: {attack} | DEF: {defense} | SPD: {speed}");
        }
    }
}