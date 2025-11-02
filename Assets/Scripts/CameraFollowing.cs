using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public string playerTag = "Player";
    public Vector3 offset = new Vector3(0f, 10f, -6f);
    public float smooth = 6f;
    Transform target;

    void Start()
    {
        var go = GameObject.FindWithTag(playerTag);
        if (go) target = go.transform;
    }

    void LateUpdate()
    {
        if (!target)
        {
            var go = GameObject.FindWithTag(playerTag);
            if (go) target = go.transform;
            return;
        }
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
        transform.LookAt(target);
    }
}
