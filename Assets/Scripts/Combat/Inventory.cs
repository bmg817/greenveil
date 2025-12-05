using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Greenveil.Combat
{
    /// <summary>
    /// Represents a stack of items in inventory
    /// </summary>
    [System.Serializable]
    public class ItemStack
    {
        public Item item;
        public int quantity;

        public ItemStack(Item item, int quantity = 1)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public bool CanAddMore(int maxStack)
        {
            return quantity < maxStack;
        }

        public void Add(int amount)
        {
            quantity += amount;
        }

        public bool Remove(int amount = 1)
        {
            if (quantity >= amount)
            {
                quantity -= amount;
                return true;
            }
            return false;
        }

        public bool IsEmpty()
        {
            return quantity <= 0;
        }
    }

    /// <summary>
    /// Manages party inventory for combat items
    /// Attach to a persistent GameObject (like GameManager)
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int maxSlots = 20;
        [SerializeField] private List<ItemStack> items = new List<ItemStack>();

        // Events
        public System.Action<Item, int> OnItemAdded;
        public System.Action<Item, int> OnItemRemoved;
        public System.Action<Item> OnItemUsed;

        #region Properties
        public List<ItemStack> Items => items;
        public int CurrentSlots => items.Count;
        public int MaxSlots => maxSlots;
        #endregion

        #region Add/Remove Items
        /// <summary>
        /// Add item to inventory
        /// </summary>
        public bool AddItem(Item item, int quantity = 1)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot add null item!");
                return false;
            }

            // Try to find existing stack
            ItemStack existingStack = items.FirstOrDefault(stack => stack.item == item);

            if (existingStack != null)
            {
                // Add to existing stack
                existingStack.Add(quantity);
                Debug.Log($"Added {quantity}x {item.ItemName}. Total: {existingStack.quantity}");
            }
            else
            {
                // Create new stack
                if (items.Count >= maxSlots)
                {
                    Debug.LogWarning("Inventory is full!");
                    return false;
                }

                items.Add(new ItemStack(item, quantity));
                Debug.Log($"Added new item: {quantity}x {item.ItemName}");
            }

            OnItemAdded?.Invoke(item, quantity);
            return true;
        }

        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public bool RemoveItem(Item item, int quantity = 1)
        {
            ItemStack stack = items.FirstOrDefault(s => s.item == item);
            
            if (stack == null)
            {
                Debug.LogWarning($"{item.ItemName} not found in inventory!");
                return false;
            }

            if (stack.Remove(quantity))
            {
                Debug.Log($"Removed {quantity}x {item.ItemName}. Remaining: {stack.quantity}");
                
                // Remove empty stacks
                if (stack.IsEmpty())
                {
                    items.Remove(stack);
                }

                OnItemRemoved?.Invoke(item, quantity);
                return true;
            }

            Debug.LogWarning($"Not enough {item.ItemName}. Have: {stack.quantity}, Need: {quantity}");
            return false;
        }

        /// <summary>
        /// Check if item exists in inventory
        /// </summary>
        public bool HasItem(Item item, int quantity = 1)
        {
            ItemStack stack = items.FirstOrDefault(s => s.item == item);
            return stack != null && stack.quantity >= quantity;
        }

        /// <summary>
        /// Get quantity of specific item
        /// </summary>
        public int GetItemQuantity(Item item)
        {
            ItemStack stack = items.FirstOrDefault(s => s.item == item);
            return stack?.quantity ?? 0;
        }
        #endregion

        #region Use Items
        /// <summary>
        /// Use an item on target(s)
        /// </summary>
        public bool UseItem(Item item, CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!HasItem(item))
            {
                Debug.LogWarning($"Don't have {item.ItemName}!");
                return false;
            }

            // Use the item
            bool success = item.UseItem(user, targets);

            if (success && item.IsConsumable)
            {
                // Remove from inventory
                RemoveItem(item, 1);
                OnItemUsed?.Invoke(item);
            }

            return success;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Get all items of a specific type
        /// </summary>
        public List<ItemStack> GetItemsByType(ItemType type)
        {
            return items.Where(stack => stack.item.Type == type).ToList();
        }

        /// <summary>
        /// Clear entire inventory
        /// </summary>
        public void ClearInventory()
        {
            items.Clear();
            Debug.Log("Inventory cleared!");
        }

        /// <summary>
        /// Print inventory contents
        /// </summary>
        public void PrintInventory()
        {
            Debug.Log($"=== Inventory ({items.Count}/{maxSlots} slots) ===");
            foreach (var stack in items)
            {
                Debug.Log($"  {stack.item.ItemName} x{stack.quantity}");
            }
        }
        #endregion
    }
}
