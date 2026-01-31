using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    public enum DialogueTrigger
    {
        OnCombatStart,
        OnTurnStart,
        OnHealthLow,
        OnAllyDefeated,
        OnEnemyDefeated,
        OnStatusApplied,
        OnAbilityUsed,
        OnRoundNumber,
        Manual
    }

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        public string text;
        public float displayDuration = 3f;
        public CombatCharacter speaker;
    }

    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Greenveil/Combat Dialogue")]
    public class CombatDialogue : ScriptableObject
    {
        [SerializeField] private string dialogueId;
        [SerializeField] private DialogueTrigger trigger;
        [SerializeField] private int triggerRound = 1;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private int priority = 0;
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();
        [SerializeField] private bool pauseCombat = true;
        [SerializeField] private bool canSkip = true;

        private bool hasPlayed = false;

        public string DialogueId => dialogueId;
        public DialogueTrigger Trigger => trigger;
        public int TriggerRound => triggerRound;
        public bool TriggerOnce => triggerOnce;
        public int Priority => priority;
        public List<DialogueLine> Lines => lines;
        public bool PauseCombat => pauseCombat;
        public bool CanSkip => canSkip;
        public bool HasPlayed => hasPlayed;

        public void MarkAsPlayed()
        {
            hasPlayed = true;
        }

        public void Reset()
        {
            hasPlayed = false;
        }

        public bool ShouldTrigger(DialogueTrigger currentTrigger, int roundNumber = 0)
        {
            if (triggerOnce && hasPlayed) return false;
            if (trigger != currentTrigger) return false;

            if (trigger == DialogueTrigger.OnRoundNumber && roundNumber != triggerRound)
                return false;

            return true;
        }
    }

    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] private List<CombatDialogue> combatDialogues = new List<CombatDialogue>();
        [SerializeField] private bool isPlayingDialogue = false;
        [SerializeField] private CombatDialogue currentDialogue;
        [SerializeField] private int currentLineIndex = 0;

        private TurnOrderManager turnManager;

        public System.Action<DialogueLine> OnDialogueLineStart;
        public System.Action OnDialogueLineEnd;
        public System.Action<CombatDialogue> OnDialogueStart;
        public System.Action OnDialogueEnd;

        public bool IsPlayingDialogue => isPlayingDialogue;
        public CombatDialogue CurrentDialogue => currentDialogue;
        public DialogueLine CurrentLine => currentDialogue?.Lines[currentLineIndex];

        void Awake()
        {
            turnManager = GetComponent<TurnOrderManager>();
        }

        void Start()
        {
            if (turnManager != null)
            {
                turnManager.OnCombatStart += () => TriggerDialogue(DialogueTrigger.OnCombatStart);
                turnManager.OnNewRound += (round) => TriggerDialogue(DialogueTrigger.OnRoundNumber, round);
                turnManager.OnTurnStart += (character) => TriggerDialogue(DialogueTrigger.OnTurnStart);
            }
        }

        public void TriggerDialogue(DialogueTrigger trigger, int roundNumber = 0)
        {
            if (isPlayingDialogue) return;

            List<CombatDialogue> matches = new List<CombatDialogue>();
            foreach (var dialogue in combatDialogues)
            {
                if (dialogue.ShouldTrigger(trigger, roundNumber))
                    matches.Add(dialogue);
            }

            if (matches.Count == 0) return;

            matches.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            PlayDialogue(matches[0]);
        }

        public void TriggerDialogueById(string dialogueId)
        {
            CombatDialogue dialogue = combatDialogues.Find(d => d.DialogueId == dialogueId);
            if (dialogue != null)
                PlayDialogue(dialogue);
            else
                Debug.LogWarning($"Dialogue '{dialogueId}' not found!");
        }

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

            if (dialogue.TriggerOnce)
                dialogue.MarkAsPlayed();

            ShowNextLine();
        }

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

            if (line.displayDuration > 0)
                Invoke(nameof(AdvanceLine), line.displayDuration);
        }

        public void AdvanceLine()
        {
            CancelInvoke(nameof(AdvanceLine));

            OnDialogueLineEnd?.Invoke();
            currentLineIndex++;
            ShowNextLine();
        }

        public void SkipDialogue()
        {
            if (currentDialogue != null && currentDialogue.CanSkip)
            {
                Debug.Log("Dialogue skipped!");
                EndDialogue();
            }
        }

        private void EndDialogue()
        {
            CancelInvoke(nameof(AdvanceLine));

            Debug.Log($"=== Dialogue Ended: {currentDialogue.DialogueId} ===");
            OnDialogueEnd?.Invoke();

            currentDialogue = null;
            currentLineIndex = 0;
            isPlayingDialogue = false;
        }

        public void AddDialogue(CombatDialogue dialogue)
        {
            if (!combatDialogues.Contains(dialogue))
                combatDialogues.Add(dialogue);
        }

        public void ResetAllDialogues()
        {
            foreach (var dialogue in combatDialogues)
                dialogue.Reset();
        }
    }
}
