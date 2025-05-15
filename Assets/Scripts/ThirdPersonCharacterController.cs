using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Voice.Unity;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class ThirdPersonCharacterController : MonoBehaviourPun
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float accelerationTime = 0.1f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private Image micIcon;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 inputMove;
    private Vector3 moveVelocity;
    private Vector3 smoothVelocity;
    private Vector3 velocity;

    private bool isGrounded;
    private bool groundedDelayActive = true;

    private Recorder recorder;
    private TMP_InputField chatInputField;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        recorder = GetComponent<Recorder>();
        inputActions = new PlayerInputActions();
        chatInputField = FindObjectOfType<TMP_InputField>();
    }

    private IEnumerator Start()
    {
        if (!photonView.IsMine)
        {
            if (playerCamera != null) playerCamera.SetActive(false);
            yield break;
        }

        if (playerCamera != null) playerCamera.SetActive(true);
        if (recorder != null)
        {
            recorder.TransmitEnabled = true;
            InvokeRepeating(nameof(UpdateMicUI), 0f, 0.1f);
        }

        yield return new WaitForSeconds(0.25f);
        groundedDelayActive = false;
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        var gameplay = inputActions.Gameplay;
        gameplay.Enable();
        gameplay.Jump.performed += _ => TryJump();
        gameplay.Interact.performed += _ => TryInteract();
    }

    private void OnDisable()
    {
        if (!photonView.IsMine) return;

        var gameplay = inputActions.Gameplay;
        gameplay.Jump.performed -= _ => TryJump();
        gameplay.Interact.performed -= _ => TryInteract();
        gameplay.Disable();
    }

    private void Update()
    {
        if (!photonView.IsMine || groundedDelayActive) return;

        // Pause all control if typing in chat
        if (chatInputField != null && chatInputField.isFocused)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0);
            return;
        }

        inputMove = inputActions.Gameplay.Move.ReadValue<Vector2>();

        HandleGroundCheck();
        HandleMovement();
        HandleGravity();
    }

    private void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleMovement()
    {
        Vector3 inputDir = new Vector3(inputMove.x, 0, inputMove.y).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            Vector3 targetVelocity = transform.forward * moveSpeed;
            moveVelocity = Vector3.SmoothDamp(moveVelocity, targetVelocity, ref smoothVelocity, accelerationTime);
            controller.Move(moveVelocity * Time.deltaTime);

            animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", inputDir.magnitude);
        }
        else
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0);
            moveVelocity = Vector3.SmoothDamp(moveVelocity, Vector3.zero, ref smoothVelocity, accelerationTime);
        }
    }

    private void HandleGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void TryJump()
    {
        if (isGrounded && !groundedDelayActive && (chatInputField == null || !chatInputField.isFocused))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }
    }

    private void TryInteract()
    {
        if (isGrounded && (chatInputField == null || !chatInputField.isFocused))
        {
            animator.SetTrigger("Interact");
        }
    }

    private void UpdateMicUI()
    {
        if (micIcon == null || recorder == null) return;

        bool isSpeaking = recorder.LevelMeter.CurrentAvgAmp > 0.01f;
        micIcon.color = isSpeaking ? Color.green : new Color(1, 1, 1, 0.3f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
#endif
}
