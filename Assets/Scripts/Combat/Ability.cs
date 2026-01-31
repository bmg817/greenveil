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

    [System.Serializable]
    public class Ability
    {
        public string id;
        public string abilityName;
        public string description;
        public AbilityType abilityType;
        public TargetType targetType;
        public float mpCostPercent;
        public float mpRestorePercent;
        public float hpCostPercent;
        public float basePower;
        public ElementType element;
        public int duration;
        public StatusEffectType statusEffect;
        public float statusChance;
        public bool isMultiHit;
        public int hitCount = 1;
        public bool canCrit = true;
        public float critChance = 0.1f;
        public float critMultiplier = 1.5f;
        public bool isCleanse;
        public StatusEffectType statusEffect2;
        public float statusChance2;
        public int duration2;
        public StatusEffectType selfStatusEffect;
        public int selfDuration;
        public float selfMagnitude;

        public string AbilityName => abilityName;
        public string Description => description;
        public AbilityType Type => abilityType;
        public TargetType Target => targetType;
        public float MPCostPercent => mpCostPercent;
        public float MPRestorePercent => mpRestorePercent;
        public float BasePower => basePower;
        public ElementType Element => element;
        public bool CostsMP => mpCostPercent > 0f;
        public bool RestoresMP => mpRestorePercent > 0f;

        public bool CanUse(CombatCharacter user)
        {
            if (!user.IsAlive) return false;

            if (CostsMP)
            {
                float mpCost = user.MaxMP * (mpCostPercent / 100f);
                if (user.CurrentMP < mpCost)
                {
                    Debug.Log($"[{user.CharacterName}] Cannot use {abilityName}: Not enough MP ({user.CurrentMP:F1}/{mpCost:F1} needed)");
                    return false;
                }
            }

            if (hpCostPercent > 0)
            {
                float hpCost = user.MaxHealth * (hpCostPercent / 100f);
                if (user.CurrentHealth <= hpCost) return false;
            }

            return true;
        }

        public void Use(CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!CanUse(user)) return;

            if (CostsMP)
            {
                float mpCost = user.MaxMP * (mpCostPercent / 100f);
                user.ConsumeMP(mpCost);
            }
            else if (RestoresMP)
            {
                float mpRestore = user.MaxMP * (mpRestorePercent / 100f);
                user.RestoreMP(mpRestore);
            }

            if (hpCostPercent > 0)
            {
                float hpCost = user.MaxHealth * (hpCostPercent / 100f);
                user.TakeDamage(hpCost, ElementType.Neutral, true);
            }

            Debug.Log($">>> {user.CharacterName} uses {abilityName}! <<<");

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
                    Debug.Log($"[UTILITY] {abilityName} executed!");
                    break;
            }

            if (selfDuration > 0)
            {
                user.ApplyStatusEffect(new StatusEffect(selfStatusEffect, selfDuration, selfMagnitude));
                Debug.Log($"[SELF] {user.CharacterName} gains {selfStatusEffect} for {selfDuration} turns");
            }
        }

        private bool CheckAccuracy(CombatCharacter user)
        {
            if (user.HasStatusEffectType(StatusEffectType.AccuracyDown) && Random.value < 0.3f)
            {
                Debug.Log($"[MISS] {user.CharacterName}'s attack missed!");
                return false;
            }
            return true;
        }

        private void ExecuteBasicAttack(CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!CheckAccuracy(user)) return;

            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                float damage = CalculateDamage(user);
                target.TakeDamage(damage, element, false, user);
                Debug.Log($"[BASIC ATTACK] {user.CharacterName} hits {target.CharacterName} for {damage:F0}");
            }
        }

        private void ExecuteDamageSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!CheckAccuracy(user)) return;

            int hits = isMultiHit ? hitCount : 1;

            for (int h = 0; h < hits; h++)
            {
                foreach (var target in targets)
                {
                    if (!target.IsAlive) continue;

                    float damage = CalculateDamage(user);

                    if (canCrit && Random.value < critChance)
                    {
                        damage *= critMultiplier;
                        Debug.Log("[CRITICAL HIT!]");
                    }

                    target.TakeDamage(damage, element, false, user);
                    Debug.Log($"[DAMAGE SKILL] {user.CharacterName} hits {target.CharacterName} for {damage:F0}");

                    if (statusChance > 0 && Random.value < statusChance)
                    {
                        target.ApplyStatusEffect(new StatusEffect(statusEffect, duration));
                        Debug.Log($"[STATUS] {target.CharacterName} is now {statusEffect}!");
                    }

                    if (statusChance2 > 0 && Random.value < statusChance2)
                    {
                        target.ApplyStatusEffect(new StatusEffect(statusEffect2, duration2));
                        Debug.Log($"[STATUS] {target.CharacterName} is now {statusEffect2}!");
                    }
                }
            }
        }

        private void ExecuteHealSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                target.Heal(basePower);
                Debug.Log($"[HEAL] {target.CharacterName} healed for {basePower:F0} HP");

                if (isCleanse)
                {
                    target.ClearAllStatusEffects();
                    Debug.Log($"[CLEANSE] {target.CharacterName}'s status effects removed");
                }
            }
        }

        private void ExecuteBuffSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                target.ApplyStatusEffect(new StatusEffect(statusEffect, duration, basePower));
                Debug.Log($"[BUFF] {target.CharacterName} gains {statusEffect} for {duration} turns");
            }
        }

        private void ExecuteDebuffSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;

                if (statusChance > 0 && Random.value <= statusChance)
                {
                    target.ApplyStatusEffect(new StatusEffect(statusEffect, duration, basePower));
                    Debug.Log($"[DEBUFF] {target.CharacterName} suffers {statusEffect} for {duration} turns");
                }
                else if (statusChance > 0)
                {
                    Debug.Log($"[DEBUFF] {statusEffect} missed {target.CharacterName}!");
                }

                if (statusChance2 > 0 && Random.value <= statusChance2)
                {
                    target.ApplyStatusEffect(new StatusEffect(statusEffect2, duration2));
                    Debug.Log($"[DEBUFF] {target.CharacterName} suffers {statusEffect2} for {duration2} turns");
                }
                else if (statusChance2 > 0)
                {
                    Debug.Log($"[DEBUFF] {statusEffect2} missed {target.CharacterName}!");
                }
            }
        }

        private void ExecuteReviveSkill(CombatCharacter user, List<CombatCharacter> targets)
        {
            foreach (var target in targets)
            {
                if (target.IsAlive) continue;
                target.Revive(basePower / 100f);
                Debug.Log($"[REVIVE] {target.CharacterName} has been revived!");
            }
        }

        private float CalculateDamage(CombatCharacter user)
        {
            return basePower + user.GetModifiedAttack();
        }
    }
}
