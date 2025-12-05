using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    /// <summary>
    /// Types of actions a character can take on their turn
    /// </summary>
    public enum CombatActionType
    {
        Attack,     // Basic attack
        Skill,      // Use an ability
        Item,       // Use an item
        Defend,     // Increase defense for this round
        Flee,       // Attempt to escape
        Talk        // Trigger dialogue
    }

    /// <summary>
    /// Represents a single combat action
    /// </summary>
    public class CombatAction
    {
        public CombatActionType actionType;
        public CombatCharacter user;
        public List<CombatCharacter> targets;
        public Ability ability;      // For Skill actions
        public Item item;           // For Item actions
        public string dialogueId;   // For Talk actions

        public CombatAction(CombatActionType type, CombatCharacter user)
        {
            this.actionType = type;
            this.user = user;
            this.targets = new List<CombatCharacter>();
        }
    }

    /// <summary>
    /// Executes combat actions during turns
    /// Attach to CombatManager GameObject
    /// </summary>
    public class CombatActionExecutor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnOrderManager turnManager;
        [SerializeField] private Inventory inventory;

        [Header("Basic Attack Settings")]
        [SerializeField] private float basicAttackPower = 10f;
        [SerializeField] private float basicAttackMPRestore = 20f; // Percentage

        [Header("Defend Settings")]
        [SerializeField] private float defendMultiplier = 0.5f; // 50% damage reduction
        private Dictionary<CombatCharacter, bool> defendingCharacters = new Dictionary<CombatCharacter, bool>();

        // Events
        public System.Action<CombatAction> OnActionExecuted;
        public System.Action<CombatCharacter, float> OnDamageDealt;
        public System.Action<CombatCharacter, float> OnHealingDone;

        void Awake()
        {
            if (turnManager == null)
                turnManager = GetComponent<TurnOrderManager>();
            if (inventory == null)
                inventory = GetComponent<Inventory>();
            // DialogueManager will be found at runtime if needed
        }

        void Start()
        {
            // Subscribe to turn events
            if (turnManager != null)
            {
                turnManager.OnTurnEnd += OnTurnEnd;
            }
        }

        #region Execute Actions
        /// <summary>
        /// Execute a combat action
        /// </summary>
        public bool ExecuteAction(CombatAction action)
        {
            if (action == null || action.user == null)
            {
                Debug.LogError("Invalid action!");
                return false;
            }

            if (!action.user.IsAlive)
            {
                Debug.LogWarning($"{action.user.CharacterName} is defeated and cannot act!");
                return false;
            }

            Debug.Log($">>> {action.user.CharacterName} performs {action.actionType} <<<");

            bool success = false;

            switch (action.actionType)
            {
                case CombatActionType.Attack:
                    success = ExecuteBasicAttack(action);
                    break;
                case CombatActionType.Skill:
                    success = ExecuteSkill(action);
                    break;
                case CombatActionType.Item:
                    success = ExecuteItem(action);
                    break;
                case CombatActionType.Defend:
                    success = ExecuteDefend(action);
                    break;
                case CombatActionType.Flee:
                    success = ExecuteFlee(action);
                    break;
                case CombatActionType.Talk:
                    success = ExecuteTalk(action);
                    break;
            }

            if (success)
            {
                OnActionExecuted?.Invoke(action);
            }

            return success;
        }

        /// <summary>
        /// Execute basic attack
        /// </summary>
        private bool ExecuteBasicAttack(CombatAction action)
        {
            if (action.targets == null || action.targets.Count == 0)
            {
                Debug.LogWarning("No target for attack!");
                return false;
            }

            CombatCharacter target = action.targets[0];

            if (!target.IsAlive)
            {
                Debug.LogWarning($"{target.CharacterName} is already defeated!");
                return false;
            }

            // Calculate damage
            float damage = basicAttackPower + action.user.GetModifiedAttack();
            
            // Apply defend modifier if target is defending
            if (IsDefending(target))
            {
                damage *= defendMultiplier;
                Debug.Log($"{target.CharacterName} is defending! Damage reduced.");
            }

            // Deal damage
            target.TakeDamage(damage, action.user.PrimaryElement);
            
            Debug.Log($"{action.user.CharacterName} attacks {target.CharacterName} for {damage} damage!");
            OnDamageDealt?.Invoke(target, damage);

            // Restore MP
            float mpRestore = action.user.MaxMP * (basicAttackMPRestore / 100f);
            action.user.RestoreMP(mpRestore);
            Debug.Log($"{action.user.CharacterName} restored {mpRestore} MP!");

            return true;
        }

        /// <summary>
        /// Execute skill/ability
        /// </summary>
        private bool ExecuteSkill(CombatAction action)
        {
            if (action.ability == null)
            {
                Debug.LogWarning("No ability specified!");
                return false;
            }

            if (!action.ability.CanUse(action.user))
            {
                Debug.LogWarning($"Cannot use {action.ability.AbilityName}!");
                return false;
            }

            if (action.targets == null || action.targets.Count == 0)
            {
                Debug.LogWarning("No targets for ability!");
                return false;
            }

            // Use the ability
            action.ability.Use(action.user, action.targets);
            
            return true;
        }

        /// <summary>
        /// Execute item usage
        /// </summary>
        private bool ExecuteItem(CombatAction action)
        {
            if (action.item == null)
            {
                Debug.LogWarning("No item specified!");
                return false;
            }

            // Try to find inventory
            if (inventory == null)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }

            if (inventory == null)
            {
                Debug.LogWarning("No inventory system found! Add Inventory.cs to use items.");
                Debug.Log($"{action.user.CharacterName} tries to use {action.item.ItemName} but no inventory exists!");
                return false;
            }

            if (!inventory.HasItem(action.item))
            {
                Debug.LogWarning($"Don't have {action.item.ItemName}!");
                return false;
            }

            if (action.targets == null || action.targets.Count == 0)
            {
                Debug.LogWarning("No targets for item!");
                return false;
            }

            // Use the item
            bool success = inventory.UseItem(action.item, action.user, action.targets);
            
            return success;
        }

        /// <summary>
        /// Execute defend action
        /// </summary>
        private bool ExecuteDefend(CombatAction action)
        {
            defendingCharacters[action.user] = true;
            Debug.Log($"{action.user.CharacterName} takes a defensive stance!");
            return true;
        }

        /// <summary>
        /// Execute flee attempt
        /// </summary>
        private bool ExecuteFlee(CombatAction action)
        {
            if (turnManager == null)
            {
                Debug.LogWarning("No turn manager for flee!");
                return false;
            }

            Debug.Log($"{action.user.CharacterName} attempts to flee!");
            bool success = turnManager.AttemptFlee();
            
            return success;
        }

        /// <summary>
        /// Execute talk action
        /// NOTE: Full dialogue system requires DialogueManager.cs
        /// For now, just logs the dialogue
        /// </summary>
        private bool ExecuteTalk(CombatAction action)
        {
            if (string.IsNullOrEmpty(action.dialogueId))
            {
                Debug.LogWarning("No dialogue ID specified!");
                return false;
            }

            Debug.Log($"=== {action.user.CharacterName} initiates dialogue: {action.dialogueId} ===");
            Debug.Log("(Add DialogueManager.cs for full dialogue system)");
            
            // TODO: Once DialogueManager is added, uncomment this:
            // var dialogueManager = GetComponent<DialogueManager>();
            // if (dialogueManager != null)
            //     dialogueManager.TriggerDialogueById(action.dialogueId);
            
            return true;
        }
        #endregion

        #region Defend System
        /// <summary>
        /// Check if character is defending
        /// </summary>
        public bool IsDefending(CombatCharacter character)
        {
            return defendingCharacters.ContainsKey(character) && defendingCharacters[character];
        }

        /// <summary>
        /// Clear defend status at end of turn
        /// </summary>
        private void OnTurnEnd(CombatCharacter character)
        {
            if (defendingCharacters.ContainsKey(character))
            {
                defendingCharacters[character] = false;
            }
        }
        #endregion

        #region Target Selection Helpers
        /// <summary>
        /// Get all valid targets for an action
        /// </summary>
        public List<CombatCharacter> GetValidTargets(CombatActionType actionType, TargetType targetType, CombatCharacter user)
        {
            List<CombatCharacter> validTargets = new List<CombatCharacter>();

            // Determine if targeting allies or enemies
            bool targetAllies = IsAllyTargeting(actionType, targetType);

            if (targetAllies)
            {
                validTargets = turnManager.GetLivingCharacters(true); // Get living allies
            }
            else
            {
                validTargets = turnManager.GetLivingCharacters(false); // Get living enemies
            }

            // Filter based on target type
            switch (targetType)
            {
                case TargetType.Self:
                    validTargets = new List<CombatCharacter> { user };
                    break;
                case TargetType.SingleAlly:
                case TargetType.SingleEnemy:
                    // Return all valid single targets
                    break;
                case TargetType.AllAllies:
                case TargetType.AllEnemies:
                    // Return all in group
                    break;
                case TargetType.Random:
                    if (validTargets.Count > 0)
                    {
                        int randomIndex = Random.Range(0, validTargets.Count);
                        validTargets = new List<CombatCharacter> { validTargets[randomIndex] };
                    }
                    break;
            }

            return validTargets;
        }

        private bool IsAllyTargeting(CombatActionType actionType, TargetType targetType)
        {
            switch (targetType)
            {
                case TargetType.SingleAlly:
                case TargetType.AllAllies:
                case TargetType.Self:
                    return true;
                case TargetType.SingleEnemy:
                case TargetType.AllEnemies:
                    return false;
                default:
                    return false;
            }
        }
        #endregion

        #region Utility
        /// <summary>
        /// Create a basic attack action
        /// </summary>
        public static CombatAction CreateAttackAction(CombatCharacter user, CombatCharacter target)
        {
            CombatAction action = new CombatAction(CombatActionType.Attack, user);
            action.targets.Add(target);
            return action;
        }

        /// <summary>
        /// Create a skill action
        /// </summary>
        public static CombatAction CreateSkillAction(CombatCharacter user, Ability ability, List<CombatCharacter> targets)
        {
            CombatAction action = new CombatAction(CombatActionType.Skill, user);
            action.ability = ability;
            action.targets = targets;
            return action;
        }

        /// <summary>
        /// Create an item action
        /// </summary>
        public static CombatAction CreateItemAction(CombatCharacter user, Item item, List<CombatCharacter> targets)
        {
            CombatAction action = new CombatAction(CombatActionType.Item, user);
            action.item = item;
            action.targets = targets;
            return action;
        }

        /// <summary>
        /// Create a flee action
        /// </summary>
        public static CombatAction CreateFleeAction(CombatCharacter user)
        {
            return new CombatAction(CombatActionType.Flee, user);
        }

        /// <summary>
        /// Create a talk action
        /// </summary>
        public static CombatAction CreateTalkAction(CombatCharacter user, string dialogueId)
        {
            CombatAction action = new CombatAction(CombatActionType.Talk, user);
            action.dialogueId = dialogueId;
            return action;
        }

        /// <summary>
        /// Create a defend action
        /// </summary>
        public static CombatAction CreateDefendAction(CombatCharacter user)
        {
            return new CombatAction(CombatActionType.Defend, user);
        }
        #endregion

        void OnDestroy()
        {
            if (turnManager != null)
            {
                turnManager.OnTurnEnd -= OnTurnEnd;
            }
        }
    }
}