using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Greenveil.Combat;

public class CombatStarter : MonoBehaviour
{
    [Header("Combat Participants")]
    [SerializeField] private CombatCharacter playerCharacter;
    [SerializeField] private CombatCharacter enemyCharacter;
    
    [Header("Test Abilities")]
    [SerializeField] private Ability testSkill;
    
    private TurnOrderManager turnManager;
    private CombatActionExecutor actionExecutor;
    private AutoCombatHUD autoCombatHUD;
    private List<CharacterVisual> allVisuals = new List<CharacterVisual>();

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
        
        if (playerCharacter == null || enemyCharacter == null)
        {
            Debug.LogError("Assign Player and Enemy characters!");
            return;
        }
        
        if (autoCombatHUD != null)
        {
            autoCombatHUD.RegisterCharacter(playerCharacter, true);
            autoCombatHUD.RegisterCharacter(enemyCharacter, false);
        }
        
        var playerVisual = playerCharacter.GetComponent<CharacterVisual>();
        var enemyVisual = enemyCharacter.GetComponent<CharacterVisual>();
        if (playerVisual != null) allVisuals.Add(playerVisual);
        if (enemyVisual != null) allVisuals.Add(enemyVisual);
        
        turnManager.OnTurnStart += OnCharacterTurnStart;
        
        List<CombatCharacter> players = new List<CombatCharacter> { playerCharacter };
        List<CombatCharacter> enemies = new List<CombatCharacter> { enemyCharacter };
        
        Debug.Log("Starting combat...");
        Debug.Log($"Player MP: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        turnManager.InitializeCombat(players, enemies);
        
        PrintControls();
    }

    void PrintControls()
    {
        Debug.Log("=== CONTROLS ===");
        Debug.Log("SPACE = Attack (+20% MP)");
        Debug.Log("1 = Skill 30% | 2 = Skill 50% | 3 = Skill 20%");
        Debug.Log("T = TEST: Drain 10 MP");
        Debug.Log("================");
    }

    void OnCharacterTurnStart(CombatCharacter character)
    {
        foreach (var visual in allVisuals)
            if (visual != null) visual.SetActive(false);
        
        CharacterVisual currentVisual = character.GetComponent<CharacterVisual>();
        if (currentVisual != null) currentVisual.SetActive(true);

        if (character == enemyCharacter)
            Invoke(nameof(EnemyAutoAttack), 1f);
    }

    void EnemyAutoAttack()
    {
        CombatAction action = CombatActionExecutor.CreateAttackAction(enemyCharacter, playerCharacter);
        actionExecutor.ExecuteAction(action);
        Invoke(nameof(EndCurrentTurn), 0.5f);
    }

    void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TestDrainMP();
            return;
        }

        if (!turnManager.IsCombatActive) return;
        if (turnManager.CurrentCharacter != playerCharacter) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            PerformBasicAttack();

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            PerformTestSkill(30f);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            PerformTestSkill(50f);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            PerformTestSkill(20f);

        if (Keyboard.current.fKey.wasPressedThisFrame)
            PerformFlee();

        if (Keyboard.current.dKey.wasPressedThisFrame)
            PerformDefend();
    }

    void PerformBasicAttack()
    {
        Debug.Log($"=== BASIC ATTACK ===");
        Debug.Log($"MP BEFORE: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        
        CombatAction action = CombatActionExecutor.CreateAttackAction(playerCharacter, enemyCharacter);
        actionExecutor.ExecuteAction(action);
        
        Debug.Log($"MP AFTER: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        
        EndCurrentTurn();
    }

    void PerformTestSkill(float mpCostPercent)
    {
        float mpCost = playerCharacter.GetMPCost(mpCostPercent);
        
        Debug.Log($"=== SKILL {mpCostPercent}% ===");
        Debug.Log($"MP BEFORE: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        Debug.Log($"Cost: {mpCost} MP");
        
        if (!playerCharacter.HasEnoughMP(mpCostPercent))
        {
            Debug.LogWarning($"NOT ENOUGH MP! Need {mpCost}, have {playerCharacter.CurrentMP}");
            if (autoCombatHUD != null)
                autoCombatHUD.AddToLog($"Need {mpCost:F0} MP!");
            return;
        }
        
        bool consumed = playerCharacter.ConsumeMPPercent(mpCostPercent);
        Debug.Log($"MP consumed: {consumed}");
        Debug.Log($"MP AFTER: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        
        float damage = playerCharacter.Attack * 1.5f;
        enemyCharacter.TakeDamage(damage, playerCharacter.PrimaryElement);
        
        if (autoCombatHUD != null)
            autoCombatHUD.AddToLog($"Skill! -{mpCost:F0} MP");
        
        actionExecutor.OnDamageDealt?.Invoke(enemyCharacter, damage);
        
        EndCurrentTurn();
    }

    void PerformFlee()
    {
        CombatAction action = CombatActionExecutor.CreateFleeAction(playerCharacter);
        actionExecutor.ExecuteAction(action);
    }

    void PerformDefend()
    {
        CombatAction action = CombatActionExecutor.CreateDefendAction(playerCharacter);
        actionExecutor.ExecuteAction(action);
        EndCurrentTurn();
    }

    void TestDrainMP()
    {
        Debug.Log($"=== TEST MP DRAIN ===");
        Debug.Log($"MP BEFORE: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        
        float drainAmount = 10f;
        playerCharacter.ConsumeMP(drainAmount);
        
        Debug.Log($"MP AFTER: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        
        if (autoCombatHUD != null)
            autoCombatHUD.AddToLog($"Drained {drainAmount} MP");
    }

    void EndCurrentTurn()
    {
        turnManager.EndTurn();
    }

    void OnDestroy()
    {
        if (turnManager != null)
            turnManager.OnTurnStart -= OnCharacterTurnStart;
    }
}