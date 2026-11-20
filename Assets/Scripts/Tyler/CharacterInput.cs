using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    InputAction move, look;

    public Vector2 Move => move.ReadValue<Vector2>();
    public Vector2 Look => look.ReadValue<Vector2>();

    void Awake()
    {
        move = InputSystem.actions.FindAction("Player/Move");
        look = InputSystem.actions.FindAction("Player/Look");
    }
}