using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Greenveil.Combat
{
    /// <summary>
    /// Manages turn order based on character speed stats
    /// </summary>
    public class TurnOrderManager : MonoBehaviour
    {
        [Header("Combat Participants")]
        [SerializeField] private List<CombatCharacter> playerCharacters = new List<CombatCharacter>();
        [SerializeField] private List<CombatCharacter> enemyCharacters = new List<CombatCharacter>();
        
        private List<CombatCharacter> turnOrder = new List<CombatCharacter>();
        private int currentTurnIndex = 0;
        
        [Header("Turn State")]
        [SerializeField] private bool combatActive = false;
        [SerializeField] private int roundNumber = 1;
        
        // Events
        public System.Action<CombatCharacter> OnTurnStart;
        public System.Action<CombatCharacter> OnTurnEnd;
        public System.Action<int> OnNewRound;
        public System.Action OnCombatStart;
        public System.Action<bool> OnCombatEnd; // bool = player victory
        public System.Action<bool> OnFleeAttempt; // bool = flee success

        #region Properties
        public CombatCharacter CurrentCharacter => turnOrder.Count > 0 ? turnOrder[currentTurnIndex] : null;
        public bool IsCombatActive => combatActive;
        public int RoundNumber => roundNumber;
        public List<CombatCharacter> TurnOrder => turnOrder;
        #endregion

        #region Combat Initialization
        /// <summary>
        /// Start combat with given characters
        /// </summary>
        public void InitializeCombat(List<CombatCharacter> players, List<CombatCharacter> enemies)
        {
            playerCharacters = new List<CombatCharacter>(players);
            enemyCharacters = new List<CombatCharacter>(enemies);
            
            // Calculate initial turn order
            CalculateTurnOrder();
            
            combatActive = true;
            roundNumber = 1;
            currentTurnIndex = 0;
            
            OnCombatStart?.Invoke();
            
            Debug.Log("=== Combat Started ===");
            PrintTurnOrder();
            
            // Start first turn
            StartTurn();
        }

        /// <summary>
        /// Calculate turn order based on speed (highest speed goes first)
        /// </summary>
        private void CalculateTurnOrder()
        {
            turnOrder.Clear();
            
            // Combine all characters
            List<CombatCharacter> allCharacters = new List<CombatCharacter>();
            allCharacters.AddRange(playerCharacters);
            allCharacters.AddRange(enemyCharacters);
            
            // Filter out defeated characters
            allCharacters = allCharacters.Where(c => c.IsAlive).ToList();
            
            // Sort by speed (descending)
            turnOrder = allCharacters.OrderByDescending(c => c.GetModifiedSpeed()).ToList();
            
            // Add some randomness for characters with same speed
            for (int i = 0; i < turnOrder.Count - 1; i++)
            {
                if (turnOrder[i].GetModifiedSpeed() == turnOrder[i + 1].GetModifiedSpeed())
                {
                    if (Random.value > 0.5f)
                    {
                        var temp = turnOrder[i];
                        turnOrder[i] = turnOrder[i + 1];
                        turnOrder[i + 1] = temp;
                    }
                }
            }
        }
        #endregion

        #region Turn Management
        /// <summary>
        /// Start the current character's turn
        /// </summary>
        private void StartTurn()
        {
            if (!combatActive) return;
            if (turnOrder.Count == 0) return;
            
            CombatCharacter currentChar = CurrentCharacter;
            
            // Skip turn if character is defeated
            if (!currentChar.IsAlive)
            {
                NextTurn();
                return;
            }
            
            // Process status effects
            currentChar.ProcessStatusEffects();
            
            // Check if status effects prevent action
            bool canAct = true;
            foreach (var effect in currentChar.ActiveStatusEffects)
            {
                if (effect.PreventsAction())
                {
                    canAct = false;
                    Debug.Log($"{currentChar.CharacterName}'s turn is skipped due to {effect.EffectName}!");
                    break;
                }
            }
            
            OnTurnStart?.Invoke(currentChar);
            
            if (!canAct)
            {
                // Auto-end turn if character can't act
                Invoke(nameof(EndTurnDelayed), 1f);
            }
            else
            {
                Debug.Log($">>> {currentChar.CharacterName}'s Turn (Speed: {currentChar.GetModifiedSpeed()}) <<<");
            }
        }

        /// <summary>
        /// End the current turn and move to next
        /// </summary>
        public void EndTurn()
        {
            if (!combatActive) return;
            
            CombatCharacter currentChar = CurrentCharacter;
            OnTurnEnd?.Invoke(currentChar);
            
            NextTurn();
        }

        private void EndTurnDelayed()
        {
            EndTurn();
        }

        /// <summary>
        /// Advance to next turn
        /// </summary>
        private void NextTurn()
        {
            currentTurnIndex++;
            
            // Check if round is complete
            if (currentTurnIndex >= turnOrder.Count)
            {
                EndRound();
                return;
            }
            
            // Check win/loss conditions
            if (CheckCombatEnd())
            {
                return;
            }
            
            StartTurn();
        }

        /// <summary>
        /// End current round and start new one
        /// </summary>
        private void EndRound()
        {
            Debug.Log($"=== Round {roundNumber} Complete ===");
            
            roundNumber++;
            currentTurnIndex = 0;
            
            // Recalculate turn order for new round
            CalculateTurnOrder();
            
            OnNewRound?.Invoke(roundNumber);
            
            Debug.Log($"=== Round {roundNumber} Start ===");
            PrintTurnOrder();
            
            StartTurn();
        }
        #endregion

        #region Combat End Conditions
        /// <summary>
        /// Check if combat should end
        /// </summary>
        private bool CheckCombatEnd()
        {
            bool allEnemiesDefeated = enemyCharacters.All(e => !e.IsAlive);
            bool allPlayersDefeated = playerCharacters.All(p => !p.IsAlive);
            
            if (allEnemiesDefeated)
            {
                EndCombat(true);
                return true;
            }
            else if (allPlayersDefeated)
            {
                EndCombat(false);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// End combat
        /// </summary>
        private void EndCombat(bool playerVictory)
        {
            combatActive = false;
            OnCombatEnd?.Invoke(playerVictory);
            
            if (playerVictory)
            {
                Debug.Log("=== VICTORY ===");
            }
            else
            {
                Debug.Log("=== DEFEAT ===");
            }
        }
        #endregion

        #region Flee System
        /// <summary>
        /// Attempt to flee from combat
        /// Success is based on party speed vs enemy speed
        /// </summary>
        public bool AttemptFlee()
        {
            if (!combatActive)
            {
                Debug.LogWarning("Cannot flee - combat is not active!");
                return false;
            }

            // Calculate average speeds
            float playerAvgSpeed = CalculateAverageSpeed(playerCharacters);
            float enemyAvgSpeed = CalculateAverageSpeed(enemyCharacters);

            // Base flee chance: 50%
            float fleeChance = 0.5f;

            // Adjust based on speed difference
            float speedDifference = playerAvgSpeed - enemyAvgSpeed;
            fleeChance += speedDifference * 0.01f; // +/- 1% per speed point difference

            // Clamp between 10% and 90%
            fleeChance = Mathf.Clamp(fleeChance, 0.1f, 0.9f);

            // Roll for success
            bool success = Random.value < fleeChance;

            Debug.Log($"Flee attempt! Chance: {fleeChance * 100}% - {(success ? "SUCCESS" : "FAILED")}");
            OnFleeAttempt?.Invoke(success);

            if (success)
            {
                FleeSuccess();
            }
            else
            {
                FleeFailed();
            }

            return success;
        }

        /// <summary>
        /// Guaranteed flee (from escape items or special abilities)
        /// </summary>
        public void GuaranteedFlee()
        {
            Debug.Log("Guaranteed flee!");
            OnFleeAttempt?.Invoke(true);
            FleeSuccess();
        }

        private void FleeSuccess()
        {
            combatActive = false;
            Debug.Log("=== ESCAPED FROM BATTLE ===");
            OnCombatEnd?.Invoke(false); // Not a victory, but not a defeat either
        }

        private void FleeFailed()
        {
            Debug.Log("Failed to escape! Turn continues...");
            // Failed flee attempts end the turn
            EndTurn();
        }

        private float CalculateAverageSpeed(List<CombatCharacter> characters)
        {
            if (characters == null || characters.Count == 0) return 0f;

            float totalSpeed = 0f;
            int aliveCount = 0;

            foreach (var character in characters)
            {
                if (character.IsAlive)
                {
                    totalSpeed += character.GetModifiedSpeed();
                    aliveCount++;
                }
            }

            return aliveCount > 0 ? totalSpeed / aliveCount : 0f;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Force recalculation of turn order (for speed buffs/debuffs)
        /// </summary>
        public void RecalculateTurnOrder()
        {
            // Store current character
            CombatCharacter current = CurrentCharacter;
            
            CalculateTurnOrder();
            
            // Try to maintain current character's position
            currentTurnIndex = turnOrder.IndexOf(current);
            if (currentTurnIndex < 0) currentTurnIndex = 0;
            
            Debug.Log("Turn order recalculated!");
            PrintTurnOrder();
        }

        /// <summary>
        /// Skip a specific character's next turn
        /// </summary>
        public void SkipCharacterTurn(CombatCharacter character)
        {
            // Remove from turn order temporarily
            int index = turnOrder.IndexOf(character);
            if (index >= 0)
            {
                turnOrder.RemoveAt(index);
                
                // Adjust current index if needed
                if (index < currentTurnIndex)
                {
                    currentTurnIndex--;
                }
                
                Debug.Log($"{character.CharacterName}'s turn has been skipped!");
            }
        }

        /// <summary>
        /// Get all living characters on a team
        /// </summary>
        public List<CombatCharacter> GetLivingCharacters(bool playerTeam)
        {
            List<CombatCharacter> team = playerTeam ? playerCharacters : enemyCharacters;
            return team.Where(c => c.IsAlive).ToList();
        }

        /// <summary>
        /// Debug print current turn order
        /// </summary>
        private void PrintTurnOrder()
        {
            Debug.Log("Turn Order:");
            for (int i = 0; i < turnOrder.Count; i++)
            {
                string marker = (i == currentTurnIndex) ? ">>>" : "   ";
                Debug.Log($"{marker} {i + 1}. {turnOrder[i].CharacterName} (Speed: {turnOrder[i].GetModifiedSpeed()})");
            }
        }
        #endregion
    }
}