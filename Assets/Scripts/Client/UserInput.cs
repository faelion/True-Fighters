using UnityEngine;
using UnityEngine.InputSystem;
public class UserInput : MonoBehaviour
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
        if (mouse == null) return;

        Vector2 screenPos = mouse.position.ReadValue();
        Ray r = Camera.main.ScreenPointToRay(screenPos);
        
        // Raycast against everything so we can click on 'void' or non-walkable triggers too 
        // We trust the PathfindingService to snap it to NavMesh. 
        // (Assuming a giant collider or plane exists for the ray to hit at y=0, or we use Plane math)
        
        Vector3 targetPoint = Vector3.zero;
        if (Physics.Raycast(r, out RaycastHit hit, 200f)) 
        {
            targetPoint = hit.point;
        }
        else
        {
             // Fallback: Plane math if no collider
             Plane p = new Plane(Vector3.up, Vector3.zero);
             if (p.Raycast(r, out float dist)) targetPoint = r.GetPoint(dist);
             else return;
        }
        
        var msg = new InputMessage
        {
            playerId = net.AssignedPlayerId,
            kind = kind,
            targetX = targetPoint.x,
            targetY = targetPoint.z
        };
        if (GameSettings.UseMovementPrediction && kind == InputKind.RightClick)
        {
            if (NetEntityView.AllViews.TryGetValue(net.AssignedPlayerId, out var localView))
            {
                 localView.GetComponent<Client.Replicator.NetworkTransformVisual>()?.PredictMovement(new Vector3(targetPoint.x, 0, targetPoint.z));
            }
        }
        net.SendInput(msg);
    }
}
