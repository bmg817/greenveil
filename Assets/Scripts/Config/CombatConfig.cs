using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace Greenveil.Combat
{
    [Serializable]
    public class AbilityData
    {
        public string id;
        public string name;
        public string description;
        public string type;
        public string target;
        public float mpCostPercent;
        public float mpRestorePercent;
        public float hpCostPercent;
        public float basePower;
        public string element;
        public string statusEffect;
        public float statusChance;
        public int duration;
        public bool isMultiHit;
        public int hitCount;
        public bool canCrit;
        public float critChance;
        public float critMultiplier;
        public bool isCleanse;
        public string statusEffect2;
        public float statusChance2;
        public int duration2;
        public string selfStatusEffect;
        public int selfDuration;
        public float selfMagnitude;
    }

    [Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public string role;
        public string element;
        public float maxHealth;
        public float maxMP;
        public float attack;
        public float defense;
        public int speed;
        public string basicAttackId;
        public string[] skillIds;
    }

    [Serializable]
    public class AbilityDataList
    {
        public AbilityData[] abilities;
    }

    [Serializable]
    public class CharacterDataList
    {
        public CharacterData[] characters;
    }

    public static class CombatConfig
    {
        private static Dictionary<string, Ability> abilities = new Dictionary<string, Ability>();
        private static Dictionary<string, CharacterConfig> characters = new Dictionary<string, CharacterConfig>();
        private static bool loaded;

        public static void Load()
        {
            if (loaded) return;

            string configPath = Path.Combine(Application.streamingAssetsPath, "Config");

            string abilitiesJson = File.ReadAllText(Path.Combine(configPath, "abilities.json"));
            var abilityDataList = JsonUtility.FromJson<AbilityDataList>(abilitiesJson);
            foreach (var data in abilityDataList.abilities)
                abilities[data.id] = ConvertAbility(data);

            string charactersJson = File.ReadAllText(Path.Combine(configPath, "characters.json"));
            var characterDataList = JsonUtility.FromJson<CharacterDataList>(charactersJson);
            foreach (var data in characterDataList.characters)
                characters[data.id] = ConvertCharacter(data);

            loaded = true;
            Debug.Log($"[CombatConfig] Loaded {abilities.Count} abilities and {characters.Count} characters");
        }

        public static Ability GetAbility(string id)
        {
            if (!loaded) Load();
            return abilities.TryGetValue(id, out var ability) ? ability : null;
        }

        public static CharacterConfig GetCharacter(string id)
        {
            if (!loaded) Load();
            return characters.TryGetValue(id, out var config) ? config : null;
        }

        public static Ability[] GetAbilitiesForCharacter(string characterId)
        {
            if (!loaded) Load();
            var config = GetCharacter(characterId);
            if (config == null || config.skillIds == null) return new Ability[0];

            var result = new List<Ability>();
            foreach (var skillId in config.skillIds)
            {
                var ability = GetAbility(skillId);
                if (ability != null) result.Add(ability);
            }
            return result.ToArray();
        }

        private static Ability ConvertAbility(AbilityData data)
        {
            return new Ability
            {
                id = data.id,
                abilityName = data.name,
                description = data.description ?? "",
                abilityType = ParseEnum<AbilityType>(data.type),
                targetType = ParseEnum<TargetType>(data.target),
                mpCostPercent = data.mpCostPercent,
                mpRestorePercent = data.mpRestorePercent,
                hpCostPercent = data.hpCostPercent,
                basePower = data.basePower,
                element = ParseEnum<ElementType>(data.element),
                statusEffect = string.IsNullOrEmpty(data.statusEffect)
                    ? default
                    : ParseEnum<StatusEffectType>(data.statusEffect),
                statusChance = data.statusChance,
                duration = data.duration,
                isMultiHit = data.isMultiHit,
                hitCount = data.hitCount > 0 ? data.hitCount : 1,
                canCrit = data.canCrit,
                critChance = data.critChance,
                critMultiplier = data.critMultiplier > 0 ? data.critMultiplier : 1.5f,
                isCleanse = data.isCleanse,
                statusEffect2 = string.IsNullOrEmpty(data.statusEffect2)
                    ? default
                    : ParseEnum<StatusEffectType>(data.statusEffect2),
                statusChance2 = data.statusChance2,
                duration2 = data.duration2,
                selfStatusEffect = string.IsNullOrEmpty(data.selfStatusEffect)
                    ? default
                    : ParseEnum<StatusEffectType>(data.selfStatusEffect),
                selfDuration = data.selfDuration,
                selfMagnitude = data.selfMagnitude
            };
        }

        private static CharacterConfig ConvertCharacter(CharacterData data)
        {
            return new CharacterConfig
            {
                id = data.id,
                characterName = data.name,
                role = ParseEnum<CharacterRole>(data.role),
                primaryElement = ParseEnum<ElementType>(data.element),
                maxHealth = data.maxHealth,
                maxMP = data.maxMP,
                attack = data.attack,
                defense = data.defense,
                speed = data.speed,
                basicAttackId = data.basicAttackId,
                skillIds = data.skillIds
            };
        }

        private static T ParseEnum<T>(string value) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return default;
            if (Enum.TryParse<T>(value, true, out var result)) return result;
            Debug.LogWarning($"[CombatConfig] Could not parse '{value}' as {typeof(T).Name}");
            return default;
        }
    }
}
