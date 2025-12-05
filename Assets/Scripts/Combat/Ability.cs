using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    public enum AbilityType
    {
        BasicAttack, 
        DamageSkill, 
        HealSkill, 
        BuffSkill, 
        DebuffSkill, 
        UtilitySkill,
        ReviveSkill
    }

    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self,
        Random
    }

    [CreateAssetMenu(fileName = "New Ability", menuName = "Greenveil/Ability")]
    public class Ability : ScriptableObject
    {
        [Header("Ability Info")]
        [SerializeField] private string abilityName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private AbilityType abilityType;
        [SerializeField] private TargetType targetType;
        
        [Header("MP - Cost OR Restore (use one, not both)")]
        [Tooltip("MP cost as percentage of max MP (0-100). Use for SKILLS.")]
        [SerializeField] private float mpCostPercent = 0f;
        
        [Tooltip("MP restore as percentage of max MP (0-100). Use for BASIC ATTACKS.")]
        [SerializeField] private float mpRestorePercent = 0f;
        
        [Header("HP Cost (optional)")]
        [Tooltip("HP cost as percentage of max HP. Some abilities cost HP to use.")]
        [SerializeField] private float hpCostPercent = 0f;
        
        [Header("Effect Values")]
        [SerializeField] private float basePower;
        [SerializeField] private ElementType element = ElementType.Neutral;
        [SerializeField] private int duration;
        
        [Header("Status Effects")]
        [SerializeField] private StatusEffectType statusEffect;
        [SerializeField] private float statusChance = 0f;
        
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
        public float MPRestorePercent => mpRestorePercent;
        public float HPCostPercent => hpCostPercent;
        public float BasePower => basePower;
        public ElementType Element => element;
        public int Duration => duration;
        public StatusEffectType StatusEffect => statusEffect;
        public float StatusChance => statusChance;
        

        public bool CostsMP => mpCostPercent > 0f;
        

        public bool RestoresMP => mpRestorePercent > 0f;
        #endregion

 
        public bool CanUse(CombatCharacter user)
        {
            if (!user.IsAlive)
            {
                return false;
            }

            if (CostsMP)
            {
                float mpCost = user.MaxMP * (mpCostPercent / 100f);
                if (!user.HasEnoughMP(mpCost))
                {
                    Debug.LogWarning($"[{user.CharacterName}] Cannot use {abilityName}: Not enough MP ({user.CurrentMP:F1}/{mpCost:F1} needed)");
                    return false;
                }
            }
            
            if (hpCostPercent > 0)
            {
                float hpCost = user.MaxHealth * (hpCostPercent / 100f);
                if (user.CurrentHealth <= hpCost)
                {
                    Debug.LogWarning($"[{user.CharacterName}] Cannot use {abilityName}: Not enough HP");
                    return false;
                }
            }
            
            return true;
        }

        public virtual void Use(CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!CanUse(user))
            {
                return;
            }
            
            if (CostsMP)
            {
                float mpCost = user.MaxMP * (mpCostPercent / 100f);
                user.ConsumeMP(mpCost);
                Debug.Log($"[{user.CharacterName}] {abilityName} costs {mpCost:F1} MP ({mpCostPercent}%)");
            }
            else if (RestoresMP)
            {
                float mpRestore = user.MaxMP * (mpRestorePercent / 100f);
                user.RestoreMP(mpRestore);
                Debug.Log($"[{user.CharacterName}] {abilityName} restores {mpRestore:F1} MP ({mpRestorePercent}%)");
            }
            
            if (hpCostPercent > 0)
            {
                float hpCost = user.MaxHealth * (hpCostPercent / 100f);
                user.TakeDamage(hpCost);
                Debug.Log($"[{user.CharacterName}] {abilityName} costs {hpCost:F1} HP");
            }
            
            Debug.Log($">>> {user.CharacterName} uses {abilityName}! <<<");
            
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
                
                Debug.Log($"[BASIC ATTACK] {user.CharacterName} hits {target.CharacterName} for {damage:F0} damage");
                
                // NOTE: MP restore is now handled in Use() method via mpRestorePercent
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
                        Debug.Log($"[CRITICAL HIT!]");
                    }
                    
                    target.TakeDamage(damage, element);
                    Debug.Log($"[DAMAGE SKILL] {user.CharacterName} hits {target.CharacterName} for {damage:F0} damage");
                    
                    // Apply status effect if applicable
                    if (statusChance > 0 && Random.value < statusChance)
                    {
                        StatusEffect status = new StatusEffect(statusEffect, duration);
                        target.ApplyStatusEffect(status);
                        Debug.Log($"[STATUS] {target.CharacterName} is now {statusEffect}!");
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
                Debug.Log($"[HEAL] {target.CharacterName} healed for {healAmount:F0} HP");
                
                // Clear status effects if this is a cleanse ability
                if (statusEffect == StatusEffectType.Poisoned)
                {
                    target.ClearAllStatusEffects();
                    Debug.Log($"[CLEANSE] {target.CharacterName}'s status effects removed");
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
                Debug.Log($"[BUFF] {target.CharacterName} gains {statusEffect} for {duration} turns");
            }
        }

        protected virtual void ExecuteDebuffSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                
                StatusEffect debuff = new StatusEffect(statusEffect, duration, basePower);
                target.ApplyStatusEffect(debuff);
                Debug.Log($"[DEBUFF] {target.CharacterName} suffers {statusEffect} for {duration} turns");
            }
        }

        protected virtual void ExecuteReviveSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive) continue;
                
                target.Revive(basePower / 100f);
                Debug.Log($"[REVIVE] {target.CharacterName} has been revived!");
            }
        }

        protected virtual void ExecuteUtilitySkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            Debug.Log($"[UTILITY] {abilityName} executed!");
        }
        #endregion

        #region Damage Calculation
        protected float CalculateDamage(CombatCharacter user, CombatCharacter target)
        {
            float baseDamage = basePower + user.GetModifiedAttack();
            float damage = Mathf.Max(1f, baseDamage - target.GetModifiedDefense());
            return damage;
        }
        #endregion

        #region Editor Helpers
        public string GetSummary()
        {
            string mpInfo = "";
            if (CostsMP) mpInfo = $"Costs {mpCostPercent}% MP";
            else if (RestoresMP) mpInfo = $"Restores {mpRestorePercent}% MP";
            else mpInfo = "No MP change";
            
            return $"{abilityName} ({abilityType}) - {mpInfo} - Power: {basePower}";
        }
        #endregion
    }
}