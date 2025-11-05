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
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Ray r = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(r, out RaycastHit hit, 100f, groundMask))
            {
                InputMessage m = new InputMessage()
                {
                    playerId = net.AssignedPlayerId,
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
                var dir = new Vector2(hit.point.x - transform.position.x, hit.point.z - transform.position.z);
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
                dir.Normalize();
                AbilityRequestMessage ar = new AbilityRequestMessage()
                {
                    playerId = net.AssignedPlayerId,
                    abilityIdOrKey = "Q",
                    targetType = AbilityTargetType.Point,
                    targetX = hit.point.x,
                    targetY = hit.point.z,
                    dirX = dir.x,
                    dirY = dir.y,
                    targetEntityId = 0,
                    seq = 0
                };
                net.SendAbility(ar);
            }
        }

        // Rest of moveset to be added in full version
    }
}
