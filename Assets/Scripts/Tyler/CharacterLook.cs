using Unity.Netcode;
using UnityEngine;

public class CharacterLook : NetworkBehaviour
{
    CharacterInput input;
    [SerializeField] Transform cameraPivot;
    [SerializeField] float sensitivity = 0.1f;
    [SerializeField] float maxPitch = 89f;

    float pitch;
    // TODO: rotate head, pass transform to server?

    void Awake()
    {
        input = GetComponent<CharacterInput>();
    }

    void LateUpdate()
    {
        if (!IsOwner) return;

        Vector2 look = input.Look * sensitivity;
        transform.Rotate(Vector3.up * look.x);
        pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}