using UnityEngine;

namespace Greenveil.Combat
{
    /// <summary>
    /// Types of status effects that can be applied
    /// </summary>
    public enum StatusEffectType
    {
        // Negative Effects
        Paralyzed,
        Poisoned,
        Sleeping,
        Confused,
        Rooted,
        Disoriented,
        Melancholy, // From Memory Storm boss
        
        // Positive Effects
        AttackBuff,
        DefenseBuff,
        SpeedBuff,
        Shielded,
        Evading,
        
        // Special
        Burning,
        Frozen,
        Weakened
    }

    /// <summary>
    /// Base class for status effects
    /// </summary>
    [System.Serializable]
    public class StatusEffect
    {
        [SerializeField] private string effectName;
        [SerializeField] private StatusEffectType effectType;
        [SerializeField] private int duration; // In turns
        [SerializeField] private float magnitude; // Strength of the effect
        
        private int currentDuration;

        public string EffectName => effectName;
        public StatusEffectType EffectType => effectType;
        public int Duration => duration;
        public bool IsExpired => currentDuration <= 0;

        public StatusEffect(StatusEffectType type, int duration, float magnitude = 1f)
        {
            this.effectType = type;
            this.effectName = type.ToString();
            this.duration = duration;
            this.currentDuration = duration;
            this.magnitude = magnitude;
        }

        /// <summary>
        /// Process this effect at the start of a character's turn
        /// </summary>
        public virtual void ProcessEffect(CombatCharacter target)
        {
            switch (effectType)
            {
                case StatusEffectType.Poisoned:
                    // Deal damage over time
                    float poisonDamage = target.MaxHealth * 0.05f * magnitude;
                    target.TakeDamage(poisonDamage);
                    Debug.Log($"{target.CharacterName} took {poisonDamage} poison damage!");
                    break;
                    
                case StatusEffectType.Paralyzed:
                    // Chance to skip turn handled in TurnOrderManager
                    Debug.Log($"{target.CharacterName} is paralyzed!");
                    break;
                    
                case StatusEffectType.Sleeping:
                    // Skip turn
                    Debug.Log($"{target.CharacterName} is sleeping...");
                    break;
                    
                case StatusEffectType.Confused:
                    // Random action or attack allies
                    Debug.Log($"{target.CharacterName} is confused!");
                    break;
                    
                case StatusEffectType.Burning:
                    // Fire damage over time
                    float burnDamage = 5f * magnitude;
                    target.TakeDamage(burnDamage, ElementType.Fire);
                    Debug.Log($"{target.CharacterName} took {burnDamage} burn damage!");
                    break;
                    
                case StatusEffectType.Melancholy:
                    // Backlash damage when dealing damage (handled in combat manager)
                    Debug.Log($"{target.CharacterName} feels melancholy...");
                    break;
            }
            
            // Decrease duration
            currentDuration--;
        }

        /// <summary>
        /// Check if this effect prevents action
        /// </summary>
        public bool PreventsAction()
        {
            switch (effectType)
            {
                case StatusEffectType.Sleeping:
                case StatusEffectType.Frozen:
                    return true;
                case StatusEffectType.Paralyzed:
                    // 50% chance to prevent action
                    return Random.value < 0.5f;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get stat modifier from this effect
        /// </summary>
        public float GetStatModifier(string statName)
        {
            switch (effectType)
            {
                case StatusEffectType.AttackBuff:
                    return statName == "attack" ? magnitude : 1f;
                case StatusEffectType.DefenseBuff:
                    return statName == "defense" ? magnitude : 1f;
                case StatusEffectType.SpeedBuff:
                    return statName == "speed" ? magnitude : 1f;
                case StatusEffectType.Weakened:
                    return statName == "attack" ? 0.5f : 1f;
                default:
                    return 1f;
            }
        }
    }
}
