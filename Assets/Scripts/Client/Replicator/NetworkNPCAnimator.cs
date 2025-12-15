using UnityEngine;
using System.IO;
using ServerGame.Entities;
using Client.Replicator;

public class NetworkNpcAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private float runSpeedThreshold = 0.1f;
    [SerializeField] private float smoothTime = 0.1f;

    // public int TargetComponentType => (int)ComponentType.Transform;

    private Vector3 lastPos;
    private float currentSpeed;
    private float speedVel;

    void Awake()
    {
        TryFindAnimator();
        lastPos = transform.position;
    }

    private void TryFindAnimator()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator)
        {
            float dist = Vector3.Distance(transform.position, lastPos);
            float instantSpeed = dist / Time.deltaTime;
            float targetSpeed = (instantSpeed > runSpeedThreshold) ? 1f : 0f;

            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVel, smoothTime);
            animator.SetFloat(speedParam, currentSpeed);

            lastPos = transform.position;
        }
    }

    public void TriggerAttack()
    {
        if (animator) animator.SetTrigger(attackTrigger);
    }

    public void Initialize(ClientContent.NeutralEntitySO neutralDef)
    {
        // TODO: Map specific neutral animations if needed
        TryFindAnimator();
    }
}
