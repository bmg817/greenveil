using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Greenveil.Combat
{
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

    public class Inventory : MonoBehaviour
    {
        [SerializeField] private int maxSlots = 20;
        [SerializeField] private List<ItemStack> items = new List<ItemStack>();

        public System.Action<Item, int> OnItemAdded;
        public System.Action<Item, int> OnItemRemoved;
        public System.Action<Item> OnItemUsed;

        public List<ItemStack> Items => items;
        public int CurrentSlots => items.Count;
        public int MaxSlots => maxSlots;

        public bool AddItem(Item item, int quantity = 1)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot add null item!");
                return false;
            }

            ItemStack existingStack = items.FirstOrDefault(stack => stack.item == item);

            if (existingStack != null)
            {
                existingStack.Add(quantity);
                Debug.Log($"Added {quantity}x {item.ItemName}. Total: {existingStack.quantity}");
            }
            else
            {
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

                if (stack.IsEmpty())
                    items.Remove(stack);

                OnItemRemoved?.Invoke(item, quantity);
                return true;
            }

            Debug.LogWarning($"Not enough {item.ItemName}. Have: {stack.quantity}, Need: {quantity}");
            return false;
        }

        public bool HasItem(Item item, int quantity = 1)
        {
            ItemStack stack = items.FirstOrDefault(s => s.item == item);
            return stack != null && stack.quantity >= quantity;
        }

        public int GetItemQuantity(Item item)
        {
            ItemStack stack = items.FirstOrDefault(s => s.item == item);
            return stack?.quantity ?? 0;
        }

        public bool UseItem(Item item, CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!HasItem(item))
            {
                Debug.LogWarning($"Don't have {item.ItemName}!");
                return false;
            }

            bool success = item.UseItem(user, targets);

            if (success && item.IsConsumable)
            {
                RemoveItem(item, 1);
                OnItemUsed?.Invoke(item);
            }

            return success;
        }

        public List<ItemStack> GetItemsByType(ItemType type)
        {
            return items.Where(stack => stack.item.Type == type).ToList();
        }

        public void ClearInventory()
        {
            items.Clear();
            Debug.Log("Inventory cleared!");
        }
    }
}
