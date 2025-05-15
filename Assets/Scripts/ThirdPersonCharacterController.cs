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
    private Vector3 velocity;

    private bool isGrounded;
    private bool hasJumped;
    private bool groundedDelayActive = true;

    private Recorder recorder;
    private TMP_InputField chatInputField;

    private void Awake()
{
    controller = GetComponent<CharacterController>();
    recorder = GetComponent<Recorder>();
    inputActions = new PlayerInputActions(); // ✅ moved here
}


    private IEnumerator Start()
    {
        controller = GetComponent<CharacterController>();
        recorder = GetComponent<Recorder>();

       if (!photonView.IsMine)
{
    if (playerCamera != null) playerCamera.SetActive(false);
    // animator stays enabled so PhotonAnimatorView can drive it
    yield break;
}


        if (playerCamera != null) playerCamera.SetActive(true);

        if (recorder != null)
        {
            recorder.TransmitEnabled = true;
            InvokeRepeating(nameof(UpdateMicUI), 0f, 0.1f);
        }

        // Find chat input from scene (don't assign in prefab)
        chatInputField = FindObjectOfType<TMP_InputField>();

        yield return new WaitForSeconds(0.25f);
        groundedDelayActive = false;
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Jump.performed += _ => hasJumped = true;
    }

    private void OnDisable()
    {
        if (!photonView.IsMine) return;

        inputActions.Gameplay.Jump.performed -= _ => hasJumped = true;
        inputActions.Gameplay.Disable();
    }

    private void Update()
{
    if (!photonView.IsMine) return;

    if (groundedDelayActive) return;
    if (chatInputField != null && chatInputField.isFocused) return;

    inputMove = inputActions.Gameplay.Move.ReadValue<Vector2>(); // ✅ Poll every frame

    HandleGroundCheck();
    HandleMovement();
    HandleJump();
    ApplyGravity();
}


    private void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
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

        hasJumped = false;
    }

    private void ApplyGravity()
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
