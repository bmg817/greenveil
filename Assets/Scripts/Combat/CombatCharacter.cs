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
        [SerializeField] private float maxMP = 100f;
        [SerializeField] private float currentMP;
        
        [SerializeField] private float attack = 10f;
        [SerializeField] private float defense = 5f;
        [SerializeField] private int speed = 50; // Used for turn order
        
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
            // Initialize health and MP to max
            currentHealth = maxHealth;
            currentMP = maxMP; // ✅ FIXED: Start combat with FULL MP (decreases when using abilities)
        }

        #region Health Management
        /// <summary>
        /// Apply damage to this character
        /// </summary>
        public void TakeDamage(float damage, ElementType damageElement = ElementType.Neutral)
        {
            if (!isAlive) return;

            // Apply defense reduction
            float actualDamage = Mathf.Max(1f, damage - defense);
            
            // TODO: Apply elemental weakness/resistance modifiers here
            // actualDamage = ApplyElementalModifier(actualDamage, damageElement);
            
            currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            Debug.Log($"{characterName} took {actualDamage} damage. HP: {currentHealth}/{maxHealth}");
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal this character
        /// </summary>
        public void Heal(float amount)
        {
            if (!isAlive) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            Debug.Log($"{characterName} healed for {amount}. HP: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Handle character death
        /// </summary>
        private void Die()
        {
            isAlive = false;
            OnCharacterDefeated?.Invoke();
            Debug.Log($"{characterName} has been defeated!");
        }

        /// <summary>
        /// Revive character (for abilities like Pip's "Signed, With Care")
        /// </summary>
        public void Revive(float healthPercent = 0.5f)
        {
            if (isAlive) return;
            
            isAlive = true;
            currentHealth = maxHealth * healthPercent;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            Debug.Log($"{characterName} has been revived with {currentHealth} HP!");
        }
        #endregion

        #region MP Management
        /// <summary>
        /// Restore MP (typically from basic attacks)
        /// </summary>
        public void RestoreMP(float amount)
        {
            currentMP = Mathf.Min(maxMP, currentMP + amount);
            OnMPChanged?.Invoke(currentMP, maxMP);
            
            Debug.Log($"{characterName} restored {amount} MP. MP: {currentMP}/{maxMP}");
        }

        /// <summary>
        /// Consume MP for using skills
        /// </summary>
        public bool ConsumeMP(float amount)
        {
            if (currentMP < amount)
            {
                Debug.LogWarning($"{characterName} doesn't have enough MP! Need {amount}, have {currentMP}");
                return false;
            }
            
            currentMP -= amount;
            OnMPChanged?.Invoke(currentMP, maxMP);
            
            Debug.Log($"{characterName} used {amount} MP. MP: {currentMP}/{maxMP}");
            return true;
        }

        /// <summary>
        /// Check if character has enough MP for an ability
        /// </summary>
        public bool HasEnoughMP(float required)
        {
            return currentMP >= required;
        }
        #endregion

        #region Status Effects
        /// <summary>
        /// Apply a status effect to this character
        /// </summary>
        public void ApplyStatusEffect(StatusEffect effect)
        {
            activeStatusEffects.Add(effect);
            OnStatusEffectApplied?.Invoke(effect);
            
            Debug.Log($"{characterName} is affected by {effect.EffectName}");
        }

        /// <summary>
        /// Remove a specific status effect
        /// </summary>
        public void RemoveStatusEffect(StatusEffect effect)
        {
            activeStatusEffects.Remove(effect);
        }

        /// <summary>
        /// Clear all status effects (for abilities like Miri's Petal Draught)
        /// </summary>
        public void ClearAllStatusEffects()
        {
            activeStatusEffects.Clear();
        }

        /// <summary>
        /// Process status effects at start of turn
        /// </summary>
        public void ProcessStatusEffects()
        {
            for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = activeStatusEffects[i];
                effect.ProcessEffect(this);
                
                // Remove expired effects
                if (effect.IsExpired)
                {
                    activeStatusEffects.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Stat Modifiers (for buffs/debuffs)
        private float attackModifier = 1f;
        private float defenseModifier = 1f;
        private float speedModifier = 1f;

        public void ModifyAttack(float multiplier, int duration)
        {
            attackModifier = multiplier;
            // TODO: Implement duration tracking
        }

        public void ModifyDefense(float multiplier, int duration)
        {
            defenseModifier = multiplier;
        }

        public void ModifySpeed(float multiplier, int duration)
        {
            speedModifier = multiplier;
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
        #endregion
    }
}