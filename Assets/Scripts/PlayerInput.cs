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
        var mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Ray r = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(r, out RaycastHit hit, 100f, groundMask))
            {
                InputMessage m = new InputMessage()
                {
                    playerId = playerId,
                    isMove = true,
                    targetX = hit.point.x,
                    targetY = hit.point.z,
                    skillKey = null,
                    seq = 0
                };
                net.SendInput(m);
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Ray r = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(r, out RaycastHit hit, 100f, groundMask))
            {
                InputMessage m = new InputMessage()
                {
                    playerId = playerId,
                    isMove = false,
                    targetX = hit.point.x,
                    targetY = hit.point.z,
                    skillKey = "Q",
                    seq = 0
                };
                net.SendInput(m);
            }
        }

        // Rest of moveset to be added in full version
    }
}
