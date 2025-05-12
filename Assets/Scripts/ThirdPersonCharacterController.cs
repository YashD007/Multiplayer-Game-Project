using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("References")]
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 inputMove;
    private Vector3 velocity;
    private bool isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Move.performed += ctx => inputMove = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled += _ => inputMove = Vector2.zero;
        inputActions.Gameplay.Jump.performed += _ => Jump();
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Jump.performed -= _ => Jump();
        inputActions.Gameplay.Move.performed -= ctx => inputMove = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled -= _ => inputMove = Vector2.zero;
        inputActions.Gameplay.Disable();
    }

    private void Update()
    {
        MovePlayer();
        ApplyGravity();
    }

    private void MovePlayer()
    {
        isGrounded = controller.isGrounded;

        Vector3 moveDirection = new Vector3(inputMove.x, 0f, inputMove.y);

        if (moveDirection.magnitude >= 0.1f)
        {
            // Rotate based on local player direction (not camera)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // Move in the forward direction of the character
            Vector3 move = transform.forward * moveDirection.magnitude;
            controller.Move(move * moveSpeed * Time.deltaTime);

            animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", moveDirection.magnitude);
        }
        else
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0f);
        }
    }

    private void Jump()
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // small downward force to keep grounded
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
