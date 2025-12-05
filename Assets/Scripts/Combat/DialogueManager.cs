using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    /// <summary>
    /// Dialogue trigger conditions
    /// </summary>
    public enum DialogueTrigger
    {
        OnCombatStart,
        OnTurnStart,
        OnHealthLow,        // Character below 25% HP
        OnAllyDefeated,
        OnEnemyDefeated,
        OnStatusApplied,
        OnAbilityUsed,
        OnRoundNumber,      // Specific round
        Manual              // Triggered by player action
    }

    /// <summary>
    /// A single dialogue line
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        public string text;
        [Tooltip("How long to display this line (0 = wait for input)")]
        public float displayDuration = 3f;
        [Tooltip("Optional: Character who says this line")]
        public CombatCharacter speaker;
    }

    /// <summary>
    /// A conversation that can trigger during combat
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Greenveil/Combat Dialogue")]
    public class CombatDialogue : ScriptableObject
    {
        [Header("Dialogue Info")]
        [SerializeField] private string dialogueId;
        [SerializeField] private DialogueTrigger trigger;
        
        [Header("Trigger Conditions")]
        [SerializeField] private int triggerRound = 1; // For OnRoundNumber trigger
        [SerializeField] private bool triggerOnce = true; // Only play once
        [SerializeField] private int priority = 0; // Higher = plays first if multiple trigger
        
        [Header("Dialogue Lines")]
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();
        
        [Header("Effects")]
        [SerializeField] private bool pauseCombat = true; // Pause turn timer during dialogue
        [SerializeField] private bool canSkip = true;

        private bool hasPlayed = false;

        #region Properties
        public string DialogueId => dialogueId;
        public DialogueTrigger Trigger => trigger;
        public int TriggerRound => triggerRound;
        public bool TriggerOnce => triggerOnce;
        public int Priority => priority;
        public List<DialogueLine> Lines => lines;
        public bool PauseCombat => pauseCombat;
        public bool CanSkip => canSkip;
        public bool HasPlayed => hasPlayed;
        #endregion

        /// <summary>
        /// Mark this dialogue as played
        /// </summary>
        public void MarkAsPlayed()
        {
            hasPlayed = true;
        }

        /// <summary>
        /// Reset played state (for new combat)
        /// </summary>
        public void Reset()
        {
            hasPlayed = false;
        }

        /// <summary>
        /// Check if this dialogue should trigger
        /// </summary>
        public bool ShouldTrigger(DialogueTrigger currentTrigger, int roundNumber = 0)
        {
            if (triggerOnce && hasPlayed) return false;
            if (trigger != currentTrigger) return false;
            
            if (trigger == DialogueTrigger.OnRoundNumber && roundNumber != triggerRound)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Manages dialogue during combat
    /// Attach to CombatManager or persistent GameObject
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [Header("Available Dialogues")]
        [SerializeField] private List<CombatDialogue> combatDialogues = new List<CombatDialogue>();
        
        [Header("Current Dialogue")]
        [SerializeField] private bool isPlayingDialogue = false;
        [SerializeField] private CombatDialogue currentDialogue;
        [SerializeField] private int currentLineIndex = 0;

        private TurnOrderManager turnManager;

        // Events
        public System.Action<DialogueLine> OnDialogueLineStart;
        public System.Action OnDialogueLineEnd;
        public System.Action<CombatDialogue> OnDialogueStart;
        public System.Action OnDialogueEnd;

        #region Properties
        public bool IsPlayingDialogue => isPlayingDialogue;
        public CombatDialogue CurrentDialogue => currentDialogue;
        public DialogueLine CurrentLine => currentDialogue?.Lines[currentLineIndex];
        #endregion

        void Awake()
        {
            turnManager = GetComponent<TurnOrderManager>();
        }

        void Start()
        {
            // Subscribe to combat events
            if (turnManager != null)
            {
                turnManager.OnCombatStart += () => TriggerDialogue(DialogueTrigger.OnCombatStart);
                turnManager.OnNewRound += (round) => TriggerDialogue(DialogueTrigger.OnRoundNumber, round);
                turnManager.OnTurnStart += (character) => TriggerDialogue(DialogueTrigger.OnTurnStart);
            }
        }

        #region Trigger Dialogues
        /// <summary>
        /// Attempt to trigger dialogue based on condition
        /// </summary>
        public void TriggerDialogue(DialogueTrigger trigger, int roundNumber = 0)
        {
            if (isPlayingDialogue) return;

            // Find all matching dialogues
            List<CombatDialogue> matches = new List<CombatDialogue>();
            foreach (var dialogue in combatDialogues)
            {
                if (dialogue.ShouldTrigger(trigger, roundNumber))
                {
                    matches.Add(dialogue);
                }
            }

            if (matches.Count == 0) return;

            // Sort by priority
            matches.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Play highest priority dialogue
            PlayDialogue(matches[0]);
        }

        /// <summary>
        /// Manually trigger a specific dialogue by ID
        /// </summary>
        public void TriggerDialogueById(string dialogueId)
        {
            CombatDialogue dialogue = combatDialogues.Find(d => d.DialogueId == dialogueId);
            if (dialogue != null)
            {
                PlayDialogue(dialogue);
            }
            else
            {
                Debug.LogWarning($"Dialogue '{dialogueId}' not found!");
            }
        }
        #endregion

        #region Play Dialogue
        /// <summary>
        /// Start playing a dialogue
        /// </summary>
        private void PlayDialogue(CombatDialogue dialogue)
        {
            if (dialogue.Lines.Count == 0)
            {
                Debug.LogWarning($"Dialogue '{dialogue.DialogueId}' has no lines!");
                return;
            }

            currentDialogue = dialogue;
            currentLineIndex = 0;
            isPlayingDialogue = true;

            Debug.Log($"=== Starting Dialogue: {dialogue.DialogueId} ===");
            OnDialogueStart?.Invoke(dialogue);

            // Mark as played if trigger once
            if (dialogue.TriggerOnce)
            {
                dialogue.MarkAsPlayed();
            }

            ShowNextLine();
        }

        /// <summary>
        /// Show next line of dialogue
        /// </summary>
        public void ShowNextLine()
        {
            if (currentDialogue == null || currentLineIndex >= currentDialogue.Lines.Count)
            {
                EndDialogue();
                return;
            }

            DialogueLine line = currentDialogue.Lines[currentLineIndex];
            Debug.Log($"{line.speakerName}: {line.text}");
            
            OnDialogueLineStart?.Invoke(line);

            // Auto-advance if duration is set
            if (line.displayDuration > 0)
            {
                Invoke(nameof(AdvanceLine), line.displayDuration);
            }
        }

        /// <summary>
        /// Advance to next line (called by UI button or timer)
        /// </summary>
        public void AdvanceLine()
        {
            CancelInvoke(nameof(AdvanceLine)); // Cancel any pending auto-advance
            
            OnDialogueLineEnd?.Invoke();
            currentLineIndex++;
            ShowNextLine();
        }

        /// <summary>
        /// Skip entire dialogue
        /// </summary>
        public void SkipDialogue()
        {
            if (currentDialogue != null && currentDialogue.CanSkip)
            {
                Debug.Log("Dialogue skipped!");
                EndDialogue();
            }
        }

        /// <summary>
        /// End current dialogue
        /// </summary>
        private void EndDialogue()
        {
            CancelInvoke(nameof(AdvanceLine));
            
            Debug.Log($"=== Dialogue Ended: {currentDialogue.DialogueId} ===");
            OnDialogueEnd?.Invoke();

            currentDialogue = null;
            currentLineIndex = 0;
            isPlayingDialogue = false;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Add dialogue to available list
        /// </summary>
        public void AddDialogue(CombatDialogue dialogue)
        {
            if (!combatDialogues.Contains(dialogue))
            {
                combatDialogues.Add(dialogue);
            }
        }

        /// <summary>
        /// Reset all dialogues (for new combat)
        /// </summary>
        public void ResetAllDialogues()
        {
            foreach (var dialogue in combatDialogues)
            {
                dialogue.Reset();
            }
        }
        #endregion
    }
}
