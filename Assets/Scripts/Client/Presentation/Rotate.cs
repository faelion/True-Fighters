using UnityEngine;

public class Rotate : MonoBehaviour
{
    public int speed = 20;

    void Update()
    {
        transform.Rotate(transform.rotation.x, transform.rotation.y + Time.deltaTime * speed, transform.rotation.z);
    }
}
