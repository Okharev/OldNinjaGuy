using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Setup")]
        public InputActionReference moveAction;
        public InputActionReference dashAction;
        public InputActionReference attackAction; // NOUVEAU : L'action pour attaquer

        [Header("Movement Settings")]
        public float moveSpeed = 10f;
        [Tooltip("How fast the character rotates to face their movement. Lower is faster.")]
        public float turnSmoothTime = 0.05f;

        [Header("Dash Settings")]
        public float dashSpeed = 30f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 0.5f;
        [Tooltip("The shape of the dash. Starts at 1 (max speed) and drops to 0.")]
        public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [Tooltip("How long to remember a dash input if pressed during cooldown.")]
        public float inputBufferTime = 0.2f;

        [Header("Combat Settings")]
        [Tooltip("Durée de chaque attaque du combo (Coup 1, Coup 2, Coup 3)")]
        public float[] attackDurations = { 0.3f, 0.3f, 0.5f }; 
        [Tooltip("Temps d'attente maximum avant que le combo ne retombe à zéro")]
        public float comboResetTime = 0.8f;
        [Tooltip("Permet d'enregistrer la touche d'attaque juste avant la fin de l'attaque précédente")]
        public float attackInputBufferTime = 0.2f;

        // Component references
        private CharacterController controller;
        
        // State tracking (Movement)
        private Vector3 movementInput;
        private Vector3 movementDirection;
        private Vector3 lockedDashDirection;
        private float turnSmoothVelocity;
        
        // State tracking (Dash)
        private bool isDashing = false;
        private float dashTimer;
        private float nextAvailableDashTime;
        private float dashBufferCounter;

        // State tracking (Combat) - NOUVEAU
        private bool isAttacking = false;
        private int currentComboStep = 0; // Va de 0 à 2 (pour 3 coups)
        private float attackTimer;
        private float lastAttackEndTime;
        private float attackBufferCounter;

        void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (dashAction != null) dashAction.action.Enable();
            if (attackAction != null) attackAction.action.Enable(); // NOUVEAU
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (dashAction != null) dashAction.action.Disable();
            if (attackAction != null) attackAction.action.Disable(); // NOUVEAU
        }

        void Update()
        {
            GatherInput();
            HandleDashInput();
            HandleAttackInput(); // NOUVEAU
            ApplyMovement();
        }

        void GatherInput()
        {
            Vector2 inputVector = moveAction.action.ReadValue<Vector2>();
            movementInput = new Vector3(inputVector.x, 0, inputVector.y).normalized;
            movementDirection = ToIsometric(movementInput);
        }

        void HandleDashInput()
        {
            if (dashAction.action.WasPressedThisFrame())
            {
                dashBufferCounter = inputBufferTime;
            }
            else
            {
                dashBufferCounter -= Time.deltaTime;
            }

            if (dashBufferCounter > 0f && Time.time >= nextAvailableDashTime && movementInput.magnitude > 0)
            {
                // Dashing annule l'attaque en cours (Très important pour un jeu style Hades)
                isAttacking = false; 
                
                isDashing = true;
                dashTimer = dashDuration;
                nextAvailableDashTime = Time.time + dashCooldown;
                dashBufferCounter = 0f;
                lockedDashDirection = movementDirection; 
            }
        }

        // NOUVEAU : Gestion du système de combo
        void HandleAttackInput()
        {
            // 1. Réinitialiser le combo si on a trop attendu depuis la dernière attaque
            if (!isAttacking && Time.time - lastAttackEndTime > comboResetTime)
            {
                currentComboStep = 0;
            }

            // 2. Input Buffering pour l'attaque (pour un gameplay fluide)
            if (attackAction.action.WasPressedThisFrame())
            {
                attackBufferCounter = attackInputBufferTime;
            }
            else
            {
                attackBufferCounter -= Time.deltaTime;
            }

            // 3. Déclencher l'attaque si on a une entrée en attente et qu'on n'attaque pas déjà (ou qu'on ne dash pas)
            if (attackBufferCounter > 0f && !isAttacking && !isDashing)
            {
                StartAttack();
            }

            // 4. Gérer le minuteur de l'attaque en cours
            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    isAttacking = false;
                    lastAttackEndTime = Time.time; // On mémorise quand l'attaque a fini
                    
                    // Passer à l'étape suivante du combo, ou revenir à 0 si c'était le 3ème coup
                    currentComboStep++;
                    if (currentComboStep >= attackDurations.Length)
                    {
                        currentComboStep = 0;
                    }
                }
            }
        }

        // NOUVEAU : Fonction pour démarrer une attaque
        void StartAttack()
        {
            isAttacking = true;
            attackBufferCounter = 0f; // On consomme l'entrée du joueur
            
            // La durée de l'attaque dépend de l'étape du combo
            attackTimer = attackDurations[currentComboStep];

            // C'est ici que tu déclencheras tes animations et tes boîtes de collision (hitboxes) !
            Debug.Log($"Attaque {currentComboStep + 1} ! Durée : {attackTimer}s");
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

                if (dashTimer <= 0)
                {
                    isDashing = false;
                }
            }
            else if (!isAttacking) // NOUVEAU : On ne bouge que si on n'attaque pas
            {
                currentVelocity = movementDirection * moveSpeed;
            }

            currentVelocity.y += Physics.gravity.y; 
            controller.Move(currentVelocity * Time.deltaTime);

            // Rotation
            Vector3 directionToFace = movementDirection;
            if (isDashing) directionToFace = lockedDashDirection;
            
            // NOUVEAU : Si on attaque sans bouger le joystick, on garde la rotation actuelle
            if (isAttacking && directionToFace == Vector3.zero) 
            {
                return; // Ne pas modifier la rotation
            }

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
    }
}