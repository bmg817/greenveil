using UnityEngine;

public class CombatSceneSetup : MonoBehaviour
{
    [SerializeField] private Transform[] playerCharacters;
    [SerializeField] private Transform[] enemyCharacters;
    [SerializeField] private Vector3 playerStartPosition = new Vector3(-4f, 0f, 0f);
    [SerializeField] private Vector3 enemyStartPosition = new Vector3(4f, 0f, 0f);
    [SerializeField] private float verticalSpacing = 2f;

    void Start()
    {
        PositionCharacters();
    }

    void PositionCharacters()
    {
        for (int i = 0; i < playerCharacters.Length; i++)
        {
            if (playerCharacters[i] != null)
            {
                float yOffset = (playerCharacters.Length - 1) * verticalSpacing / 2f;
                playerCharacters[i].position = playerStartPosition + new Vector3(0, yOffset - (i * verticalSpacing), 0);
            }
        }

        for (int i = 0; i < enemyCharacters.Length; i++)
        {
            if (enemyCharacters[i] != null)
            {
                float yOffset = (enemyCharacters.Length - 1) * verticalSpacing / 2f;
                enemyCharacters[i].position = enemyStartPosition + new Vector3(0, yOffset - (i * verticalSpacing), 0);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(playerStartPosition, new Vector3(2f, 5f, 1f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(enemyStartPosition, new Vector3(2f, 5f, 1f));
    }
}
