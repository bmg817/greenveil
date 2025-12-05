using UnityEngine;

namespace Greenveil.Combat
{
    public static class CharacterPresets
    {
        public static void ApplyAlderPreset(CharacterConfig config)
        {
            config.characterName = "Alder";
            config.role = CharacterRole.Damage;
            config.maxHealth = 100f;
            config.maxMP = 20f;
            config.attack = 25f;
            config.defense = 10f;
            config.speed = 10;
            config.primaryElement = ElementType.Fire;
        }

        public static void ApplyMiriPreset(CharacterConfig config)
        {
            config.characterName = "Miri";
            config.role = CharacterRole.Support;
            config.maxHealth = 80f;
            config.maxMP = 30f;
            config.attack = 22f;
            config.defense = 8f;
            config.speed = 15;
            config.primaryElement = ElementType.Nature;
        }

        public static void ApplyThornPreset(CharacterConfig config)
        {
            config.characterName = "Thorn";
            config.role = CharacterRole.Tank;
            config.maxHealth = 150f;
            config.maxMP = 20f;
            config.attack = 20f;
            config.defense = 15f;
            config.speed = 7;
            config.primaryElement = ElementType.Earth;
        }
    }

#if UNITY_EDITOR
    public static class CharacterConfigCreator
    {
        [UnityEditor.MenuItem("Greenveil/Create Character Configs")]
        public static void CreateAllConfigs()
        {
            CreateConfig("Alder", CharacterPresets.ApplyAlderPreset);
            CreateConfig("Miri", CharacterPresets.ApplyMiriPreset);
            CreateConfig("Thorn", CharacterPresets.ApplyThornPreset);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Created Alder, Miri, and Thorn configs in Assets/Data/Characters/");
        }

        private static void CreateConfig(string name, System.Action<CharacterConfig> applyPreset)
        {
            string folder = "Assets/Data/Characters";
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Data"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Data");
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets/Data", "Characters");

            string path = $"{folder}/{name}Config.asset";
            
            CharacterConfig existing = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterConfig>(path);
            if (existing != null)
            {
                Debug.Log($"{name} config already exists at {path}");
                return;
            }

            CharacterConfig config = ScriptableObject.CreateInstance<CharacterConfig>();
            applyPreset(config);
            UnityEditor.AssetDatabase.CreateAsset(config, path);
            Debug.Log($"Created {name} config at {path}");
        }
    }
#endif
}