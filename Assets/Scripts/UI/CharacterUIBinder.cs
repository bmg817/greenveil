using UnityEngine;

public class CharacterUIBinder : MonoBehaviour
{
    [SerializeField] private CharacterCombat combat;
    [SerializeField] private CharacterUIController ui;

    private void Start()
    {
        ui.Initialize(combat.MaxHP, combat.MaxMP);

        combat.OnHealthChanged += ui.SetHealth;
        combat.OnMPChanged += ui.SetMP;
        combat.OnStatusEffectsChanged += ui.UpdateStatusEffects;
    }

    private void OnDestroy()
    {
        combat.OnHealthChanged -= ui.SetHealth;
        combat.OnMPChanged -= ui.SetMP;
        combat.OnStatusEffectsChanged -= ui.UpdateStatusEffects;
    }
}