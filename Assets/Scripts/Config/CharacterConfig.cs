using UnityEngine;

namespace Greenveil.Combat
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Greenveil/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("Identity")]
        public string characterName = "New Character";
        public CharacterRole role = CharacterRole.Damage;
        public Sprite portrait;
        
        [Header("Base Stats")]
        [Range(1, 500)] public float maxHealth = 100f;
        [Range(1, 100)] public float maxMP = 20f;
        [Range(1, 100)] public float attack = 10f;
        [Range(0, 50)] public float defense = 5f;
        [Range(1, 100)] public int speed = 50;
        
        [Header("Element")]
        public ElementType primaryElement = ElementType.Neutral;
        
        [Header("Abilities")]
        public Ability basicAttack;
        public Ability[] skills;
        
        [Header("Visuals")]
        public Color characterColor = Color.white;
        public RuntimeAnimatorController animator;
        
        [Header("Audio")]
        public AudioClip attackSound;
        public AudioClip hurtSound;
        public AudioClip defeatSound;
    }
}