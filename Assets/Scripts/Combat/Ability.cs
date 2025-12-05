        using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    /// <summary>
    /// Types of abilities
    /// </summary>
    public enum AbilityType
    {
        BasicAttack,    // Restores MP
        DamageSkill,    // Deals damage, costs MP
        HealSkill,      // Heals allies, costs MP
        BuffSkill,      // Buffs allies, costs MP
        DebuffSkill,    // Debuffs enemies, costs MP
        UtilitySkill,   // Special effects, costs MP
        ReviveSkill     // Revives fallen ally
    }

    /// <summary>
    /// Target type for abilities
    /// </summary>
    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self,
        Random
    }

    /// <summary>
    /// Base class for all combat abilities
    /// </summary>
    [CreateAssetMenu(fileName = "New Ability", menuName = "Greenveil/Ability")]
    public class Ability : ScriptableObject
    {
        [Header("Ability Info")]
        [SerializeField] private string abilityName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private AbilityType abilityType;
        [SerializeField] private TargetType targetType;
        
        [Header("Costs")]
        [SerializeField] private float mpCostPercent; // As percentage of max MP (0-100)
        [SerializeField] private float hpCostPercent; // Some abilities cost HP
        
        [Header("Effect Values")]
        [SerializeField] private float basePower; // Base damage/healing amount
        [SerializeField] private ElementType element = ElementType.Neutral;
        [SerializeField] private int duration; // For buffs/debuffs
        
        [Header("Status Effects")]
        [SerializeField] private StatusEffectType statusEffect;
        [SerializeField] private float statusChance = 0f; // 0-1 chance to apply
        
        [Header("Special Mechanics")]
        [SerializeField] private bool isMultiHit = false;
        [SerializeField] private int hitCount = 1;
        [SerializeField] private bool canCrit = true;
        [SerializeField] private float critChance = 0.1f;
        [SerializeField] private float critMultiplier = 1.5f;

        #region Properties
        public string AbilityName => abilityName;
        public string Description => description;
        public AbilityType Type => abilityType;
        public TargetType Target => targetType;
        public float MPCostPercent => mpCostPercent;
        public float HPCostPercent => hpCostPercent;
        public float BasePower => basePower;
        public ElementType Element => element;
        #endregion

        /// <summary>
        /// Check if the user can use this ability
        /// </summary>
        public bool CanUse(CombatCharacter user)
        {
            // Check MP cost
            float mpCost = user.MaxMP * (mpCostPercent / 100f);
            if (!user.HasEnoughMP(mpCost))
            {
                return false;
            }
            
            // Check HP cost
            float hpCost = user.MaxHealth * (hpCostPercent / 100f);
            if (user.CurrentHealth <= hpCost)
            {
                return false;
            }
            
            // Check if user is alive
            if (!user.IsAlive)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Use this ability on target(s)
        /// </summary>
        public virtual void Use(CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!CanUse(user))
            {
                Debug.LogWarning($"{user.CharacterName} cannot use {abilityName}!");
                return;
            }
            
            // Pay costs
            float mpCost = user.MaxMP * (mpCostPercent / 100f);
            float hpCost = user.MaxHealth * (hpCostPercent / 100f);
            
            user.ConsumeMP(mpCost);
            if (hpCost > 0)
            {
                user.TakeDamage(hpCost);
            }
            
            Debug.Log($"{user.CharacterName} uses {abilityName}!");
            
            // Apply effects based on ability type
            switch (abilityType)
            {
                case AbilityType.BasicAttack:
                    ExecuteBasicAttack(user, targets);
                    break;
                case AbilityType.DamageSkill:
                    ExecuteDamageSkill(user, targets);
                    break;
                case AbilityType.HealSkill:
                    ExecuteHealSkill(user, targets);
                    break;
                case AbilityType.BuffSkill:
                    ExecuteBuffSkill(user, targets);
                    break;
                case AbilityType.DebuffSkill:
                    ExecuteDebuffSkill(user, targets);
                    break;
                case AbilityType.ReviveSkill:
                    ExecuteReviveSkill(user, targets);
                    break;
                case AbilityType.UtilitySkill:
                    ExecuteUtilitySkill(user, targets);
                    break;
            }
        }

        #region Ability Execution
        protected virtual void ExecuteBasicAttack(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                
                float damage = CalculateDamage(user, target);
                target.TakeDamage(damage, element);
                
                // Basic attacks restore MP
                float mpRestore = user.MaxMP * 0.20f; // 20% MP restore
                user.RestoreMP(mpRestore);
            }
        }

        protected virtual void ExecuteDamageSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            int hits = isMultiHit ? hitCount : 1;
            
            for (int h = 0; h < hits; h++)
            {
                foreach (var target in targets)
                {
                    if (!target.IsAlive) continue;
                    
                    float damage = CalculateDamage(user, target);
                    
                    // Check for critical hit
                    if (canCrit && Random.value < critChance)
                    {
                        damage *= critMultiplier;
                        Debug.Log("Critical Hit!");
                    }
                    
                    target.TakeDamage(damage, element);
                    
                    // Apply status effect if applicable
                    if (statusChance > 0 && Random.value < statusChance)
                    {
                        StatusEffect status = new StatusEffect(statusEffect, duration);
                        target.ApplyStatusEffect(status);
                    }
                }
            }
        }

        protected virtual void ExecuteHealSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                
                float healAmount = basePower;
                target.Heal(healAmount);
                
                // Clear status effects if specified
                if (statusEffect == StatusEffectType.Poisoned) // Example: clear poison
                {
                    target.ClearAllStatusEffects();
                }
            }
        }

        protected virtual void ExecuteBuffSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                
                StatusEffect buff = new StatusEffect(statusEffect, duration, basePower);
                target.ApplyStatusEffect(buff);
            }
        }

        protected virtual void ExecuteDebuffSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                
                StatusEffect debuff = new StatusEffect(statusEffect, duration, basePower);
                target.ApplyStatusEffect(debuff);
            }
        }

        protected virtual void ExecuteReviveSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive) continue;
                
                target.Revive(basePower); // basePower is the revival HP percentage
                Debug.Log($"{target.CharacterName} has been revived!");
            }
        }

        protected virtual void ExecuteUtilitySkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            // Override in specific ability implementations
            Debug.Log($"Utility skill {abilityName} executed!");
        }
        #endregion

        #region Damage Calculation
        /// <summary>
        /// Calculate damage based on user's attack and target's defense
        /// </summary>
        protected float CalculateDamage(CombatCharacter user, CombatCharacter target)
        {
            float baseDamage = basePower + user.GetModifiedAttack();
            
            // Apply defense reduction
            float damage = Mathf.Max(1f, baseDamage - target.GetModifiedDefense());
            
            // TODO: Apply elemental modifiers
            // damage = ApplyElementalModifier(damage, element, target.PrimaryElement);
            
            return damage;
        }
        #endregion
    }
}
