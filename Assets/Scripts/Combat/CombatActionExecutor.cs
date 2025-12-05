using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    /// <summary>
    /// Types of actions a character can take on their turn
    /// </summary>
    public enum CombatActionType
    {
        Attack,
        Skill,
        Item,
        Defend,
        Flee,
        Talk
    }

    /// <summary>
    /// Represents a single combat action
    /// </summary>
    public class CombatAction
    {
        public CombatActionType actionType;
        public CombatCharacter user;
        public List<CombatCharacter> targets;
        public Ability ability;
        public Item item;
        public string dialogueId;

        public CombatAction(CombatActionType type, CombatCharacter user)
        {
            this.actionType = type;
            this.user = user;
            this.targets = new List<CombatCharacter>();
        }
    }


    /// </summary>
    public class CombatActionExecutor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnOrderManager turnManager;
        [SerializeField] private Inventory inventory;

        [Header("Basic Attack Settings")]
        [SerializeField] private float basicAttackPower = 10f;
        [SerializeField] private float basicAttackMPRestorePercent = 20f;

        [Header("Defend Settings")]
        [SerializeField] private float defendMultiplier = 0.5f;
        private Dictionary<CombatCharacter, bool> defendingCharacters = new Dictionary<CombatCharacter, bool>();

        // Events
        public System.Action<CombatAction> OnActionExecuted;
        public System.Action<CombatCharacter, float> OnDamageDealt;
        public System.Action<CombatCharacter, float> OnHealingDone;
        public System.Action<CombatCharacter, float> OnMPRestored; 

        void Awake()
        {
            if (turnManager == null)
                turnManager = GetComponent<TurnOrderManager>();
            if (inventory == null)
                inventory = GetComponent<Inventory>();
        }

        void Start()
        {
            if (turnManager != null)
            {
                turnManager.OnTurnEnd += OnTurnEnd;
            }
        }

        #region Execute Actions
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
            
            if (IsDefending(target))
            {
                damage *= defendMultiplier;
                Debug.Log($"{target.CharacterName} is defending! Damage reduced.");
            }

            // Deal damage
            target.TakeDamage(damage, action.user.PrimaryElement);
            
            Debug.Log($"[ATTACK] {action.user.CharacterName} attacks {target.CharacterName} for {damage:F1} damage!");
            OnDamageDealt?.Invoke(target, damage);

            float mpBefore = action.user.CurrentMP;
            float mpRestoreAmount = action.user.MaxMP * (basicAttackMPRestorePercent / 100f);
            action.user.RestoreMP(mpRestoreAmount);
            float mpAfter = action.user.CurrentMP;
            
            Debug.Log($"[MP RESTORE] {action.user.CharacterName}: {mpBefore:F1} + {mpRestoreAmount:F1} = {mpAfter:F1} MP");
            OnMPRestored?.Invoke(action.user, mpRestoreAmount);

            return true;
        }

        private bool ExecuteSkill(CombatAction action)
        {
            if (action.ability == null)
            {
                Debug.LogWarning("No ability specified!");
                return false;
            }

            if (!action.ability.CanUse(action.user))
            {
                Debug.LogWarning($"Cannot use {action.ability.AbilityName}! (check MP)");
                return false;
            }

            if (action.targets == null || action.targets.Count == 0)
            {
                Debug.LogWarning("No targets for ability!");
                return false;
            }

            Debug.Log($"[SKILL] {action.user.CharacterName} uses {action.ability.AbilityName}");
            float mpBefore = action.user.CurrentMP;
            
            action.ability.Use(action.user, action.targets);
            
            float mpAfter = action.user.CurrentMP;
            Debug.Log($"[SKILL] MP: {mpBefore:F1} -> {mpAfter:F1}");
            
            return true;
        }

        private bool ExecuteItem(CombatAction action)
        {
            if (action.item == null)
            {
                Debug.LogWarning("No item specified!");
                return false;
            }

            if (inventory == null)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }

            if (inventory == null)
            {
                Debug.LogWarning("No inventory system found!");
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

            bool success = inventory.UseItem(action.item, action.user, action.targets);
            return success;
        }

        private bool ExecuteDefend(CombatAction action)
        {
            defendingCharacters[action.user] = true;
            Debug.Log($"{action.user.CharacterName} takes a defensive stance!");
            return true;
        }

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

        private bool ExecuteTalk(CombatAction action)
        {
            if (string.IsNullOrEmpty(action.dialogueId))
            {
                Debug.LogWarning("No dialogue ID specified!");
                return false;
            }

            Debug.Log($"=== {action.user.CharacterName} initiates dialogue: {action.dialogueId} ===");
            return true;
        }
        #endregion

        #region Defend System
        public bool IsDefending(CombatCharacter character)
        {
            return defendingCharacters.ContainsKey(character) && defendingCharacters[character];
        }

        private void OnTurnEnd(CombatCharacter character)
        {
            if (defendingCharacters.ContainsKey(character))
            {
                defendingCharacters[character] = false;
            }
        }
        #endregion

        #region Target Selection Helpers
        public List<CombatCharacter> GetValidTargets(CombatActionType actionType, TargetType targetType, CombatCharacter user)
        {
            List<CombatCharacter> validTargets = new List<CombatCharacter>();
            bool targetAllies = IsAllyTargeting(actionType, targetType);

            if (targetAllies)
            {
                validTargets = turnManager.GetLivingCharacters(true);
            }
            else
            {
                validTargets = turnManager.GetLivingCharacters(false);
            }

            switch (targetType)
            {
                case TargetType.Self:
                    validTargets = new List<CombatCharacter> { user };
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
                default:
                    return false;
            }
        }
        #endregion

        #region Static Action Creators
        public static CombatAction CreateAttackAction(CombatCharacter user, CombatCharacter target)
        {
            CombatAction action = new CombatAction(CombatActionType.Attack, user);
            action.targets.Add(target);
            return action;
        }

        public static CombatAction CreateSkillAction(CombatCharacter user, Ability ability, List<CombatCharacter> targets)
        {
            CombatAction action = new CombatAction(CombatActionType.Skill, user);
            action.ability = ability;
            action.targets = targets;
            return action;
        }

        public static CombatAction CreateItemAction(CombatCharacter user, Item item, List<CombatCharacter> targets)
        {
            CombatAction action = new CombatAction(CombatActionType.Item, user);
            action.item = item;
            action.targets = targets;
            return action;
        }

        public static CombatAction CreateFleeAction(CombatCharacter user)
        {
            return new CombatAction(CombatActionType.Flee, user);
        }

        public static CombatAction CreateTalkAction(CombatCharacter user, string dialogueId)
        {
            CombatAction action = new CombatAction(CombatActionType.Talk, user);
            action.dialogueId = dialogueId;
            return action;
        }

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