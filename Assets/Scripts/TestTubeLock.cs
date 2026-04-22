using UnityEngine;

public class TestTubeLock : MonoBehaviour
{
    [SerializeField]
    private Transform rackTopLimit; // ponto acima do rack
    [SerializeField]
    private Transform socketPoint; // ponto no meio do furo do rack
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private Rigidbody rb;
    private bool isLockedInRack = true;
    void Start()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        LockInRack();
    }
    void Update()
    {
        if (isLockedInRack)
        {
            CheckIfReleased();
        }
    }
    void LockInRack()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation
        | RigidbodyConstraints.FreezePositionX
        | RigidbodyConstraints.FreezePositionZ;
        isLockedInRack = true;
    }
    void UnlockFromRack()
    {
        rb.constraints = RigidbodyConstraints.None;
        isLockedInRack = false;
    }
    void CheckIfReleased()
    {
        if (transform.position.y > rackTopLimit.position.y)
        {
            UnlockFromRack();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TubeSocket") && !isLockedInRack)
        {
            SnapToSocket();
        }
    }
    void SnapToSocket()
    {
        transform.position = socketPoint.position;
        transform.rotation = socketPoint.rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        LockInRack();
    }
}