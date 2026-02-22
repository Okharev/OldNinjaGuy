using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using UnityEditor.Animations;

namespace Movement
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        // --- UI REGISTRATION EVENTS ---
        public static event Action<EnemyController> OnEnemySpawned;
        public static event Action<EnemyController> OnEnemyDespawned;
        public event Action<float> OnHealthChanged;

        [Header("Stats & Santé")]
        public int maxHealth = 50;
        private int currentHealth;

        [Header("Feedback Visuel")]
        public Renderer[] enemyRenderers; 
        public Color flashColor = Color.red;
        public float flashDuration = 0.1f;

        [Header("Ciblage")]
        public string playerTag = "Player";
        private Transform playerTransform;
        private HealthComponent playerHealth; // [ADDED] Reference to the player's health

        [Header("Paramètres de combat")]
        public float attackRange = 2f; 
        public float attackCooldown = 1.5f;
        public int attackDamage = 15;         // [ADDED] How much damage the enemy deals
        public float attackWindupTime = 0.5f; // [ADDED] Must stay in range for 0.5s to hit

        [Header("Animator")]
        [SerializeField] public Animator animator;

        private NavMeshAgent agent;
        private float lastAttackTime;
        private float attackRangeSqr; 
        private float timeInRange;            // [ADDED] Tracks how long the player is in range

        private Vector3 knockbackVelocity;
        private float knockbackDuration;
        private Color[] originalColors;

        void OnEnable()
        {
            currentHealth = maxHealth;
            OnEnemySpawned?.Invoke(this); 
        }

        void OnDisable()
        {
            OnEnemyDespawned?.Invoke(this);
        }

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            attackRangeSqr = attackRange * attackRange;

            // Initialisation du feedback visuel
            if (enemyRenderers.Length > 0)
            {
                originalColors = new Color[enemyRenderers.Length];
                for (int i = 0; i < enemyRenderers.Length; i++)
                {
                    originalColors[i] = enemyRenderers[i].material.color;
                }
            }

            // Find the player AND their HealthComponent
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null) 
            {
                playerTransform = playerObj.transform;
                playerHealth = playerObj.GetComponent<HealthComponent>();
            }
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

            if (distanceToPlayerSqr <= attackRangeSqr) 
            {
                AttackPlayer();
            }
            else 
            {
                ChasePlayer();
                timeInRange = 0f; // [ADDED] Reset windup timer if player escapes range
            }

            animator.SetBool("IsMoving", agent.speed > 0); // Update movement animation
        }

        public void TakeDamage(int damage, Vector3 sourcePosition, float knockbackForce)
        {
            currentHealth -= damage;
            
            float healthPercent = Mathf.Clamp01((float)currentHealth / maxHealth);
            OnHealthChanged?.Invoke(healthPercent);

            StopAllCoroutines(); 
            StartCoroutine(FlashRoutine());

            Vector3 knockbackDir = (transform.position - sourcePosition).normalized;
            knockbackDir.y = 0;
            knockbackVelocity = knockbackDir * knockbackForce;
            knockbackDuration = 0.25f;

            if (currentHealth <= 0) Die();
        }

        private IEnumerator FlashRoutine()
        {
            for (int i = 0; i < enemyRenderers.Length; i++)
                if (enemyRenderers[i]) enemyRenderers[i].material.color = flashColor;

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < enemyRenderers.Length; i++)
                if (enemyRenderers[i] != null) enemyRenderers[i].material.color = originalColors[i];
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
            // Stop moving to attack
            if (!agent.isStopped) agent.isStopped = true;
            
            // Look at the player
            Vector3 lookPos = playerTransform.position - transform.position;
            lookPos.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 5f);

            // 1. Check if the cooldown has passed
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                // 2. Increase the windup timer
                timeInRange += Time.deltaTime;

                // 3. If we've been in range for 0.5s, STRIKE!
                if (timeInRange >= attackWindupTime)
                {
                    if (playerHealth != null)
                    {
                        // Deal damage, pass position for knockback, and apply a knockback force of 5
                        playerHealth.TakeDamage(attackDamage, transform.position, 5f);
                    }

                    // Reset timers
                    lastAttackTime = Time.time;
                    timeInRange = 0f; // Reset windup so they have to charge the next attack
                }
            }
            else
            {
                // Reset windup timer if we are currently on cooldown
                timeInRange = 0f; 
            }
        }
    }
}