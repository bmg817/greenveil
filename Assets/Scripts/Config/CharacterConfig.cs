using System;

namespace Greenveil.Combat
{
    [Serializable]
    public class CharacterConfig
    {
        public string id;
        public string characterName;
        public CharacterRole role;
        public ElementType primaryElement;
        public float maxHealth;
        public float maxMP;
        public float attack;
        public float defense;
        public int speed;
        public string basicAttackId;
        public string[] skillIds;
    }
}
