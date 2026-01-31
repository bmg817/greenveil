using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Greenveil.Combat
{
    public class TurnOrderManager : MonoBehaviour
    {
        private List<CombatCharacter> playerCharacters = new List<CombatCharacter>();
        private List<CombatCharacter> enemyCharacters = new List<CombatCharacter>();

        private const float TICK_THRESHOLD = 100f;
        private const float SPEED_BASE = 10f;
        private Dictionary<CombatCharacter, float> tickCounters = new Dictionary<CombatCharacter, float>();
        private HashSet<CombatCharacter> actedThisRound = new HashSet<CombatCharacter>();
        private CombatCharacter currentCharacter;

        private bool combatActive = false;
        private int roundNumber = 1;

        public System.Action<CombatCharacter> OnTurnStart;
        public System.Action<CombatCharacter> OnTurnEnd;
        public System.Action<int> OnNewRound;
        public System.Action OnCombatStart;
        public System.Action<bool> OnCombatEnd;
        public System.Action<bool> OnFleeAttempt;

        public CombatCharacter CurrentCharacter => currentCharacter;
        public bool IsCombatActive => combatActive;
        public int RoundNumber => roundNumber;
        public List<CombatCharacter> TurnOrder => GetUpcomingTurns(20);

        public void InitializeCombat(List<CombatCharacter> players, List<CombatCharacter> enemies)
        {
            playerCharacters = new List<CombatCharacter>(players);
            enemyCharacters = new List<CombatCharacter>(enemies);

            tickCounters.Clear();
            actedThisRound.Clear();

            foreach (var c in playerCharacters.Concat(enemyCharacters))
            {
                if (c.IsAlive)
                    tickCounters[c] = TICK_THRESHOLD / (SPEED_BASE + Mathf.Max(1, c.GetModifiedSpeed()));
            }

            combatActive = true;
            roundNumber = 1;

            OnCombatStart?.Invoke();

            Debug.Log("=== Combat Started ===");
            PrintTurnOrder();

            AdvanceToNextCharacter();
        }

        private void AdvanceToNextCharacter()
        {
            if (!combatActive) return;

            var dead = tickCounters.Keys.Where(c => !c.IsAlive).ToList();
            foreach (var d in dead)
            {
                tickCounters.Remove(d);
                actedThisRound.Remove(d);
            }

            if (tickCounters.Count == 0) return;

            CombatCharacter next = null;
            float lowestTick = float.MaxValue;
            foreach (var kvp in tickCounters)
            {
                if (kvp.Value < lowestTick || (kvp.Value == lowestTick && Random.value > 0.5f))
                {
                    lowestTick = kvp.Value;
                    next = kvp.Key;
                }
            }

            if (lowestTick > 0)
            {
                var keys = tickCounters.Keys.ToList();
                foreach (var key in keys)
                    tickCounters[key] -= lowestTick;
            }

            currentCharacter = next;
            StartTurn();
        }

        private void StartTurn()
        {
            if (!combatActive) return;
            if (currentCharacter == null) return;

            if (!currentCharacter.IsAlive)
            {
                tickCounters[currentCharacter] = TICK_THRESHOLD / (SPEED_BASE + Mathf.Max(1, currentCharacter.GetModifiedSpeed()));
                AdvanceToNextCharacter();
                return;
            }

            currentCharacter.ProcessStatusEffects();

            bool canAct = true;
            foreach (var effect in currentCharacter.ActiveStatusEffects)
            {
                if (effect.PreventsAction())
                {
                    canAct = false;
                    Debug.Log($"{currentCharacter.CharacterName}'s turn is skipped due to {effect.EffectName}!");
                    break;
                }
            }

            OnTurnStart?.Invoke(currentCharacter);

            if (!canAct)
            {
                Invoke(nameof(EndTurnDelayed), 1f);
            }
            else
            {
                Debug.Log($">>> {currentCharacter.CharacterName}'s Turn (Speed: {currentCharacter.GetModifiedSpeed()}) <<<");
            }
        }

        public void EndTurn()
        {
            if (!combatActive) return;

            OnTurnEnd?.Invoke(currentCharacter);

            actedThisRound.Add(currentCharacter);
            tickCounters[currentCharacter] = TICK_THRESHOLD / (SPEED_BASE + Mathf.Max(1, currentCharacter.GetModifiedSpeed()));

            CheckVirtualRound();

            if (CheckCombatEnd()) return;

            AdvanceToNextCharacter();
        }

        private void EndTurnDelayed()
        {
            EndTurn();
        }

        private void CheckVirtualRound()
        {
            var allLiving = playerCharacters.Concat(enemyCharacters).Where(c => c.IsAlive).ToList();
            bool allActed = allLiving.All(c => actedThisRound.Contains(c));

            if (allActed)
            {
                Debug.Log($"=== Round {roundNumber} Complete ===");
                roundNumber++;
                actedThisRound.Clear();
                OnNewRound?.Invoke(roundNumber);
                Debug.Log($"=== Round {roundNumber} Start ===");
            }
        }

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

        private void EndCombat(bool playerVictory)
        {
            combatActive = false;
            OnCombatEnd?.Invoke(playerVictory);

            if (playerVictory)
                Debug.Log("=== VICTORY ===");
            else
                Debug.Log("=== DEFEAT ===");
        }

        public bool AttemptFlee()
        {
            if (!combatActive)
            {
                Debug.LogWarning("Cannot flee - combat is not active!");
                return false;
            }

            float playerAvgSpeed = CalculateAverageSpeed(playerCharacters);
            float enemyAvgSpeed = CalculateAverageSpeed(enemyCharacters);

            float fleeChance = 0.5f;
            float speedDifference = playerAvgSpeed - enemyAvgSpeed;
            fleeChance += speedDifference * 0.01f;
            fleeChance = Mathf.Clamp(fleeChance, 0.1f, 0.9f);

            bool success = Random.value < fleeChance;

            Debug.Log($"Flee attempt! Chance: {fleeChance * 100}% - {(success ? "SUCCESS" : "FAILED")}");
            OnFleeAttempt?.Invoke(success);

            if (success)
                FleeSuccess();
            else
                FleeFailed();

            return success;
        }

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
            OnCombatEnd?.Invoke(false);
        }

        private void FleeFailed()
        {
            Debug.Log("Failed to escape! Turn continues...");
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

        public bool IsPlayerCharacter(CombatCharacter character)
        {
            return playerCharacters.Contains(character);
        }

        public void RecalculateTurnOrder()
        {
            var dead = tickCounters.Keys.Where(c => !c.IsAlive).ToList();
            foreach (var d in dead)
            {
                tickCounters.Remove(d);
                actedThisRound.Remove(d);
            }

            foreach (var c in playerCharacters.Concat(enemyCharacters))
            {
                if (c.IsAlive && !tickCounters.ContainsKey(c))
                    tickCounters[c] = TICK_THRESHOLD / (SPEED_BASE + Mathf.Max(1, c.GetModifiedSpeed()));
            }

            Debug.Log("Turn order recalculated!");
            PrintTurnOrder();
        }

        public void SkipCharacterTurn(CombatCharacter character)
        {
            if (tickCounters.ContainsKey(character))
            {
                tickCounters[character] = TICK_THRESHOLD / (SPEED_BASE + Mathf.Max(1, character.GetModifiedSpeed()));
                Debug.Log($"{character.CharacterName}'s turn has been skipped!");
            }
        }

        public List<CombatCharacter> GetLivingCharacters(bool playerTeam)
        {
            List<CombatCharacter> team = playerTeam ? playerCharacters : enemyCharacters;
            return team.Where(c => c.IsAlive).ToList();
        }

        public List<CombatCharacter> GetUpcomingTurns(int count)
        {
            var result = new List<CombatCharacter>();

            if (currentCharacter != null && currentCharacter.IsAlive)
                result.Add(currentCharacter);

            var simTicks = new Dictionary<CombatCharacter, float>();
            foreach (var kvp in tickCounters)
            {
                if (kvp.Key.IsAlive)
                {
                    if (kvp.Key == currentCharacter)
                        simTicks[kvp.Key] = TICK_THRESHOLD / (SPEED_BASE + Mathf.Max(1, kvp.Key.GetModifiedSpeed()));
                    else
                        simTicks[kvp.Key] = kvp.Value;
                }
            }

            while (result.Count < count && simTicks.Count > 0)
            {
                CombatCharacter next = null;
                float lowest = float.MaxValue;
                foreach (var kvp in simTicks)
                {
                    if (kvp.Value < lowest)
                    {
                        lowest = kvp.Value;
                        next = kvp.Key;
                    }
                }

                if (next == null) break;

                var keys = simTicks.Keys.ToList();
                foreach (var key in keys)
                    simTicks[key] -= lowest;

                result.Add(next);
                simTicks[next] = TICK_THRESHOLD / (SPEED_BASE + Mathf.Max(1, next.GetModifiedSpeed()));
            }

            return result;
        }

        private void PrintTurnOrder()
        {
            var upcoming = GetUpcomingTurns(10);
            Debug.Log("Turn Order (CTB):");
            for (int i = 0; i < upcoming.Count; i++)
            {
                string marker = (i == 0) ? ">>>" : "   ";
                Debug.Log($"{marker} {i + 1}. {upcoming[i].CharacterName} (Speed: {upcoming[i].GetModifiedSpeed()})");
            }
        }
    }
}
