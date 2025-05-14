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
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpCutOffVelocity = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private Image micIcon;
    private TMP_InputField chatInputField;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private InputAction interactAction;
    private Vector2 inputMove;
    private Vector3 velocity;

    private bool isGrounded;
    private bool hasJumped;
    private bool cutJumpShort;
    private bool groundedDelayActive = true;

    private Recorder recorder;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        recorder = GetComponent<Recorder>();
    }

    private IEnumerator Start()
    {
        if (!photonView.IsMine)
        {
            if (playerCamera != null) playerCamera.SetActive(false);
            if (animator != null) animator.enabled = false;
            yield break;
        }

        if (playerCamera != null) playerCamera.SetActive(true);

        if (recorder != null)
        {
            recorder.TransmitEnabled = true;
            InvokeRepeating(nameof(UpdateMicUI), 0f, 0.1f);
        }

        yield return new WaitForSeconds(0.25f); // Prevent premature fall-through

        chatInputField = GameObject.FindWithTag("ChatInput").GetComponent<TMP_InputField>();

        groundedDelayActive = false;
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Move.performed += ctx => inputMove = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled += _ => inputMove = Vector2.zero;
        inputActions.Gameplay.Jump.performed += _ => hasJumped = true;
        inputActions.Gameplay.Jump.canceled += _ => cutJumpShort = true;

        interactAction = inputActions.Gameplay.Interact;
        interactAction.performed += _ => Interact();
    }

    private void OnDisable()
    {
        if (!photonView.IsMine) return;

        inputActions.Gameplay.Move.performed -= ctx => inputMove = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled -= _ => inputMove = Vector2.zero;
        inputActions.Gameplay.Jump.performed -= _ => hasJumped = true;
        inputActions.Gameplay.Jump.canceled -= _ => cutJumpShort = true;
        interactAction.performed -= _ => Interact();

        inputActions.Gameplay.Disable();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (chatInputField != null && chatInputField.isFocused) return;
        if (groundedDelayActive) return;

        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void HandleMovement()
    {
        Vector3 moveDir = new Vector3(inputMove.x, 0f, inputMove.y);

        if (moveDir.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            Vector3 move = transform.forward * moveDir.magnitude;
            controller.Move(move * moveSpeed * Time.deltaTime);

            animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", moveDir.magnitude);
        }
        else
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0f);
        }
    }

    private void HandleJump()
    {
        if (hasJumped && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        if (cutJumpShort && !isGrounded && velocity.y > jumpCutOffVelocity)
        {
            velocity.y = jumpCutOffVelocity;
        }

        hasJumped = false;
        cutJumpShort = false;
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
    }

    private void Interact()
    {
        if (animator != null)
        {
            animator.SetTrigger("Interact");
        }
    }

    private void UpdateMicUI()
    {
        if (micIcon == null || recorder == null) return;

        bool isSpeaking = recorder.LevelMeter.CurrentAvgAmp > 0.01f;
        micIcon.color = isSpeaking ? Color.green : new Color(1f, 1f, 1f, 0.3f);
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
