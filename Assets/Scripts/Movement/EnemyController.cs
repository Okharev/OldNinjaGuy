using UnityEngine;
using UnityEngine.AI;
using System.Collections; // REQUIS pour les Coroutines

namespace Movement
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Stats & Santé")]
        public int maxHealth = 50;
        private int currentHealth;

        [Header("Feedback Visuel")]
        [Tooltip("Glisse ici le(s) Renderer(s) de l'ennemi (le corps, l'armure, etc.)")]
        public Renderer[] enemyRenderers; 
        [Tooltip("La couleur du flash (souvent rouge ou blanc)")]
        public Color flashColor = Color.red;
        [Tooltip("Durée du flash en secondes")]
        public float flashDuration = 0.1f;

        [Header("Ciblage")]
        public string playerTag = "Player";
        private Transform playerTransform;

        [Header("Paramètres de combat")]
        public float attackRange = 2f; 
        public float attackCooldown = 1.5f; 

        private NavMeshAgent agent;
        private float lastAttackTime;
        private float attackRangeSqr; 

        private Vector3 knockbackVelocity;
        private float knockbackDuration;
        
        // Pour mémoriser les couleurs de base de chaque partie du corps
        private Color[] originalColors;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            attackRangeSqr = attackRange * attackRange;
            currentHealth = maxHealth;

            // Initialisation du feedback visuel
            if (enemyRenderers.Length > 0)
            {
                originalColors = new Color[enemyRenderers.Length];
                for (int i = 0; i < enemyRenderers.Length; i++)
                {
                    // On mémorise la couleur de base du matériau
                    originalColors[i] = enemyRenderers[i].material.color;
                }
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        void Update()
        {
            if (knockbackDuration > 0)
            {
                knockbackDuration -= Time.deltaTime;
                agent.Move(knockbackVelocity * Time.deltaTime);
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 5f);
                return; 
            }

            if (playerTransform == null) return;

            Vector3 directionToPlayer = playerTransform.position - transform.position;
            float distanceToPlayerSqr = directionToPlayer.sqrMagnitude;

            if (distanceToPlayerSqr <= attackRangeSqr) AttackPlayer();
            else ChasePlayer();
        }

        public void TakeDamage(int damage, Vector3 sourcePosition, float knockbackForce)
        {
            currentHealth -= damage;
            
            // DÉCLENCHEMENT DU FLASH
            StopAllCoroutines(); // On arrête un éventuel flash en cours
            StartCoroutine(FlashRoutine());

            Vector3 knockbackDir = (transform.position - sourcePosition).normalized;
            knockbackDir.y = 0;
            knockbackVelocity = knockbackDir * knockbackForce;
            knockbackDuration = 0.25f;

            if (currentHealth <= 0) Die();
        }

        // LA COROUTINE DU FLASH
        private IEnumerator FlashRoutine()
        {
            // Étape 1 : Passer tous les renderers en rouge
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                if (enemyRenderers[i])
                    enemyRenderers[i].material.color = flashColor;
            }

            // Étape 2 : Attendre un tout petit peu
            yield return new WaitForSeconds(flashDuration);

            // Étape 3 : Remettre les couleurs d'origine
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                if (enemyRenderers[i] != null)
                    enemyRenderers[i].material.color = originalColors[i];
            }
        }

        private void Die()
        {
            Destroy(gameObject);
        }

        private void ChasePlayer()
        {
            if (agent.isStopped) agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }

        private void AttackPlayer()
        {
            if (!agent.isStopped) agent.isStopped = true;
            Vector3 lookPos = playerTransform.position - transform.position;
            lookPos.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 5f);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
            }
        }
    }
}