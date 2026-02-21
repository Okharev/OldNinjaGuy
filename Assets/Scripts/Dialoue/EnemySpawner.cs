using UnityEngine;

namespace Dialoue
{
    public class EnemySpawner : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public int countToSpawn = 5;
        public float spawnRadius = 2f;

        // Appelée via l'UnityEvent du DialogueEventReceiver
        public void TriggerSpawn()
        {
            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle * spawnRadius);
                Instantiate(enemyPrefab, pos, Quaternion.identity);
            }
        }
    }
}