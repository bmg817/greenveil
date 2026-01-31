using UnityEngine;

namespace Greenveil.Combat
{
    public enum StatusEffectType
    {
        Poisoned,
        Burning,
        Sleeping,
        Paralyzed,
        Frozen,
        Confused,
        Rooted,
        Weakened,
        Sneeze,
        AccuracyDown,
        Taunting,
        DamageAbsorb,
        AttackBuff,
        DefenseBuff,
        SpeedBuff,
        Shielded,
        Evading,
        Marked,
        HitShield,
        FlowerTrap,
        DamageReflect
    }

    [System.Serializable]
    public class StatusEffect
    {
        [SerializeField] private string effectName;
        [SerializeField] private StatusEffectType effectType;
        [SerializeField] private int duration;
        [SerializeField] private float magnitude;

        private int currentDuration;

        public string EffectName => effectName;
        public StatusEffectType EffectType => effectType;
        public int Duration => duration;
        public float Magnitude => magnitude;
        public bool IsExpired => currentDuration <= 0;

        public StatusEffect(StatusEffectType type, int duration, float magnitude = 1f)
        {
            effectType = type;
            effectName = type.ToString();
            this.duration = duration;
            currentDuration = duration;
            this.magnitude = magnitude;
        }

        public void ProcessEffect(CombatCharacter target)
        {
            switch (effectType)
            {
                case StatusEffectType.Poisoned:
                    float poisonDamage = target.MaxHealth * 0.05f * magnitude;
                    target.TakeDamage(poisonDamage, ElementType.Neutral, true);
                    Debug.Log($"{target.CharacterName} took {poisonDamage:F0} poison damage!");
                    break;

                case StatusEffectType.Burning:
                    float burnDamage = 5f * magnitude;
                    target.TakeDamage(burnDamage, ElementType.Fire, true);
                    Debug.Log($"{target.CharacterName} took {burnDamage:F0} burn damage!");
                    break;
            }

            currentDuration--;
        }

        public bool PreventsAction()
        {
            switch (effectType)
            {
                case StatusEffectType.Sleeping:
                case StatusEffectType.Frozen:
                    return true;
                case StatusEffectType.Paralyzed:
                    return Random.value < 0.5f;
                case StatusEffectType.Sneeze:
                    return Random.value < 0.3f;
                default:
                    return false;
            }
        }

        public void DecrementMagnitude()
        {
            magnitude -= 1f;
        }

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
                case StatusEffectType.Rooted:
                    return statName == "speed" ? 0f : 1f;
                default:
                    return 1f;
            }
        }
    }
}
