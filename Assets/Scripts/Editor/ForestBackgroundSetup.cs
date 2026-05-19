using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ForestBackgroundSetup : EditorWindow
{
    [MenuItem("Tools/Setup Forest Background")]
    public static void SetupForestBackground()
    {
        // Check if ForestBackground already exists
        var existing = Object.FindFirstObjectByType<ForestBackground>();
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Forest Background",
                "A ForestBackground already exists in the scene. Replace it?", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // Create the background GameObject
        GameObject bgObj = new GameObject("ForestBackground");
        Undo.RegisterCreatedObjectUndo(bgObj, "Create Forest Background");
        var forestBg = bgObj.AddComponent<ForestBackground>();

        var so = new SerializedObject(forestBg);

        // Categorize sprites
        string bgFolder = "Assets/Sprites/Background";

        string[] largeNames = {
            "Mega_tree1", "Mega_tree2", "Luminous_tree1", "Luminous_tree2", "Luminous_tree3"
        };

        string[] mediumNames = {
            "Curved_tree1", "Curved_tree2", "Willow1", "Willow2",
            "White_tree1", "White_tree2", "Blue-green_balls_tree1",
            "Light_balls_tree1", "Swirling tree1", "Swirling tree2"
        };

        string[] smallNames = {
            "Chanterelles1", "Chanterelles2", "Beige_green_mushroom1",
            "Beige_green_mushroom2", "White-red_mushroom1", "White-red_mushroom2",
            "Tree_idol_deer", "Living gazebo1"
        };

        var largeSpritesList = FindSprites(largeNames, bgFolder);
        var mediumSpritesList = FindSprites(mediumNames, bgFolder);
        var smallSpritesList = FindSprites(smallNames, bgFolder);

        SetSpriteArray(so, "largeTrees", largeSpritesList);
        SetSpriteArray(so, "mediumTrees", mediumSpritesList);
        SetSpriteArray(so, "smallObjects", smallSpritesList);

        so.ApplyModifiedProperties();

        // Update camera background color to dark forest green
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Change Camera Background");
            cam.backgroundColor = new Color(0.08f, 0.14f, 0.05f, 1f);
            EditorUtility.SetDirty(cam);
        }

        int total = largeSpritesList.Count + mediumSpritesList.Count + smallSpritesList.Count;
        EditorUtility.DisplayDialog("Forest Background Setup",
            $"Created ForestBackground with {total} sprites assigned:\n\n" +
            $"  Large trees: {largeSpritesList.Count}\n" +
            $"  Medium trees: {mediumSpritesList.Count}\n" +
            $"  Small objects: {smallSpritesList.Count}\n\n" +
            "Camera background updated to forest green.\n\n" +
            "If sprites are missing, select PNGs in Assets/Sprites/Background/,\n" +
            "set Texture Type to 'Sprite (2D and UI)', then re-run this tool.",
            "OK");
    }

    static List<Sprite> FindSprites(string[] names, string folder)
    {
        var sprites = new List<Sprite>();
        foreach (string name in names)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite", new[] { folder });
            if (guids.Length == 0)
                guids = AssetDatabase.FindAssets(name + " t:Texture2D", new[] { folder });

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                // Try loading as sprite first
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    // Try loading sub-assets (for sprite sheets)
                    Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var a in all)
                    {
                        if (a is Sprite s)
                        {
                            sprite = s;
                            break;
                        }
                    }
                }
                if (sprite != null)
                    sprites.Add(sprite);
                else
                    Debug.LogWarning($"[Forest BG] Found asset but no sprite for '{name}'. Set Texture Type to Sprite.");
            }
            else
            {
                Debug.LogWarning($"[Forest BG] Could not find asset: {name} in {folder}");
            }
        }
        return sprites;
    }

    static void SetSpriteArray(SerializedObject so, string propertyName, List<Sprite> sprites)
    {
        var prop = so.FindProperty(propertyName);
        prop.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
