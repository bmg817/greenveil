using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    public enum CombatActionType
    {
        Attack,
        Skill,
        Item,
        Defend,
        Flee,
        Talk
    }

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
            targets = new List<CombatCharacter>();
        }
    }

    public class CombatActionExecutor : MonoBehaviour
    {
        [SerializeField] private TurnOrderManager turnManager;
        [SerializeField] private Inventory inventory;
        [SerializeField] private float basicAttackPower = 10f;
        [SerializeField] private float basicAttackMPRestorePercent = 20f;

        public System.Action<CombatAction> OnActionExecuted;
        public System.Action<CombatCharacter, float> OnDamageDealt;
        public System.Action<CombatCharacter, float> OnHealingDone;
        public System.Action<CombatCharacter, float> OnMPChanged;

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
                turnManager.OnTurnStart += OnTurnStart;
        }

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

            bool isOffensiveAction = action.actionType == CombatActionType.Attack || action.actionType == CombatActionType.Skill;
            if (isOffensiveAction && action.user.HasStatusEffectType(StatusEffectType.Confused) && Random.value < 0.5f)
            {
                Debug.Log($"{action.user.CharacterName} is confused!");
                var allLiving = new List<CombatCharacter>();
                allLiving.AddRange(turnManager.GetLivingCharacters(true));
                allLiving.AddRange(turnManager.GetLivingCharacters(false));
                allLiving.Remove(action.user);
                if (allLiving.Count > 0)
                    action.targets = new List<CombatCharacter> { allLiving[Random.Range(0, allLiving.Count)] };
            }

            Debug.Log($">>> {action.user.CharacterName} performs {action.actionType} <<<");

            bool success = false;

            switch (action.actionType)
            {
                case CombatActionType.Attack:
                    success = ExecuteAttack(action);
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
                OnActionExecuted?.Invoke(action);

            return success;
        }

        private bool ExecuteAttack(CombatAction action)
        {
            if (action.ability != null)
                return ExecuteAbility(action.ability, action.user, action.targets);
            return ExecuteFallbackBasicAttack(action);
        }

        private bool ExecuteFallbackBasicAttack(CombatAction action)
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

            float damage = basicAttackPower + action.user.GetModifiedAttack();
            target.TakeDamage(damage, action.user.PrimaryElement, false, action.user);
            Debug.Log($"[ATTACK] {action.user.CharacterName} hits {target.CharacterName} for {damage:F0} damage");
            OnDamageDealt?.Invoke(target, damage);

            float mpRestore = action.user.MaxMP * (basicAttackMPRestorePercent / 100f);
            action.user.RestoreMP(mpRestore);
            OnMPChanged?.Invoke(action.user, mpRestore);

            return true;
        }

        private bool ExecuteSkill(CombatAction action)
        {
            if (action.ability == null)
            {
                Debug.LogWarning("No ability specified for Skill action!");
                return false;
            }
            return ExecuteAbility(action.ability, action.user, action.targets);
        }

        private bool ExecuteAbility(Ability ability, CombatCharacter user, List<CombatCharacter> targets)
        {
            if (!ability.CanUse(user))
            {
                Debug.LogWarning($"Cannot use {ability.AbilityName}!");
                return false;
            }

            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning($"No targets for {ability.AbilityName}!");
                return false;
            }

            ability.Use(user, targets);
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
                inventory = FindFirstObjectByType<Inventory>();

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

            return inventory.UseItem(action.item, action.user, action.targets);
        }

        private bool ExecuteDefend(CombatAction action)
        {
            action.user.SetDefending(true);
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
            return turnManager.AttemptFlee();
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

        private void OnTurnStart(CombatCharacter character)
        {
            character.SetDefending(false);
        }

        public List<CombatCharacter> GetValidTargets(CombatActionType actionType, TargetType targetType, CombatCharacter user)
        {
            bool userIsPlayer = turnManager.IsPlayerCharacter(user);
            bool targetAllies = targetType == TargetType.SingleAlly ||
                                targetType == TargetType.AllAllies ||
                                targetType == TargetType.Self;

            bool getPlayerTeam = targetAllies ? userIsPlayer : !userIsPlayer;
            List<CombatCharacter> validTargets = turnManager.GetLivingCharacters(getPlayerTeam);

            if (!targetAllies)
            {
                var taunter = validTargets.Find(t => t.HasStatusEffectType(StatusEffectType.Taunting));
                if (taunter != null)
                    validTargets = new List<CombatCharacter> { taunter };
            }

            switch (targetType)
            {
                case TargetType.Self:
                    return new List<CombatCharacter> { user };
                case TargetType.Random:
                    if (validTargets.Count > 0)
                    {
                        int randomIndex = Random.Range(0, validTargets.Count);
                        return new List<CombatCharacter> { validTargets[randomIndex] };
                    }
                    break;
            }

            return validTargets;
        }

        public static CombatAction CreateAttackAction(CombatCharacter user, CombatCharacter target)
        {
            var action = new CombatAction(CombatActionType.Attack, user);
            action.targets.Add(target);
            return action;
        }

        public static CombatAction CreateAttackAction(CombatCharacter user, CombatCharacter target, Ability basicAttackAbility)
        {
            var action = new CombatAction(CombatActionType.Attack, user);
            action.targets.Add(target);
            action.ability = basicAttackAbility;
            return action;
        }

        public static CombatAction CreateSkillAction(CombatCharacter user, Ability ability, List<CombatCharacter> targets)
        {
            var action = new CombatAction(CombatActionType.Skill, user);
            action.ability = ability;
            action.targets = targets;
            return action;
        }

        public static CombatAction CreateItemAction(CombatCharacter user, Item item, List<CombatCharacter> targets)
        {
            var action = new CombatAction(CombatActionType.Item, user);
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
            var action = new CombatAction(CombatActionType.Talk, user);
            action.dialogueId = dialogueId;
            return action;
        }

        public static CombatAction CreateDefendAction(CombatCharacter user)
        {
            return new CombatAction(CombatActionType.Defend, user);
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStart -= OnTurnStart;
        }
    }
}
