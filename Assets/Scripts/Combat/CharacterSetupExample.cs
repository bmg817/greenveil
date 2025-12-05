using UnityEngine;
using System.Collections.Generic;

namespace Greenveil.Combat
{
    /// <summary>
    /// Example setup showing how to create and configure characters
    /// This demonstrates Alder Finch - Forest Ranger
    /// </summary>
    public class CharacterSetupExample : MonoBehaviour
    {
        [Header("Character Abilities")]
        [SerializeField] private Ability basicAttack;
        [SerializeField] private Ability brambleBind;
        [SerializeField] private Ability echoVerse;
        [SerializeField] private Ability huntersMemory;
        
        private CombatCharacter alderCharacter;
        private List<Ability> alderAbilities = new List<Ability>();

        private void Start()
        {
            SetupAlderFinch();
        }

        /// <summary>
        /// Example: Setup Alder Finch character with stats and abilities
        /// </summary>
        private void SetupAlderFinch()
        {
            // Get or add CombatCharacter component
            alderCharacter = gameObject.GetComponent<CombatCharacter>();
            if (alderCharacter == null)
            {
                alderCharacter = gameObject.AddComponent<CombatCharacter>();
            }
            
            // Character stats are set in the Inspector, but here's how to access them:
            // The character should have these stats set in Inspector:
            // Name: "Alder Finch"
            // Role: Hybrid
            // Max Health: 120
            // Max MP: 100
            // Attack: 15
            // Defense: 8
            // Speed: 65
            // Primary Element: Earth
            
            Debug.Log("Alder Finch character setup complete!");
            alderCharacter.PrintStats();
        }

        /// <summary>
        /// Example: Use an ability in combat
        /// </summary>
        public void UseAbilityExample(Ability ability, List<CombatCharacter> targets)
        {
            if (ability.CanUse(alderCharacter))
            {
                ability.Use(alderCharacter, targets);
            }
            else
            {
                Debug.LogWarning($"Cannot use {ability.AbilityName} - insufficient resources!");
            }
        }

        /// <summary>
        /// Example: Subscribe to character events
        /// </summary>
        private void SubscribeToEvents()
        {
            alderCharacter.OnHealthChanged += HandleHealthChanged;
            alderCharacter.OnMPChanged += HandleMPChanged;
            alderCharacter.OnStatusEffectApplied += HandleStatusEffect;
            alderCharacter.OnCharacterDefeated += HandleDefeat;
        }

        private void HandleHealthChanged(float current, float max)
        {
            Debug.Log($"Alder's HP: {current}/{max} ({(current/max)*100}%)");
            // Update UI here
        }

        private void HandleMPChanged(float current, float max)
        {
            Debug.Log($"Alder's MP: {current}/{max} ({(current/max)*100}%)");
            // Update UI here
        }

        private void HandleStatusEffect(StatusEffect effect)
        {
            Debug.Log($"Alder is now affected by: {effect.EffectName}");
            // Show status icon in UI
        }

        private void HandleDefeat()
        {
            Debug.Log("Alder has been defeated!");
            // Play defeat animation, update UI
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (alderCharacter != null)
            {
                alderCharacter.OnHealthChanged -= HandleHealthChanged;
                alderCharacter.OnMPChanged -= HandleMPChanged;
                alderCharacter.OnStatusEffectApplied -= HandleStatusEffect;
                alderCharacter.OnCharacterDefeated -= HandleDefeat;
            }
        }
    }
}
