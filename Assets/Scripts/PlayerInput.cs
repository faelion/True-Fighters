using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInput : MonoBehaviour
{
    public int playerId = 1;
    private ClientNetwork net;
    public LayerMask groundMask;

    void Start()
    {
        net = GetComponent<ClientNetwork>();
    }

    void Update()
    {
        if (!net.HasAssignedId) return;

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        InputKind kind = InputKind.None;

        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            kind = InputKind.RightClick;
        }
        else if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame) kind = InputKind.Q;
            else if (keyboard.wKey.wasPressedThisFrame) kind = InputKind.W;
            else if (keyboard.eKey.wasPressedThisFrame) kind = InputKind.E;
            else if (keyboard.rKey.wasPressedThisFrame) kind = InputKind.R;
        }

        if (kind == InputKind.None) return;
        if (mouse == null) return; // need mouse position for world target

        Vector2 screenPos = mouse.position.ReadValue();
        Ray r = Camera.main.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(r, out RaycastHit hit, 100f, groundMask)) return;

        var msg = new InputMessage
        {
            playerId = net.AssignedPlayerId,
            kind = kind,
            targetX = hit.point.x,
            targetY = hit.point.z
        };
        net.SendInput(msg);

        // Rest of moveset to be added in full version
    }
}
