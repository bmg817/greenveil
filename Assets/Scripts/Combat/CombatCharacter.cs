using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    /// <summary>
    /// Enum for elemental types in the game
    /// </summary>
    public enum ElementType
    {
        Earth,
        Wind,
        Water,
        Fire,
        Light,
        Neutral
    }

    /// <summary>
    /// Enum for character roles/archetypes
    /// </summary>
    public enum CharacterRole
    {
        Tank,
        DPS,
        Support,
        Hybrid
    }

    /// <summary>
    /// Core character class for combat - used by both player characters and enemies
    /// </summary>
    public class CombatCharacter : MonoBehaviour
    {
        [Header("Character Identity")]
        [SerializeField] private string characterName;
        [SerializeField] private CharacterRole role;
        
        [Header("Combat Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float maxMP = 20f;
        [SerializeField] private float currentMP;
        
        [SerializeField] private float attack = 10f;
        [SerializeField] private float defense = 5f;
        [SerializeField] private int speed = 50;
        
        [SerializeField] private ElementType primaryElement = ElementType.Neutral;
        
        [Header("Status")]
        [SerializeField] private bool isAlive = true;
        [SerializeField] private List<StatusEffect> activeStatusEffects = new List<StatusEffect>();
        
        // Events for UI updates
        public System.Action<float, float> OnHealthChanged;
        public System.Action<float, float> OnMPChanged;
        public System.Action<StatusEffect> OnStatusEffectApplied;
        public System.Action OnCharacterDefeated;

        #region Properties
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
        #endregion

        private void Awake()
        {
            currentHealth = maxHealth;
            currentMP = maxMP;
        }

        /// <summary>
        /// Call this after HUD subscribes to ensure initial values are broadcast
        /// </summary>
        public void BroadcastInitialStats()
        {
            Debug.Log($"[{characterName}] Broadcasting initial stats - HP: {currentHealth}/{maxHealth}, MP: {currentMP}/{maxMP}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnMPChanged?.Invoke(currentMP, maxMP);
        }

        #region Health Management
        public void TakeDamage(float damage, ElementType damageElement = ElementType.Neutral)
        {
            if (!isAlive) return;

            float actualDamage = Mathf.Max(1f, damage - defense);
            currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
            
            Debug.Log($"[{characterName}] TakeDamage: {actualDamage} damage. HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive) return;
            
            float previousHP = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float actualHeal = currentHealth - previousHP;
            
            Debug.Log($"[{characterName}] Heal: +{actualHeal} HP. HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            isAlive = false;
            OnCharacterDefeated?.Invoke();
            Debug.Log($"[{characterName}] has been defeated!");
        }

        public void Revive(float healthPercent = 0.5f)
        {
            if (isAlive) return;
            
            isAlive = true;
            currentHealth = maxHealth * healthPercent;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            Debug.Log($"[{characterName}] has been revived with {currentHealth} HP!");
        }
        #endregion

        #region MP Management
        public void RestoreMP(float amount)
        {
            float previousMP = currentMP;
            currentMP = Mathf.Min(maxMP, currentMP + amount);
            float actualRestore = currentMP - previousMP;
            
            Debug.Log($"[{characterName}] RestoreMP: +{actualRestore} MP. MP: {currentMP}/{maxMP}");
            
            OnMPChanged?.Invoke(currentMP, maxMP);
        }

        public bool ConsumeMP(float amount)
        {
            if (currentMP < amount)
            {
                Debug.LogWarning($"[{characterName}] Not enough MP! Need {amount}, have {currentMP}");
                return false;
            }
            
            currentMP -= amount;
            Debug.Log($"[{characterName}] ConsumeMP: -{amount} MP. MP: {currentMP}/{maxMP}");
            OnMPChanged?.Invoke(currentMP, maxMP);
            
            return true;
        }

        public bool ConsumeMPPercent(float percent)
        {
            float amount = maxMP * (percent / 100f);
            return ConsumeMP(amount);
        }

        public bool HasEnoughMP(float required)
        {
            return currentMP >= required;
        }

        public bool HasEnoughMPPercent(float percent)
        {
            float required = maxMP * (percent / 100f);
            return currentMP >= required;
        }

        public float GetMPCost(float percent)
        {
            return maxMP * (percent / 100f);
        }
        #endregion

        #region Status Effects
        public void ApplyStatusEffect(StatusEffect effect)
        {
            activeStatusEffects.Add(effect);
            OnStatusEffectApplied?.Invoke(effect);
            Debug.Log($"[{characterName}] Status applied: {effect.EffectName}");
        }

        public void RemoveStatusEffect(StatusEffect effect)
        {
            activeStatusEffects.Remove(effect);
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
                StatusEffect effect = activeStatusEffects[i];
                effect.ProcessEffect(this);
                
                if (effect.IsExpired)
                {
                    activeStatusEffects.RemoveAt(i);
                    Debug.Log($"[{characterName}] Status expired: {effect.EffectName}");
                }
            }
        }

        public bool HasStatusEffect(StatusEffectType type)
        {
            return activeStatusEffects.Exists(e => e.EffectType == type);
        }
        #endregion

        #region Stat Modifiers
        private float attackModifier = 1f;
        private float defenseModifier = 1f;
        private float speedModifier = 1f;

        public void ModifyAttack(float multiplier, int duration)
        {
            attackModifier = multiplier;
        }

        public void ModifyDefense(float multiplier, int duration)
        {
            defenseModifier = multiplier;
        }

        public void ModifySpeed(float multiplier, int duration)
        {
            speedModifier = multiplier;
        }

        public void ResetModifiers()
        {
            attackModifier = 1f;
            defenseModifier = 1f;
            speedModifier = 1f;
        }

        public float GetModifiedAttack() => attack * attackModifier;
        public float GetModifiedDefense() => defense * defenseModifier;
        public int GetModifiedSpeed() => Mathf.RoundToInt(speed * speedModifier);
        #endregion

        #region Debug
        public void PrintStats()
        {
            Debug.Log($"=== {characterName} Stats ===");
            Debug.Log($"Role: {role}");
            Debug.Log($"HP: {currentHealth}/{maxHealth}");
            Debug.Log($"MP: {currentMP}/{maxMP}");
            Debug.Log($"ATK: {attack} | DEF: {defense} | SPD: {speed}");
            Debug.Log($"Element: {primaryElement}");
            Debug.Log($"Alive: {isAlive}");
        }

        [ContextMenu("Test MP Consume 25%")]
        public void TestConsumeMPInEditor()
        {
            ConsumeMPPercent(25f);
        }

        [ContextMenu("Test MP Restore 20%")]
        public void TestRestoreMPInEditor()
        {
            float amount = maxMP * 0.2f;
            RestoreMP(amount);
        }
        #endregion
    }
}