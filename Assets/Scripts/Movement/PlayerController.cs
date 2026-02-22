using System.Collections.Generic;
using Dialoue;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Glisse ici le composant Animator de ton personnage")]
        public Animator animator;
        
        [Header("Input Setup")]
        public InputActionReference moveAction;
        public InputActionReference dashAction;
        public InputActionReference attackAction;

        [Header("Movement Settings")]
        public float moveSpeed = 10f;
        public float turnSmoothTime = 0.05f;

        [Header("Dash Settings")]
        public float dashSpeed = 30f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 0.5f;
        public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public float inputBufferTime = 0.2f;

        [Header("Combat Settings (Combo)")]
        public float[] attackDurations = { 0.3f, 0.3f, 0.5f }; 
        public float comboResetTime = 0.8f;
        public float attackInputBufferTime = 0.2f;
        public float[] attackLungeSpeeds = { 3f, 3f, 2f }; // Note: réduit pour le dernier coup pour l'effet d'impact

        [Header("Combat Settings (Hitbox & Damage)")]
        public LayerMask enemyLayer;
        public int[] attackDamages = { 10, 10, 30 };
        public float[] attackKnockbacks = { 5f, 5f, 20f };

        [Tooltip("Taille de la boîte pour les coups 1 et 2")]
        public Vector3 normalHitboxSize = new Vector3(2f, 1.5f, 2f);
        [Tooltip("Distance de la boîte devant le joueur")]
        public float normalHitboxDistance = 1.5f;

        [Header("Last Attack AOE Settings")]
        [Tooltip("Rayon du cercle d'explosion pour le dernier coup")]
        public float lastAttackAoeRadius = 4f; // NOUVEAU

        [SerializeField] private GameObject HitVFXPrefab; // Assigne ici ton prefab de VFX d'impact
        [SerializeField] private GameObject Trail; // Assigne ici ton prefab de traînée pour le dash

        private CharacterController controller;
        [SerializeField] private bool isMoving = false;
        private Vector3 movementInput;
        private Vector3 movementDirection;
        private Vector3 lockedDashDirection;
        private float turnSmoothVelocity;
        
        private bool isDashing = false;
        private float dashTimer;
        private float nextAvailableDashTime;
        private float dashBufferCounter;

        private bool isAttacking = false;
        private int currentComboStep = 0;
        private float attackTimer;
        private float lastAttackEndTime;
        private float attackBufferCounter;

        private HashSet<Collider> enemiesHitThisAttack = new HashSet<Collider>();

        void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            // On s'abonne à l'événement
            DialogueManager.OnDialogueStateChanged += HandleDialogueStateChanged;

            if (moveAction != null) moveAction.action.Enable();
            if (dashAction != null) dashAction.action.Enable();
            if (attackAction != null) attackAction.action.Enable();
        }

        private void OnDisable()
        {
            // IMPORTANT : Toujours se désabonner pour éviter les fuites de mémoire
            DialogueManager.OnDialogueStateChanged -= HandleDialogueStateChanged;

            if (moveAction != null) moveAction.action.Enable(); // (etc. comme avant)
        }
        
        private void HandleDialogueStateChanged(bool isDialogueActive)
        {
            if (isDialogueActive)
            {
                // On coupe les entrées
                moveAction.action.Disable();
                dashAction.action.Disable();
                attackAction.action.Disable();

                // On réinitialise les états pour éviter que le joueur reste bloqué 
                // en pleine course ou attaque
                movementInput = Vector3.zero;
                movementDirection = Vector3.zero;
                isAttacking = false;
                isDashing = false;
            }
            else
            {
                // On réactive tout quand le dialogue est fini
                moveAction.action.Enable();
                dashAction.action.Enable();
                attackAction.action.Enable();
            }
        }

        void Update()
        {
            isMoving = movementInput.magnitude > 0.1f;
            GatherInput();
            HandleDashInput();
            HandleAttackInput();
            ApplyMovement();
            animator.SetBool("IsMoving", isMoving); // Mise à jour du paramètre d'animation de déplacement
            if (currentComboStep > 0 && currentComboStep <= 1)
            {
                animator.SetLayerWeight(1, 1); // Active la couche d'attaque
            }
            else
            {
                animator.SetLayerWeight(1, 0); // Désactive la couche d'attaque
            }

            if (currentComboStep == 2)
            {
                animator.SetLayerWeight(2, 1); // Active la couche du dernier coup
            }
            else
            {
                animator.SetLayerWeight(2, 0); // Désactive la couche du dernier coup
            }

            if (isDashing)
            {
                GetComponentInChildren<TrailRenderer>().enabled = true;
            }
            else
            {
                GetComponentInChildren<TrailRenderer>().enabled = false;
            }
        }

        void GatherInput()
        {
            Vector2 inputVector = moveAction.action.ReadValue<Vector2>();
            movementInput = new Vector3(inputVector.x, 0, inputVector.y).normalized;
            movementDirection = ToIsometric(movementInput);
        }

        void HandleDashInput()
        {
            if (dashAction.action.WasPressedThisFrame()) dashBufferCounter = inputBufferTime;
            else dashBufferCounter -= Time.deltaTime;

            if (dashBufferCounter > 0f && Time.time >= nextAvailableDashTime && movementInput.magnitude > 0)
            {
                isAttacking = false; 
                isDashing = true;
                dashTimer = dashDuration;
                nextAvailableDashTime = Time.time + dashCooldown;
                dashBufferCounter = 0f;
                lockedDashDirection = movementDirection; 
            }
        }

        void HandleAttackInput()
        {
            if (!isAttacking && Time.time - lastAttackEndTime > comboResetTime)
            {
                currentComboStep = 0;
            }

            if (attackAction.action.WasPressedThisFrame()) attackBufferCounter = attackInputBufferTime;
            else attackBufferCounter -= Time.deltaTime;

            if (attackBufferCounter > 0f && !isAttacking && !isDashing)
            {
                StartAttack();
            }

            if (isAttacking)
            {
                PerformHitboxCheck();

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    isAttacking = false;
                    lastAttackEndTime = Time.time; 
                    currentComboStep++;
                    if (currentComboStep >= attackDurations.Length) currentComboStep = 0;
                }
            }
        }

        void StartAttack()
        {
            isAttacking = true;
            attackBufferCounter = 0f; 
            attackTimer = attackDurations[currentComboStep];
            enemiesHitThisAttack.Clear();

            // NOUVEAU : Déclencher l'animation correspondante
            if (animator != null)
            {
                // On crée un nom de Trigger dynamique basé sur l'étape du combo (0, 1 ou 2)
                // Cela enverra le signal "Attack0", "Attack1" ou "Attack2" à l'Animator
                animator.SetTrigger("Attack" + currentComboStep); 
            }
        }

        // NOUVEAU : Logique hybride Box/AOE
        void PerformHitboxCheck()
        {
            Collider[] hitColliders;
            bool isLastHit = (currentComboStep == attackDurations.Length - 1);

            if (isLastHit)
            {
                // AOE Circulaire autour du joueur (360 degrés)
                hitColliders = Physics.OverlapSphere(transform.position, lastAttackAoeRadius, enemyLayer);
            }
            else
            {
                // Hitbox classique en boîte devant le joueur
                Vector3 hitboxCenter = transform.position + transform.forward * normalHitboxDistance;
                hitColliders = Physics.OverlapBox(hitboxCenter, normalHitboxSize / 2f, transform.rotation, enemyLayer);
            }

            foreach (Collider hit in hitColliders)
            {
                
                if (!enemiesHitThisAttack.Contains(hit))
                {
                    enemiesHitThisAttack.Add(hit); 
                    EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                    // Spawn HitVFX at collision point
                    if (HitVFXPrefab != null)
                    {
                        Vector3 hitPoint = hit.ClosestPoint(transform.position);
                        Instantiate(HitVFXPrefab, hitPoint, Quaternion.identity);
                    }
                    if (enemy != null)
                    {
                        // On envoie transform.position : l'ennemi calculera le recul radialement
                        enemy.TakeDamage(attackDamages[currentComboStep], transform.position, attackKnockbacks[currentComboStep]);
                    }
                }
            }
        }

        void ApplyMovement()
        {
            Vector3 currentVelocity = Vector3.zero;

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                float dashProgress = 1f - (dashTimer / dashDuration);
                float currentDashSpeed = dashSpeed * dashCurve.Evaluate(dashProgress);
                currentVelocity = lockedDashDirection * currentDashSpeed;
                if (dashTimer <= 0) isDashing = false;
            }
            else if (isAttacking)
            {
                currentVelocity = transform.forward * attackLungeSpeeds[currentComboStep];
            }
            else
            {
                currentVelocity = movementDirection * moveSpeed;
            }

            currentVelocity.y += Physics.gravity.y; 
            controller.Move(currentVelocity * Time.deltaTime);

            Vector3 directionToFace = movementDirection;
            if (isDashing) directionToFace = lockedDashDirection;
            if (isAttacking && directionToFace == Vector3.zero) return; 

            if (directionToFace != Vector3.zero)
            {
                float targetAngle = Mathf.Atan2(directionToFace.x, directionToFace.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        Vector3 ToIsometric(Vector3 input)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, 45, 0);
            return cameraRotation * input;
        }

        // NOUVEAU : Visualisation des deux types de Hitbox
        private void OnDrawGizmos()
        {
            if (!isAttacking) return;

            Gizmos.color = new Color(1, 0, 0, 0.4f);
            bool isLastHit = (currentComboStep == attackDurations.Length - 1);

            if (isLastHit)
            {
                Gizmos.DrawSphere(transform.position, lastAttackAoeRadius);
            }
            else
            {
                Vector3 hitboxCenter = transform.position + transform.forward * normalHitboxDistance;
                Gizmos.matrix = Matrix4x4.TRS(hitboxCenter, transform.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, normalHitboxSize);
            }
        }
    }
}