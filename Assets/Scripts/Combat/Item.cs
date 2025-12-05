using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    /// <summary>
    /// Types of items that can be used in combat
    /// </summary>
    public enum ItemType
    {
        HealingItem,      // Restores HP
        MPRestoreItem,    // Restores MP
        ReviveItem,       // Revives defeated ally
        BuffItem,         // Applies positive status effect
        CureItem,         // Removes negative status effects
        DamageItem,       // Damages enemies (throwable)
        EscapeItem        // Guarantees escape from battle
    }

    /// <summary>
    /// Consumable items that can be used in combat
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Greenveil/Item")]
    public class Item : ScriptableObject
    {
        [Header("Item Info")]
        [SerializeField] private string itemName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private ItemType itemType;
        [SerializeField] private Sprite icon;
        
        [Header("Target")]
        [SerializeField] private TargetType targetType;
        
        [Header("Effect Values")]
        [SerializeField] private float power; // Healing amount, buff strength, etc.
        [SerializeField] private int duration; // For buffs
        [SerializeField] private StatusEffectType statusEffect;
        
        [Header("Quantity")]
        [SerializeField] private bool isConsumable = true;
        // maxStack is handled by Inventory class

        #region Properties
        public string ItemName => itemName;
        public string Description => description;
        public ItemType Type => itemType;
        public Sprite Icon => icon;
        public TargetType Target => targetType;
        public bool IsConsumable => isConsumable;
        #endregion

        /// <summary>
        /// Use this item on target(s)
        /// </summary>
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
                    target.Revive(power / 100f); // power is percentage
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
            // This will be handled by TurnOrderManager
            return true;
        }
    }
}