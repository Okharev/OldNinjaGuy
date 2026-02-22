using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

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
        private HealthComponent playerHealth;

        [Header("Paramètres de combat")]
        public float attackRange = 2f; 
        public float attackCooldown = 1.5f;
        public int attackDamage = 15;         
        public float attackWindupTime = 1.5f; 

        [Header("Butin (Loot)")]
        public GameObject sakePrefab; 
        [Range(0f, 100f)] 
        public float sakeDropChance = 35f; 

        [Header("Audio")]
        public AudioClip hitSound;
        public AudioClip deathSound;
        [Range(0f, 1f)]
        public float soundVolume = 1f;
        // NOUVEAU : Paramètres pour la variation du pitch
        [Tooltip("Pitch minimum (plus grave)")]
        public float minPitch = 0.85f;
        [Tooltip("Pitch maximum (plus aigu)")]
        public float maxPitch = 1.15f;

        [Header("Animator")]
        [SerializeField] public Animator animator;

        [Header("VFX")]
        public GameObject deathVFXPrefab;
        public float deathVFXDuration = 8f;
        private GameObject VfxInstance;

        private NavMeshAgent agent;
        private float lastAttackTime;
        private float attackRangeSqr; 
        private float timeInRange;            

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

            if (enemyRenderers.Length > 0)
            {
                originalColors = new Color[enemyRenderers.Length];
                for (int i = 0; i < enemyRenderers.Length; i++)
                {
                    originalColors[i] = enemyRenderers[i].material.color;
                }
            }

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
                timeInRange = 0f; 
            }

            animator.SetBool("IsMoving", agent.speed > 0); 
        }

        public void TakeDamage(int damage, Vector3 sourcePosition, float knockbackForce)
        {
            currentHealth -= damage;
            
            float healthPercent = Mathf.Clamp01((float)currentHealth / maxHealth);
            OnHealthChanged?.Invoke(healthPercent);

            // NOUVEAU : On utilise notre fonction personnalisée
            PlaySoundWithPitch(hitSound);

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
            // NOUVEAU : On utilise notre fonction personnalisée
            PlaySoundWithPitch(deathSound);

            VfxInstance = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity, null);

            if (sakePrefab != null)
            {
                float randomValue = UnityEngine.Random.Range(0f, 100f);
                
                if (randomValue <= sakeDropChance)
                {
                    Instantiate(sakePrefab, transform.position, Quaternion.identity);
                }
            }

            Destroy(gameObject);
        }

        // NOUVEAU : Fonction personnalisée pour jouer un son avec un pitch aléatoire
        private void PlaySoundWithPitch(AudioClip clip)
        {
            if (clip == null) return;

            // 1. On crée un objet temporaire invisible
            GameObject audioObj = new GameObject("TempAudio");
            audioObj.transform.position = transform.position;

            // 2. On lui ajoute un composant AudioSource
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1f; // Rend le son 3D (il sera plus fort si on est près)

            // 3. On choisit un pitch aléatoire entre nos deux valeurs
            audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);

            // 4. On joue le son
            audioSource.Play();

            // 5. On programme la destruction de l'objet à la fin du son
            Destroy(audioObj, clip.length);
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
                timeInRange += Time.deltaTime;

                if (timeInRange >= attackWindupTime)
                {
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(attackDamage, transform.position, 5f);
                    }

                    lastAttackTime = Time.time;
                    timeInRange = 0f; 
                }
            }
            else
            {
                timeInRange = 0f; 
            }
        }
    }
}