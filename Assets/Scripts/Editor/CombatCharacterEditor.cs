#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Greenveil.Combat
{
    [CustomEditor(typeof(CombatCharacter))]
    public class CombatCharacterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CombatCharacter character = (CombatCharacter)target;
            
            serializedObject.Update();
            
            SerializedProperty configProp = serializedObject.FindProperty("config");
            EditorGUILayout.PropertyField(configProp);
            
            if (configProp.objectReferenceValue != null)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Apply Config to Character", GUILayout.Height(30)))
                {
                    Undo.RecordObject(character, "Apply Character Config");
                    character.ApplyConfig((CharacterConfig)configProp.objectReferenceValue);
                    EditorUtility.SetDirty(character);
                }
                EditorGUILayout.Space(5);
            }
            
            serializedObject.ApplyModifiedProperties();
            
            DrawDefaultInspector();
        }
    }
}
#endif