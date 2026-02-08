using UnityEngine;
using System.Collections.Generic;
using Greenveil.Combat;

public class CombatSceneSetup : MonoBehaviour
{
    [SerializeField] private float staggerAmount = 0.3f;

    void Start()
    {
        EnsureForestBackground();
        PositionCharacters();
    }

    void EnsureForestBackground()
    {
        if (FindAnyObjectByType<ForestBackground>() == null)
        {
            var bgObj = new GameObject("ForestBackground");
            bgObj.AddComponent<ForestBackground>();
        }
    }

    void PositionCharacters()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[CombatSceneSetup] Camera.main is null!");
            return;
        }

        var starter = FindAnyObjectByType<CombatStarter>();
        if (starter == null)
        {
            Debug.LogError("[CombatSceneSetup] No CombatStarter found!");
            return;
        }

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        float usableWidth = camWidth * 2f * 0.4f;

        float enemyY = camHeight * 0.4f;
        float playerY = -camHeight * 0.3f;

        Debug.Log($"[CombatSceneSetup] Camera: orthoSize={camHeight}, aspect={cam.aspect}, width={camWidth}");
        Debug.Log($"[CombatSceneSetup] Players={starter.Players.Count}, Enemies={starter.Enemies.Count}");
        Debug.Log($"[CombatSceneSetup] enemyY={enemyY}, playerY={playerY}, usableWidth={usableWidth}");

        PositionGroup(starter.Enemies, enemyY, usableWidth, true);
        PositionGroup(starter.Players, playerY, usableWidth, false);
    }

    void PositionGroup(IReadOnlyList<CombatCharacter> characters, float yPos, float totalWidth, bool faceDown)
    {
        if (characters.Count == 0) return;

        if (characters.Count == 1)
        {
            Transform t = characters[0].transform;
            t.position = new Vector3(0f, yPos, 0f);
            if (faceDown && !HasSprite(characters[0]))
                t.localScale = new Vector3(t.localScale.x, -Mathf.Abs(t.localScale.y), t.localScale.z);
            Debug.Log($"[CombatSceneSetup] Positioned {characters[0].CharacterName} at {t.position}");
            return;
        }

        float spacing = totalWidth / (characters.Count - 1);
        float startX = -totalWidth / 2f;

        for (int i = 0; i < characters.Count; i++)
        {
            Transform t = characters[i].transform;
            float yStagger = (i % 2 == 1) ? staggerAmount : 0f;
            t.position = new Vector3(startX + i * spacing, yPos + yStagger, 0f);
            if (faceDown && !HasSprite(characters[i]))
                t.localScale = new Vector3(t.localScale.x, -Mathf.Abs(t.localScale.y), t.localScale.z);
            Debug.Log($"[CombatSceneSetup] Positioned {characters[i].CharacterName} at {t.position}");
        }
    }

    bool HasSprite(CombatCharacter character)
    {
        var visual = character.GetComponent<CharacterVisual>();
        return visual != null && visual.HasAssignedSprite;
    }

    void OnDrawGizmos()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(0, camHeight * 0.5f, 0), new Vector3(camWidth * 2f, camHeight, 0));

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(new Vector3(0, -camHeight * 0.5f, 0), new Vector3(camWidth * 2f, camHeight, 0));
    }
}
