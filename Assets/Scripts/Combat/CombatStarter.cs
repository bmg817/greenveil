using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using Greenveil.Combat;

public class CombatStarter : MonoBehaviour
{
    [SerializeField] private List<CombatCharacter> playerCharacters = new List<CombatCharacter>();
    [SerializeField] private List<CombatCharacter> enemyCharacters = new List<CombatCharacter>();

    private TurnOrderManager turnManager;
    private CombatActionExecutor actionExecutor;
    private AutoCombatHUD autoCombatHUD;
    private List<CharacterVisual> allVisuals = new List<CharacterVisual>();

    private static readonly Key[] skillKeys = {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    void Start()
    {
        turnManager = GetComponent<TurnOrderManager>();
        actionExecutor = GetComponent<CombatActionExecutor>();
        autoCombatHUD = GetComponent<AutoCombatHUD>();

        if (turnManager == null || actionExecutor == null)
        {
            Debug.LogError("Missing TurnOrderManager or CombatActionExecutor!");
            return;
        }

        if (playerCharacters.Count == 0 || enemyCharacters.Count == 0)
        {
            Debug.LogError("Assign Player and Enemy characters!");
            return;
        }

        if (autoCombatHUD != null)
        {
            foreach (var player in playerCharacters)
                autoCombatHUD.RegisterCharacter(player, true);
            foreach (var enemy in enemyCharacters)
                autoCombatHUD.RegisterCharacter(enemy, false);
        }

        foreach (var character in playerCharacters.Concat(enemyCharacters))
        {
            var visual = character.GetComponent<CharacterVisual>();
            if (visual != null) allVisuals.Add(visual);
        }

        foreach (var enemy in enemyCharacters)
            enemy.OnFlowerTrapTriggered += OnFlowerTrapTriggered;
        foreach (var player in playerCharacters)
            player.OnFlowerTrapTriggered += OnFlowerTrapTriggered;

        turnManager.OnTurnStart += OnCharacterTurnStart;
        turnManager.InitializeCombat(playerCharacters, enemyCharacters);
    }

    void OnFlowerTrapTriggered(CombatCharacter triggered, float spreadDamage)
    {
        bool isEnemy = enemyCharacters.Contains(triggered);
        var teammates = isEnemy
            ? enemyCharacters.Where(c => c.IsAlive && c != triggered).ToList()
            : playerCharacters.Where(c => c.IsAlive && c != triggered).ToList();

        foreach (var teammate in teammates)
        {
            if (!teammate.HasStatusEffectType(StatusEffectType.FlowerTrap))
            {
                teammate.ApplyStatusEffect(new StatusEffect(StatusEffectType.FlowerTrap, 1, spreadDamage));
                Debug.Log($"[FLOWER TRAP] Spread to {teammate.CharacterName}!");
            }
        }
    }

    void OnCharacterTurnStart(CombatCharacter character)
    {
        foreach (var visual in allVisuals)
            if (visual != null) visual.SetActive(false);

        var currentVisual = character.GetComponent<CharacterVisual>();
        if (currentVisual != null) currentVisual.SetActive(true);

        if (autoCombatHUD != null)
        {
            if (playerCharacters.Contains(character))
                autoCombatHUD.ShowSkillsBar(character);
            else
                autoCombatHUD.HideSkillsBar();
        }

        if (enemyCharacters.Contains(character))
            Invoke(nameof(EnemyTakeTurnDelayed), 1f);
    }

    void EnemyTakeTurnDelayed()
    {
        EnemyTakeTurn(turnManager.CurrentCharacter);
    }

    void EnemyTakeTurn(CombatCharacter enemy)
    {
        Ability chosenAbility = enemy.BasicAttack;

        if (enemy.Skills != null && enemy.Skills.Length > 0)
        {
            var usable = enemy.Skills.Where(s => s.CanUse(enemy)).ToList();

            float hpPercent = enemy.CurrentHealth / enemy.MaxHealth;
            if (hpPercent <= 0.75f && !enemy.HasStatusEffectType(StatusEffectType.DamageReflect))
            {
                var reflex = usable.FirstOrDefault(s => s.id == "ancient_reflex");
                if (reflex != null)
                    chosenAbility = reflex;
                else if (usable.Count > 0)
                    chosenAbility = usable[Random.Range(0, usable.Count)];
            }
            else if (usable.Count > 0)
            {
                var filtered = usable.Where(s => s.id != "ancient_reflex").ToList();
                if (filtered.Count > 0)
                    chosenAbility = filtered[Random.Range(0, filtered.Count)];
                else
                    chosenAbility = usable[Random.Range(0, usable.Count)];
            }
        }

        if (chosenAbility == null)
        {
            var target = GetRandomLiving(playerCharacters);
            if (target != null)
            {
                var action = CombatActionExecutor.CreateAttackAction(enemy, target);
                actionExecutor.ExecuteAction(action);
            }
        }
        else
        {
            var targets = actionExecutor.GetValidTargets(CombatActionType.Skill, chosenAbility.Target, enemy);

            if (chosenAbility.Target == TargetType.SingleEnemy || chosenAbility.Target == TargetType.SingleAlly)
            {
                if (targets.Count > 0)
                    targets = new List<CombatCharacter> { targets[Random.Range(0, targets.Count)] };
            }

            if (chosenAbility.Type == AbilityType.BasicAttack)
            {
                if (targets.Count > 0)
                {
                    var action = CombatActionExecutor.CreateAttackAction(enemy, targets[0], chosenAbility);
                    actionExecutor.ExecuteAction(action);
                }
            }
            else
            {
                var action = CombatActionExecutor.CreateSkillAction(enemy, chosenAbility, targets);
                actionExecutor.ExecuteAction(action);
            }
        }

        Invoke(nameof(EndCurrentTurn), 0.5f);
    }

    void Update()
    {
        if (!turnManager.IsCombatActive) return;
        if (!playerCharacters.Contains(turnManager.CurrentCharacter)) return;

        var current = turnManager.CurrentCharacter;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            PerformBasicAttack(current);

        int skillCount = 0;
        if (current.Skills != null)
        {
            skillCount = current.Skills.Length;
            for (int i = 0; i < current.Skills.Length && i < skillKeys.Length; i++)
            {
                if (Keyboard.current[skillKeys[i]].wasPressedThisFrame)
                    PerformSkill(current, current.Skills[i], i + 1);
            }
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
            PerformDefend(current, skillCount + 1);

        if (Keyboard.current.fKey.wasPressedThisFrame)
            PerformFlee(current, skillCount + 2);
    }

    void PerformBasicAttack(CombatCharacter player)
    {
        if (autoCombatHUD != null) autoCombatHUD.HighlightAction(0);

        var target = GetRandomLiving(enemyCharacters);
        if (target == null) return;

        if (player.BasicAttack != null)
        {
            var action = CombatActionExecutor.CreateAttackAction(player, target, player.BasicAttack);
            actionExecutor.ExecuteAction(action);
        }
        else
        {
            var action = CombatActionExecutor.CreateAttackAction(player, target);
            actionExecutor.ExecuteAction(action);
        }

        EndCurrentTurn();
    }

    void PerformSkill(CombatCharacter player, Ability skill, int highlightIndex)
    {
        if (!skill.CanUse(player))
        {
            Debug.Log($"Cannot use {skill.AbilityName}!");
            return;
        }

        if (autoCombatHUD != null) autoCombatHUD.HighlightAction(highlightIndex);

        var targets = actionExecutor.GetValidTargets(CombatActionType.Skill, skill.Target, player);

        if (skill.Target == TargetType.SingleEnemy || skill.Target == TargetType.SingleAlly)
        {
            if (targets.Count > 0)
                targets = new List<CombatCharacter> { targets[0] };
        }

        var action = CombatActionExecutor.CreateSkillAction(player, skill, targets);
        actionExecutor.ExecuteAction(action);
        EndCurrentTurn();
    }

    void PerformFlee(CombatCharacter player, int highlightIndex)
    {
        if (autoCombatHUD != null) autoCombatHUD.HighlightAction(highlightIndex);
        var action = CombatActionExecutor.CreateFleeAction(player);
        actionExecutor.ExecuteAction(action);
    }

    void PerformDefend(CombatCharacter player, int highlightIndex)
    {
        if (autoCombatHUD != null) autoCombatHUD.HighlightAction(highlightIndex);
        var action = CombatActionExecutor.CreateDefendAction(player);
        actionExecutor.ExecuteAction(action);
        EndCurrentTurn();
    }

    CombatCharacter GetRandomLiving(List<CombatCharacter> characters)
    {
        var living = characters.Where(c => c.IsAlive).ToList();
        if (living.Count == 0) return null;
        return living[Random.Range(0, living.Count)];
    }

    void EndCurrentTurn()
    {
        turnManager.EndTurn();
    }

    void OnDestroy()
    {
        if (turnManager != null)
            turnManager.OnTurnStart -= OnCharacterTurnStart;

        foreach (var enemy in enemyCharacters)
            if (enemy != null) enemy.OnFlowerTrapTriggered -= OnFlowerTrapTriggered;
        foreach (var player in playerCharacters)
            if (player != null) player.OnFlowerTrapTriggered -= OnFlowerTrapTriggered;
    }
}
