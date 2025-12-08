using UnityEngine;

public class DetachCamera : MonoBehaviour
{
    void Start()
    {
        transform.SetParent(null);
    }
}
