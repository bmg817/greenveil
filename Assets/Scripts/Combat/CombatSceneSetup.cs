using UnityEngine;

/// <summary>
/// Automatically positions characters in the scene for combat
/// Attach this to an empty GameObject in your scene
/// </summary>
public class CombatSceneSetup : MonoBehaviour
{
    [Header("Character Positioning")]
    [SerializeField] private Transform[] playerCharacters;
    [SerializeField] private Transform[] enemyCharacters;
    
    [Header("Position Settings")]
    [SerializeField] private Vector3 playerStartPosition = new Vector3(-4f, 0f, 0f);
    [SerializeField] private Vector3 enemyStartPosition = new Vector3(4f, 0f, 0f);
    [SerializeField] private float verticalSpacing = 2f;

    void Start()
    {
        PositionCharacters();
    }

    void PositionCharacters()
    {
        // Position player characters on the left
        for (int i = 0; i < playerCharacters.Length; i++)
        {
            if (playerCharacters[i] != null)
            {
                float yOffset = (playerCharacters.Length - 1) * verticalSpacing / 2f;
                playerCharacters[i].position = playerStartPosition + new Vector3(0, yOffset - (i * verticalSpacing), 0);
            }
        }

        // Position enemy characters on the right
        for (int i = 0; i < enemyCharacters.Length; i++)
        {
            if (enemyCharacters[i] != null)
            {
                float yOffset = (enemyCharacters.Length - 1) * verticalSpacing / 2f;
                enemyCharacters[i].position = enemyStartPosition + new Vector3(0, yOffset - (i * verticalSpacing), 0);
            }
        }
    }

    // Draw gizmos in scene view to visualize positions
    void OnDrawGizmos()
    {
        // Draw player side
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(playerStartPosition, new Vector3(2f, 5f, 1f));

        // Draw enemy side
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(enemyStartPosition, new Vector3(2f, 5f, 1f));
    }
}
