using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    public enum ItemType
    {
        HealingItem,
        MPRestoreItem,
        ReviveItem,
        BuffItem,
        CureItem,
        DamageItem,
        EscapeItem
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "Greenveil/Item")]
    public class Item : ScriptableObject
    {
        [SerializeField] private string itemName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private ItemType itemType;
        [SerializeField] private Sprite icon;
        [SerializeField] private TargetType targetType;
        [SerializeField] private float power;
        [SerializeField] private int duration;
        [SerializeField] private StatusEffectType statusEffect;
        [SerializeField] private bool isConsumable = true;

        public string ItemName => itemName;
        public string Description => description;
        public ItemType Type => itemType;
        public Sprite Icon => icon;
        public TargetType Target => targetType;
        public bool IsConsumable => isConsumable;

        public virtual bool UseItem(CombatCharacter user, List<CombatCharacter> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning($"No valid targets for {itemName}!");
                return false;
            }

            Debug.Log($"{user.CharacterName} uses {itemName}!");

            switch (itemType)
            {
                case ItemType.HealingItem:
                    return UseHealingItem(targets);
                case ItemType.MPRestoreItem:
                    return UseMPRestoreItem(targets);
                case ItemType.ReviveItem:
                    return UseReviveItem(targets);
                case ItemType.BuffItem:
                    return UseBuffItem(targets);
                case ItemType.CureItem:
                    return UseCureItem(targets);
                case ItemType.DamageItem:
                    return UseDamageItem(targets);
                case ItemType.EscapeItem:
                    return UseEscapeItem();
                default:
                    Debug.LogWarning($"Unknown item type: {itemType}");
                    return false;
            }
        }

        private bool UseHealingItem(List<CombatCharacter> targets)
        {
            bool success = false;
            foreach (var target in targets)
            {
                if (target.IsAlive && target.CurrentHealth < target.MaxHealth)
                {
                    target.Heal(power);
                    Debug.Log($"{target.CharacterName} healed for {power} HP!");
                    success = true;
                }
            }
            return success;
        }

        private bool UseMPRestoreItem(List<CombatCharacter> targets)
        {
            bool success = false;
            foreach (var target in targets)
            {
                if (target.IsAlive && target.CurrentMP < target.MaxMP)
                {
                    target.RestoreMP(power);
                    Debug.Log($"{target.CharacterName} restored {power} MP!");
                    success = true;
                }
            }
            return success;
        }

        private bool UseReviveItem(List<CombatCharacter> targets)
        {
            bool success = false;
            foreach (var target in targets)
            {
                if (!target.IsAlive)
                {
                    target.Revive(power / 100f);
                    Debug.Log($"{target.CharacterName} has been revived!");
                    success = true;
                }
            }
            return success;
        }

        private bool UseBuffItem(List<CombatCharacter> targets)
        {
            bool success = false;
            foreach (var target in targets)
            {
                if (target.IsAlive)
                {
                    StatusEffect buff = new StatusEffect(statusEffect, duration, power);
                    target.ApplyStatusEffect(buff);
                    Debug.Log($"{target.CharacterName} received {statusEffect} buff!");
                    success = true;
                }
            }
            return success;
        }

        private bool UseCureItem(List<CombatCharacter> targets)
        {
            bool success = false;
            foreach (var target in targets)
            {
                if (target.IsAlive && target.ActiveStatusEffects.Count > 0)
                {
                    target.ClearAllStatusEffects();
                    Debug.Log($"{target.CharacterName}'s status effects removed!");
                    success = true;
                }
            }
            return success;
        }

        private bool UseDamageItem(List<CombatCharacter> targets)
        {
            bool success = false;
            foreach (var target in targets)
            {
                if (target.IsAlive)
                {
                    target.TakeDamage(power);
                    Debug.Log($"{target.CharacterName} took {power} damage from item!");
                    success = true;
                }
            }
            return success;
        }

        private bool UseEscapeItem()
        {
            Debug.Log("Escape item used! Guaranteed escape!");
            return true;
        }
    }
}
