#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Greenveil.Combat
{
    [CustomEditor(typeof(CharacterConfig))]
    public class CharacterConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CharacterConfig config = (CharacterConfig)target;

            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Alder"))
            {
                Undo.RecordObject(config, "Apply Alder Preset");
                CharacterPresets.ApplyAlderPreset(config);
                EditorUtility.SetDirty(config);
            }
            
            if (GUILayout.Button("Miri"))
            {
                Undo.RecordObject(config, "Apply Miri Preset");
                CharacterPresets.ApplyMiriPreset(config);
                EditorUtility.SetDirty(config);
            }
            
            if (GUILayout.Button("Thorn"))
            {
                Undo.RecordObject(config, "Apply Thorn Preset");
                CharacterPresets.ApplyThornPreset(config);
                EditorUtility.SetDirty(config);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Stats Summary", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            float effectiveHP = config.maxHealth;
            float effectiveDamage = config.attack;
            float survivalScore = config.maxHealth * (1 + config.defense / 50f);
            
            EditorGUILayout.FloatField("Survival Score", survivalScore);
            EditorGUILayout.FloatField("DPS Estimate", effectiveDamage * config.speed / 10f);
            EditorGUILayout.IntField("Skills Before Empty", Mathf.FloorToInt(config.maxMP / 6f));
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif