using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Master.Scripts.NPC
{
    /// <summary>
    /// Autonomous locomotion brain driving a NavMeshAgent in an anchor-leash model.
    /// Manages clean state flow: Idle -> Wander -> Interacting.
    /// Automatically pipes agent velocity into an Animator float parameter for blending,
    /// and provides public interrupt hooks (StartFocus / EndFocus) for seamless conversation engagement.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Kontexto/NPC/NPC Roaming Agent")]
    public class NPCRoamingAgent : MonoBehaviour
    {
        public enum RoamingState
        {
            Idle,
            Wander,
            Interacting
        }

        [Header("State")]
        [Tooltip("Current state of the roaming agent.")]
        [SerializeField] private RoamingState currentState = RoamingState.Idle;

        [Header("Anchor & Leash Settings")]
        [Tooltip("Center point of the NPC's roaming territory. Defaults to the object's initial position on Start.")]
        [SerializeField] private Vector3 anchorPosition;
        [Tooltip("If true, automatically captures transform.position on Start as the anchor territory center.")]
        [SerializeField] private bool useCurrentPositionAsAnchor = true;
        [Tooltip("Maximum radius (in meters) around the anchor point that the NPC is permitted to wander.")]
        [SerializeField] private float leashRadius = 8f;
        [Tooltip("Maximum distance to sample on the NavMesh when finding valid reachable points.")]
        [SerializeField] private float sampleMaxDistance = 4f;
        [Tooltip("Maximum attempts to sample a valid reachable point before falling back or retrying.")]
        [SerializeField] private int maxSampleAttempts = 10;

        [Header("Idle Settings")]
        [Tooltip("Minimum wait time in seconds during the Idle state.")]
        [SerializeField] private float minIdleDuration = 2f;
        [Tooltip("Maximum wait time in seconds during the Idle state.")]
        [SerializeField] private float maxIdleDuration = 6f;

        [Header("Wander Settings")]
        [Tooltip("Distance threshold to destination at which the NPC considers it has arrived.")]
        [SerializeField] private float arrivalDistance = 0.5f;
        [Tooltip("Safety timeout in seconds. If the NPC cannot reach its destination within this window, it returns to Idle.")]
        [SerializeField] private float wanderTimeout = 12f;
        [Tooltip("Movement speed applied to the NavMeshAgent during wander.")]
        [SerializeField] private float walkSpeed = 1.5f;
        [Tooltip("Angular rotation speed in degrees per second.")]
        [SerializeField] private float angularSpeed = 120f;
        [Tooltip("Acceleration applied to the NavMeshAgent.")]
        [SerializeField] private float acceleration = 8f;

        [Header("Animation")]
        [Tooltip("Animator to drive. Auto-detected on this GameObject or its children if left unassigned.")]
        [SerializeField] private Animator animator;
        [Tooltip("Float parameter name on the Animator Controller used for blend tree locomotion (e.g., Speed).")]
        [SerializeField] private string speedParameterName = "Speed";
        [Tooltip("Damping time in seconds for smoothing the speed float parameter in the Animator.")]
        [SerializeField] private float speedDampTime = 0.15f;

        [Header("Focus / Interaction Settings")]
        [Tooltip("Duration in seconds for smoothly rotating towards the player when focused.")]
        [SerializeField] private float focusTurnDuration = 0.5f;
        [Tooltip("Optional explicit target transform to look at. If empty, automatically resolves player via PlayerBody/Player tag or PlayerController.")]
        [SerializeField] private Transform focusTarget;

        // Public properties
        public RoamingState CurrentState => currentState;
        public Vector3 AnchorPosition => anchorPosition;
        public float LeashRadius
        {
            get => leashRadius;
            set => leashRadius = Mathf.Max(0.5f, value);
        }
        public NavMeshAgent Agent => agent;

        // State change notification event
        public event Action<RoamingState> OnStateChanged;

        // Internal references & state
        private NavMeshAgent agent;
        private int speedHash;
        private float idleTimer;
        private float wanderTimer;
        private bool anchorInitialized;
        private Coroutine rotateCoroutine;

        private void Awake()
        {
            // Auto-detect NavMeshAgent
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
            if (agent == null)
            {
                agent = GetComponentInChildren<NavMeshAgent>();
            }

            // Auto-detect Animator
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (!string.IsNullOrEmpty(speedParameterName))
            {
                speedHash = Animator.StringToHash(speedParameterName);
            }
        }

        private void Start()
        {
            // Establish anchor position
            if (useCurrentPositionAsAnchor || !anchorInitialized)
            {
                anchorPosition = transform.position;
                anchorInitialized = true;
            }

            // Configure NavMeshAgent parameters
            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.angularSpeed = angularSpeed;
                agent.acceleration = acceleration;
                agent.stoppingDistance = arrivalDistance;

                // Validate NavMesh placement: warp to nearest point if dropped slightly off-mesh
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleMaxDistance, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                    else
                    {
                        Debug.LogWarning($"NPCRoamingAgent on '{gameObject.name}': Could not find NavMesh within {sampleMaxDistance}m of position {transform.position}.");
                    }
                }
            }

            // Stagger start slightly to avoid synchronized movement among multiple NPCs
            float initialIdleWait = UnityEngine.Random.Range(minIdleDuration, maxIdleDuration);
            TransitionToIdle(initialIdleWait);
        }

        private void Update()
        {
            if (agent == null) return;

            switch (currentState)
            {
                case RoamingState.Idle:
                    UpdateIdle();
                    break;
                case RoamingState.Wander:
                    UpdateWander();
                    break;
                case RoamingState.Interacting:
                    // Movement is frozen; rotation coroutine handles facing the player
                    break;
            }

            UpdateAnimation();
        }

        #region State Machine

        private void UpdateIdle()
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                if (TryFindWanderDestination(out Vector3 destination))
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(destination);
                        TransitionToWander();
                    }
                    else
                    {
                        idleTimer = 1f; // Retry after short pause if off mesh
                    }
                }
                else
                {
                    // Failed to find valid destination; pause briefly before retrying
                    idleTimer = 1.5f;
                }
            }
        }

        private void UpdateWander()
        {
            wanderTimer += Time.deltaTime;

            if (!agent.isOnNavMesh)
            {
                TransitionToIdle();
                return;
            }

            // 1. Check arrival at destination
            if (!agent.pathPending)
            {
                float stopDist = Mathf.Max(agent.stoppingDistance, arrivalDistance);
                if (agent.remainingDistance <= stopDist || !agent.hasPath)
                {
                    TransitionToIdle();
                    return;
                }
            }

            // 2. Safety timeout (prevents remaining stuck against dynamic obstacles)
            if (wanderTimer >= wanderTimeout)
            {
                TransitionToIdle();
                return;
            }

            // 3. Path invalidated or blocked
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                TransitionToIdle();
            }
        }

        private void TransitionToIdle(float? durationOverride = null)
        {
            currentState = RoamingState.Idle;
            idleTimer = durationOverride ?? UnityEngine.Random.Range(minIdleDuration, maxIdleDuration);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }

            OnStateChanged?.Invoke(currentState);
        }

        private void TransitionToWander()
        {
            currentState = RoamingState.Wander;
            wanderTimer = 0f;
            OnStateChanged?.Invoke(currentState);
        }

        #endregion

        #region Animation Blending

        private void UpdateAnimation()
        {
            if (animator == null || speedHash == 0) return;

            float targetSpeed = 0f;

            if (currentState == RoamingState.Wander && agent != null && agent.isOnNavMesh)
            {
                targetSpeed = agent.velocity.magnitude;
            }
            else
            {
                // In Idle or Interacting, speed target is 0
                targetSpeed = 0f;
            }

            if (speedDampTime > 0.001f)
            {
                animator.SetFloat(speedHash, targetSpeed, speedDampTime, Time.deltaTime);
            }
            else
            {
                animator.SetFloat(speedHash, targetSpeed);
            }
        }

        #endregion

        #region Public Focus / Interruption Hooks

        /// <summary>
        /// Public hook to interrupt roaming and focus on the player.
        /// Freezes the agent, resets velocity to zero, and smoothly rotates transform toward the player.
        /// Can be wired directly to UnityEvents (e.g. DialogueEventExtension.onAnyStart).
        /// </summary>
        public void StartFocus()
        {
            StartFocus(null);
        }

        /// <summary>
        /// Public hook to interrupt roaming and focus on a specific target transform.
        /// </summary>
        /// <param name="target">Target transform to look at. If null, auto-resolves player.</param>
        public void StartFocus(Transform target)
        {
            currentState = RoamingState.Interacting;
            OnStateChanged?.Invoke(currentState);

            // 1. Freeze agent and zero out velocity
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }

            // 2. Immediately zero animator parameter to prevent blending lag
            if (animator != null && speedHash != 0)
            {
                animator.SetFloat(speedHash, 0f);
            }

            // 3. Resolve target
            Transform resolvedTarget = target != null ? target : ResolvePlayerTarget();

            // 4. Smoothly rotate toward player
            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
            }

            if (resolvedTarget != null && focusTurnDuration > 0f)
            {
                rotateCoroutine = StartCoroutine(SmoothLookAtTarget(resolvedTarget, focusTurnDuration));
            }
        }

        /// <summary>
        /// Public hook to release focus and resume autonomous roaming.
        /// Unfreezes the agent and transitions back to Idle before picking a new wander destination.
        /// Can be wired directly to UnityEvents (e.g. DialogueEventExtension.onAnyEnd).
        /// </summary>
        public void EndFocus()
        {
            if (currentState != RoamingState.Interacting) return;

            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
                rotateCoroutine = null;
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            // Transition to Idle with a short randomized delay before moving again
            TransitionToIdle();
        }

        #endregion

        #region Navigation Helpers

        /// <summary>
        /// Samples a random valid, reachable position on the NavMesh within the leash radius of the anchor.
        /// </summary>
        private bool TryFindWanderDestination(out Vector3 destination)
        {
            destination = Vector3.zero;
            if (agent == null || !agent.isOnNavMesh) return false;

            for (int i = 0; i < maxSampleAttempts; i++)
            {
                // Sample random point on horizontal disk inside the leash radius
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * leashRadius;
                Vector3 candidatePoint = anchorPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, sampleMaxDistance, NavMesh.AllAreas))
                {
                    // Verify that a complete path exists to candidate
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        destination = hit.position;
                        return true;
                    }
                }
            }

            // Fallback: Attempt returning towards anchor position if random sampling failed
            if (NavMesh.SamplePosition(anchorPosition, out NavMeshHit anchorHit, sampleMaxDistance, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(anchorHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    destination = anchorHit.position;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Dynamically updates the anchor position to a new location.
        /// </summary>
        public void SetAnchor(Vector3 newAnchor)
        {
            anchorPosition = newAnchor;
            anchorInitialized = true;
        }

        /// <summary>
        /// Teleports or returns the agent to its anchor position.
        /// </summary>
        public void ResetToAnchor()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(anchorPosition);
            }
            else
            {
                transform.position = anchorPosition;
            }
            TransitionToIdle();
        }

        /// <summary>
        /// Forces the agent to transition to Wander immediately if a valid destination can be found.
        /// </summary>
        public void ForceWander()
        {
            if (currentState == RoamingState.Interacting) return;

            if (TryFindWanderDestination(out Vector3 destination))
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(destination);
                    TransitionToWander();
                }
            }
        }

        /// <summary>
        /// Forces the agent to transition to Idle immediately.
        /// </summary>
        public void ForceIdle()
        {
            if (currentState == RoamingState.Interacting) return;
            TransitionToIdle();
        }

        #endregion

        #region Player Resolution & Smooth Rotation

        private Transform ResolvePlayerTarget()
        {
            if (focusTarget != null) return focusTarget;

            // 1. Check PlayerBody tag (matches CameraReframer convention)
            GameObject playerBody = GameObject.FindGameObjectWithTag("PlayerBody");
            if (playerBody != null) return playerBody.transform;

            // 2. Check Player tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) return player.transform;

            // 3. Fallback to PlayerController component in scene
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) return pc.transform;

            // 4. Main camera transform as last resort
            if (Camera.main != null) return Camera.main.transform;

            return null;
        }

        private IEnumerator SmoothLookAtTarget(Transform target, float duration)
        {
            Quaternion startRotation = transform.rotation;
            float timeElapsed = 0f;

            while (timeElapsed < duration)
            {
                if (target == null) yield break;

                Vector3 direction = target.position - transform.position;
                direction.y = 0f; // Constrain to horizontal plane (no tilting)

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timeElapsed / duration);
                }

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            if (target != null)
            {
                Vector3 finalDirection = target.position - transform.position;
                finalDirection.y = 0f;
                if (finalDirection.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(finalDirection);
                }
            }

            rotateCoroutine = null;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying && anchorInitialized ? anchorPosition : transform.position;

            // Territory leash boundary
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.4f);
            Gizmos.DrawWireSphere(center, leashRadius);

            // Anchor center marker
            Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 0.85f);
            Gizmos.DrawSphere(center, 0.25f);

            // Active path & destination visualization in play mode
            if (Application.isPlaying && currentState == RoamingState.Wander && agent != null && agent.hasPath)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, agent.destination);
                Gizmos.DrawWireSphere(agent.destination, 0.35f);
            }
        }

        #endregion
    }
}
