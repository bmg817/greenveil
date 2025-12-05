using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Greenveil.Combat;

/// <summary>
/// Combat test script with actual combat actions
/// FIXED: Added skill usage that consumes MP to verify MP system works
/// </summary>
public class CombatStarter : MonoBehaviour
{
    [Header("Combat Participants")]
    [SerializeField] private CombatCharacter playerCharacter;
    [SerializeField] private CombatCharacter enemyCharacter;
    
    [Header("Test Abilities (Optional)")]
    [SerializeField] private Ability testSkill;
    
    [Header("Debug - Test MP System")]
    [SerializeField] private bool useBuiltInTestSkills = true;
    [SerializeField] private float testSkillMPCost = 30f;  // Percentage
    
    private TurnOrderManager turnManager;
    private CombatActionExecutor actionExecutor;
    private AutoCombatHUD autoCombatHUD;
    private List<CharacterVisual> allVisuals = new List<CharacterVisual>();

    void Start()
    {
        turnManager = GetComponent<TurnOrderManager>();
        actionExecutor = GetComponent<CombatActionExecutor>();
        autoCombatHUD = GetComponent<AutoCombatHUD>();
        
        if (turnManager == null)
        {
            Debug.LogError("No TurnOrderManager found!");
            return;
        }

        if (actionExecutor == null)
        {
            Debug.LogError("No CombatActionExecutor found! Add it to CombatManager.");
            return;
        }
        
        if (playerCharacter == null || enemyCharacter == null)
        {
            Debug.LogError("Please assign Player and Enemy characters!");
            return;
        }
        
        // Register characters with HUD if available
        if (autoCombatHUD != null)
        {
            autoCombatHUD.RegisterCharacter(playerCharacter, isPlayer: true);
            autoCombatHUD.RegisterCharacter(enemyCharacter, isPlayer: false);
        }
        
        // Collect all character visuals
        var playerVisual = playerCharacter.GetComponent<CharacterVisual>();
        var enemyVisual = enemyCharacter.GetComponent<CharacterVisual>();
        if (playerVisual != null) allVisuals.Add(playerVisual);
        if (enemyVisual != null) allVisuals.Add(enemyVisual);
        
        // Subscribe to turn events
        turnManager.OnTurnStart += OnCharacterTurnStart;
        
        // Create lists of combatants
        List<CombatCharacter> players = new List<CombatCharacter> { playerCharacter };
        List<CombatCharacter> enemies = new List<CombatCharacter> { enemyCharacter };
        
        // Start combat!
        Debug.Log("Starting combat...");
        turnManager.InitializeCombat(players, enemies);
        
        PrintControls();
    }

    void PrintControls()
    {
        Debug.Log("=== COMBAT CONTROLS ===");
        Debug.Log("SPACE = Basic Attack (restores 20% MP)");
        Debug.Log($"1 = Test Skill (costs {testSkillMPCost}% MP)");
        Debug.Log("2 = Heavy Skill (costs 50% MP)");
        Debug.Log("F = Attempt Flee");
        Debug.Log("D = Defend");
        Debug.Log("======================");
    }

    void OnCharacterTurnStart(CombatCharacter character)
    {
        // Update visuals
        foreach (var visual in allVisuals)
        {
            if (visual != null)
            {
                visual.SetActive(false);
            }
        }
        
        CharacterVisual currentVisual = character.GetComponent<CharacterVisual>();
        if (currentVisual != null)
        {
            currentVisual.SetActive(true);
        }

        // If it's an enemy turn, auto-attack
        if (character == enemyCharacter)
        {
            Invoke(nameof(EnemyAutoAttack), 1f);
        }
    }

    void EnemyAutoAttack()
    {
        // Enemy automatically attacks the player
        CombatAction action = CombatActionExecutor.CreateAttackAction(enemyCharacter, playerCharacter);
        actionExecutor.ExecuteAction(action);
        
        // End turn after 0.5 seconds
        Invoke(nameof(EndCurrentTurn), 0.5f);
    }

    void Update()
    {
        if (!turnManager.IsCombatActive) return;
        if (turnManager.CurrentCharacter != playerCharacter) return;

        // SPACE = Basic Attack (restores MP)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PerformBasicAttack();
        }

