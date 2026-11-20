using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(CharacterInput))]
public class CharacterMovement : NetworkBehaviour
{
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float gravity = -20f;

    CharacterController controller;
    CharacterInput input;

    Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<CharacterInput>();
    }

    void Update()
    {
        if (!IsOwner) return;

        // ground check + small downward force keeps isgrounded stable
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // horizontal movement relative to where the player is facing
        Vector2 m = input.Move;
        Vector3 move = transform.right * m.x + transform.forward * m.y;
        move = Vector3.ClampMagnitude(move, 1f);

        controller.Move(moveSpeed * Time.deltaTime * move);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}