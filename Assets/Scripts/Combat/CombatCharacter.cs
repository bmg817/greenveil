using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    public enum CharacterRole { Damage, Tank, Support, Healer }
    public enum ElementType { Neutral, Fire, Water, Earth, Air, Light, Dark, Nature }

    public class CombatCharacter : MonoBehaviour
    {
        [SerializeField] private string characterId;
        [SerializeField] private float defendMultiplier = 0.5f;

        private string characterName = "Character";
        private CharacterRole role = CharacterRole.Damage;
        private float maxHealth = 100f;
        private float maxMP = 20f;
        private float attack = 10f;
        private float defense = 5f;
        private int speed = 50;
        private ElementType primaryElement = ElementType.Neutral;

        private float currentHealth;
        private float currentMP;
        private bool isAlive = true;
        private bool isDefending;
        private List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

        private Ability basicAttack;
        private Ability[] skills;

        public System.Action<float, float> OnHealthChanged;
        public System.Action<float, float> OnMPChanged;
        public System.Action<StatusEffect> OnStatusEffectApplied;
        public System.Action OnCharacterDefeated;
        public System.Action<CombatCharacter, float> OnFlowerTrapTriggered;
        public System.Action<float> OnDamageTaken;
        public System.Action<float> OnHealReceived;

        public string CharacterName => characterName;
        public string CharacterId => characterId;
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
        public bool IsDefending => isDefending;
        public List<StatusEffect> ActiveStatusEffects => activeStatusEffects;
        public Ability BasicAttack => basicAttack;
        public Ability[] Skills => skills;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(characterId))
                LoadFromConfig(characterId);

            currentHealth = maxHealth;
            currentMP = maxMP;
            Debug.Log($"[{characterName}] Initialized - HP: {currentHealth}/{maxHealth}, MP: {currentMP}/{maxMP}");
        }

        public void LoadFromConfig(string id)
        {
            var config = CombatConfig.GetCharacter(id);
            if (config == null)
            {
                Debug.LogWarning($"Character config not found: {id}");
                return;
            }

            characterId = id;
            characterName = config.characterName;
            role = config.role;
            maxHealth = config.maxHealth;
            maxMP = config.maxMP;
            attack = config.attack;
            defense = config.defense;
            speed = config.speed;
            primaryElement = config.primaryElement;

            if (!string.IsNullOrEmpty(config.basicAttackId))
                basicAttack = CombatConfig.GetAbility(config.basicAttackId);

            skills = CombatConfig.GetAbilitiesForCharacter(id);
        }

        public void TakeDamage(float damage, ElementType damageElement = ElementType.Neutral, bool direct = false, CombatCharacter attacker = null)
        {
            float finalDamage = damage;

            if (!direct)
            {
                var hitShield = GetStatusEffect(StatusEffectType.HitShield);
                if (hitShield != null)
                {
                    hitShield.DecrementMagnitude();
                    Debug.Log($"[{characterName}] HitShield blocked the attack! ({hitShield.Magnitude:F0} hits remaining)");
                    if (hitShield.Magnitude <= 0)
                        RemoveStatusEffect(hitShield);
                    return;
                }

                if (isDefending)
                    finalDamage *= defendMultiplier;

                var marked = GetStatusEffect(StatusEffectType.Marked);
                if (marked != null)
                {
                    finalDamage *= 1.5f;
                    Debug.Log($"[{characterName}] Marked! Taking 1.5x damage!");
                }

                var shielded = GetStatusEffect(StatusEffectType.Shielded);
                if (shielded != null)
                {
                    float absorbed = Mathf.Min(finalDamage, shielded.Magnitude);
                    finalDamage -= absorbed;
                    Debug.Log($"[{characterName}] Shield absorbed {absorbed:F0} damage!");
                }

                var evading = GetStatusEffect(StatusEffectType.Evading);
                if (evading != null && Random.value < evading.Magnitude)
                {
                    Debug.Log($"[{characterName}] Evaded the attack!");
                    return;
                }

                var damageAbsorb = GetStatusEffect(StatusEffectType.DamageAbsorb);
                if (damageAbsorb != null)
                {
                    finalDamage *= 0.5f;
                    Debug.Log($"[{characterName}] DamageAbsorb halved incoming damage!");
                }

                finalDamage = Mathf.Max(1f, finalDamage - GetModifiedDefense());
            }

            currentHealth = Mathf.Max(0, currentHealth - finalDamage);
            Debug.Log($"[{characterName}] TakeDamage: {finalDamage:F0} damage. HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageTaken?.Invoke(finalDamage);

            if (currentHealth <= 0 && isAlive)
                Die();

            if (!direct && isAlive)
            {
                var reflect = GetStatusEffect(StatusEffectType.DamageReflect);
                if (reflect != null && attacker != null)
                {
                    float reflectDamage = finalDamage * reflect.Magnitude;
                    Debug.Log($"[{characterName}] DamageReflect! {reflectDamage:F0} reflected to {attacker.CharacterName}!");
                    attacker.TakeDamage(reflectDamage, damageElement, true);
                }

                var trap = GetStatusEffect(StatusEffectType.FlowerTrap);
                if (trap != null)
                {
                    float trapDamage = trap.Magnitude;
                    Debug.Log($"[{characterName}] FlowerTrap triggered! {trapDamage:F0} bonus Nature damage!");
                    TakeDamage(trapDamage, ElementType.Nature, true);
                    RemoveStatusEffect(trap);
                    OnFlowerTrapTriggered?.Invoke(this, trapDamage * 0.5f);
                }
            }
        }

        public void Heal(float amount)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float actualHeal = currentHealth - previousHealth;
            Debug.Log($"[{characterName}] Healed: +{actualHeal}. HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (actualHeal > 0) OnHealReceived?.Invoke(actualHeal);
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

        public void SetDefending(bool defending)
        {
            isDefending = defending;
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
                Debug.Log($"[{characterName}] Status removed: {effect.EffectName}");
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

        public bool HasStatusEffectType(StatusEffectType type)
        {
            return activeStatusEffects.Exists(e => e.EffectType == type);
        }

        public StatusEffect GetStatusEffect(StatusEffectType type)
        {
            return activeStatusEffects.Find(e => e.EffectType == type);
        }
    }
}
