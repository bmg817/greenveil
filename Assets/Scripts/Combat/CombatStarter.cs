using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Greenveil.Combat;

/// <summary>
/// Combat test script with actual combat actions
/// Attach this to CombatManager GameObject
/// Uses Unity's new Input System
/// </summary>
public class CombatStarter : MonoBehaviour
{
    [Header("Combat Participants")]
    [SerializeField] private CombatCharacter playerCharacter;
    [SerializeField] private CombatCharacter enemyCharacter;
    
    [Header("Test Abilities (Optional)")]
    [SerializeField] private Ability testSkill;
    // [SerializeField] private Item testItem; // Uncomment when Item.cs is added
    
    private TurnOrderManager turnManager;
    private CombatActionExecutor actionExecutor;
    private AutoCombatHUD autoCombatHUD;
    private List<CharacterVisual> allVisuals = new List<CharacterVisual>();

    void Start()
    {
        // Get components
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
        allVisuals.Add(playerCharacter.GetComponent<CharacterVisual>());
        allVisuals.Add(enemyCharacter.GetComponent<CharacterVisual>());
        
        // Subscribe to turn events
        turnManager.OnTurnStart += OnCharacterTurnStart;
        
        // Create lists of combatants
        List<CombatCharacter> players = new List<CombatCharacter> { playerCharacter };
        List<CombatCharacter> enemies = new List<CombatCharacter> { enemyCharacter };
        
        // Start combat!
        Debug.Log("Starting combat...");
        turnManager.InitializeCombat(players, enemies);
        
        Debug.Log("=== COMBAT CONTROLS ===");
        Debug.Log("SPACE = Basic Attack");
        Debug.Log("1 = Use Test Skill (if assigned)");
        Debug.Log("2 = Use Test Item (if assigned)");
        Debug.Log("F = Attempt Flee");
        Debug.Log("D = Defend");
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
        
        // Only allow player input on player's turn
        if (turnManager.CurrentCharacter != playerCharacter) return;

        // SPACE = Basic Attack
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PerformBasicAttack();
        }

        // 1 = Use Skill
        if (Keyboard.current.digit1Key.wasPressedThisFrame && testSkill != null)
        {
            PerformSkill();
        }

        // 2 = Use Item (uncomment when Item.cs is added)
        /*
        if (Keyboard.current.digit2Key.wasPressedThisFrame && testItem != null)
        {
            PerformItem();
        }
        */

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
        Debug.Log($"[SPACE] {playerCharacter.CharacterName} attacks!");
        
        // Create attack action
        CombatAction action = CombatActionExecutor.CreateAttackAction(playerCharacter, enemyCharacter);
        
        // Execute action
        actionExecutor.ExecuteAction(action);
        
        // End turn
        EndCurrentTurn();
    }

    void PerformSkill()
    {
        Debug.Log($"[1] {playerCharacter.CharacterName} uses {testSkill.AbilityName}!");
        
        // Create skill action
        List<CombatCharacter> targets = new List<CombatCharacter> { enemyCharacter };
        CombatAction action = CombatActionExecutor.CreateSkillAction(playerCharacter, testSkill, targets);
        
        // Execute action
        actionExecutor.ExecuteAction(action);
        
        // End turn
        EndCurrentTurn();
    }

    /*
    void PerformItem()
    {
        Debug.Log($"[2] {playerCharacter.CharacterName} uses {testItem.ItemName}!");
        
        // Create item action (target self for healing, or enemy for damage)
        List<CombatCharacter> targets = testItem.Type == ItemType.HealingItem 
            ? new List<CombatCharacter> { playerCharacter }
            : new List<CombatCharacter> { enemyCharacter };
        
        CombatAction action = CombatActionExecutor.CreateItemAction(playerCharacter, testItem, targets);
        
        // Execute action
        actionExecutor.ExecuteAction(action);
        
        // End turn
        EndCurrentTurn();
    }
    */

    void PerformFlee()
    {
        Debug.Log($"[F] {playerCharacter.CharacterName} attempts to flee!");
        
        // Create flee action
        CombatAction action = CombatActionExecutor.CreateFleeAction(playerCharacter);
        
        // Execute action (will end turn automatically if failed)
        actionExecutor.ExecuteAction(action);
    }

    void PerformDefend()
    {
        Debug.Log($"[D] {playerCharacter.CharacterName} defends!");
        
        // Create defend action
        CombatAction action = CombatActionExecutor.CreateDefendAction(playerCharacter);
        
        // Execute action
        actionExecutor.ExecuteAction(action);
        
        // End turn
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