        // 1 = Test Skill (consumes MP)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (testSkill != null)
            {
                PerformSkill();
            }
            else if (useBuiltInTestSkills)
            {
                PerformTestSkill(testSkillMPCost);
            }
            else
            {
                Debug.LogWarning("No test skill assigned and built-in test skills disabled!");
            }
        }

        // 2 = Heavy Skill (consumes 50% MP)
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            PerformTestSkill(50f);
        }

        // 3 = Light Skill (consumes 20% MP)
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            PerformTestSkill(20f);
        }

        // F = Flee
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            PerformFlee();
        }

        // D = Defend
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            PerformDefend();
        }
    }

    void PerformBasicAttack()
    {
        Debug.Log($"[SPACE] {playerCharacter.CharacterName} attacks! (will restore 20% MP)");
        
        CombatAction action = CombatActionExecutor.CreateAttackAction(playerCharacter, enemyCharacter);
        actionExecutor.ExecuteAction(action);
        
        EndCurrentTurn();
    }

    void PerformSkill()
    {
        Debug.Log($"[1] {playerCharacter.CharacterName} uses {testSkill.AbilityName}!");
        
        List<CombatCharacter> targets = new List<CombatCharacter> { enemyCharacter };
        CombatAction action = CombatActionExecutor.CreateSkillAction(playerCharacter, testSkill, targets);
        actionExecutor.ExecuteAction(action);
        
        EndCurrentTurn();
    }

    /// <summary>
    /// Test skill that directly consumes MP without needing a ScriptableObject
    /// </summary>
    void PerformTestSkill(float mpCostPercent)
    {
        float mpCost = playerCharacter.MaxMP * (mpCostPercent / 100f);
        
        Debug.Log($"[SKILL] {playerCharacter.CharacterName} attempts skill costing {mpCostPercent}% MP ({mpCost:F1} MP)");
        Debug.Log($"[SKILL] Current MP: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        
        // Check if enough MP
        if (!playerCharacter.HasEnoughMP(mpCost))
        {
            Debug.LogWarning($"[SKILL] Not enough MP! Need {mpCost:F1}, have {playerCharacter.CurrentMP:F1}");
            
            // Log to HUD
            if (autoCombatHUD != null)
            {
                autoCombatHUD.AddToLog($"Not enough MP! ({playerCharacter.CurrentMP:F0}/{mpCost:F0})");
            }
            return;  // Don't end turn on failed skill
        }
        
        // Consume MP
        playerCharacter.ConsumeMP(mpCost);
        
        // Deal damage (simplified - just use attack stat)
        float damage = playerCharacter.GetModifiedAttack() * 1.5f;
        enemyCharacter.TakeDamage(damage, playerCharacter.PrimaryElement);
        
        Debug.Log($"[SKILL] {playerCharacter.CharacterName} deals {damage:F1} damage!");
        Debug.Log($"[SKILL] MP after skill: {playerCharacter.CurrentMP}/{playerCharacter.MaxMP}");
        
        // Log to HUD
        if (autoCombatHUD != null)
        {
            autoCombatHUD.AddToLog($"{playerCharacter.CharacterName} uses skill! (-{mpCost:F0} MP)");
        }
        
        // Notify damage
        actionExecutor.OnDamageDealt?.Invoke(enemyCharacter, damage);
        
        EndCurrentTurn();
    }

    void PerformFlee()
    {
        Debug.Log($"[F] {playerCharacter.CharacterName} attempts to flee!");
        
        CombatAction action = CombatActionExecutor.CreateFleeAction(playerCharacter);
        actionExecutor.ExecuteAction(action);
    }

    void PerformDefend()
    {
        Debug.Log($"[D] {playerCharacter.CharacterName} defends!");
        
        CombatAction action = CombatActionExecutor.CreateDefendAction(playerCharacter);
        actionExecutor.ExecuteAction(action);
        
        EndCurrentTurn();
    }

    void EndCurrentTurn()
    {
        turnManager.EndTurn();
    }

    void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnStart -= OnCharacterTurnStart;
        }
    }
}