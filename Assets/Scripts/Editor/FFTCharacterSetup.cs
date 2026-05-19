using UnityEngine;
using UnityEditor;
using Greenveil.Combat;

public class FFTCharacterSetup : EditorWindow
{
    [MenuItem("Tools/Setup FFT Characters")]
    public static void SetupFFTCharacters()
    {
        var starter = Object.FindFirstObjectByType<CombatStarter>();
        if (starter == null)
        {
            EditorUtility.DisplayDialog("Error", "No CombatStarter found in scene. Open your combat scene first.", "OK");
            return;
        }

        string[] charIds = { "ramza", "agrias", "beowulf", "orlandu" };
        string[] charNames = { "Ramza Beoulve", "Agrias Oaks", "Beowulf Kadmus", "Cidolfas Orlandu" };
        string[] spriteFiles = { "Ramza_Beoulve", "Agrias_Oaks", "Beowulf_Kadmus", "Cidolfas_Orlandu" };
        Color[] colors = {
            new Color(0.9f, 0.8f, 0.3f),  // Ramza - gold
            new Color(0.3f, 0.5f, 0.9f),  // Agrias - blue
            new Color(0.6f, 0.3f, 0.7f),  // Beowulf - purple
            new Color(0.8f, 0.2f, 0.2f)   // Orlandu - crimson
        };

        var playerList = starter.GetType().GetField("playerCharacters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (playerList == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not access playerCharacters field on CombatStarter.", "OK");
            return;
        }

        var players = playerList.GetValue(starter) as System.Collections.Generic.List<CombatCharacter>;

        int created = 0;
        for (int i = 0; i < charIds.Length; i++)
        {
            // Skip if already exists in scene
            bool exists = false;
            foreach (var existing in Object.FindObjectsByType<CombatCharacter>(FindObjectsSortMode.None))
            {
                if (existing.CharacterId == charIds[i])
                {
                    exists = true;
                    Debug.Log($"[FFT Setup] {charNames[i]} already exists in scene, skipping.");
                    break;
                }
            }
            if (exists) continue;

            // Create the character GameObject
            GameObject charObj = new GameObject(charNames[i]);
            Undo.RegisterCreatedObjectUndo(charObj, "Create FFT Character");

            // Add CombatCharacter and set the characterId via SerializedObject
            var combatChar = charObj.AddComponent<CombatCharacter>();
            var so = new SerializedObject(combatChar);
            so.FindProperty("characterId").stringValue = charIds[i];
            so.ApplyModifiedProperties();

            // Add CharacterVisual and configure it
            var visual = charObj.AddComponent<CharacterVisual>();
            var visualSO = new SerializedObject(visual);
            visualSO.FindProperty("characterColor").colorValue = colors[i];

            // Try to find and assign the sprite
            string[] guids = AssetDatabase.FindAssets(spriteFiles[i] + " t:Sprite", new[] { "Assets/Sprites" });
            if (guids.Length == 0)
                guids = AssetDatabase.FindAssets(spriteFiles[i] + " t:Texture2D", new[] { "Assets/Sprites" });

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                // For sprite sheets, load all sprites and use the first one
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                Sprite foundSprite = null;
                foreach (var asset in allAssets)
                {
                    if (asset is Sprite s)
                    {
                        foundSprite = s;
                        break;
                    }
                }
                // Fallback to loading as single sprite
                if (foundSprite == null)
                    foundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (foundSprite != null)
                {
                    visualSO.FindProperty("characterSprite").objectReferenceValue = foundSprite;
                    Debug.Log($"[FFT Setup] Assigned sprite for {charNames[i]} from {path}");
                }
                else
                {
                    Debug.LogWarning($"[FFT Setup] Found texture but no sprite for {charNames[i]} at {path}. Set texture import mode to Sprite in Inspector.");
                }
            }
            else
            {
                Debug.LogWarning($"[FFT Setup] No sprite found for {spriteFiles[i]} in Assets/Sprites/. Assign manually in Inspector.");
            }

            visualSO.ApplyModifiedProperties();

            // Add to player characters list
            players.Add(combatChar);
            created++;
            Debug.Log($"[FFT Setup] Created {charNames[i]} (id: {charIds[i]})");
        }

        if (created > 0)
        {
            EditorUtility.SetDirty(starter);
            EditorUtility.DisplayDialog("FFT Setup Complete",
                $"Created {created} FFT character(s) and added them to CombatStarter.playerCharacters.\n\n" +
                "Characters added:\n" +
                "- Ramza Beoulve (Damage / Light)\n" +
                "- Agrias Oaks (Tank / Light)\n" +
                "- Beowulf Kadmus (Support / Dark)\n" +
                "- Cidolfas Orlandu (Damage / Dark)\n\n" +
                "If sprites show as colored rectangles, select each sprite in Assets/Sprites/ and set Texture Type to 'Sprite (2D and UI)' in the Inspector, then re-run this tool.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("FFT Setup", "All FFT characters already exist in the scene.", "OK");
        }
    }
}
