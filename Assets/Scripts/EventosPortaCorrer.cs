using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[RequireComponent(typeof(Rigidbody))]
public class EventosPortaCorrer : MonoBehaviour
{
    public TeleportationArea teleporte;

    [Tooltip("XRGrabInteractable do handle da porta — desabilitado até o puzzle ser resolvido")]
    [SerializeField] private XRGrabInteractable grabHandle;

    [SerializeField] private float distanciaAbertura = 3f;
    [SerializeField] private float duracaoAbertura = 1.2f;

    private bool isOpen = false;
    private bool ativado = false;
    private Rigidbody rb;

    public bool EstaAberta => isOpen;
    private ConfigurableJoint joint;

    private Outline outline;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        joint = GetComponent<ConfigurableJoint>();
        outline = GetComponent<Outline>();

        if (grabHandle != null)
            grabHandle.enabled = false;
        else
            Debug.LogWarning("[EventosPortaCorrer] grabHandle não atribuído — porta não está bloqueada.");
    }

    public void AtivarPuzzle()
    {
        if (ativado) return;
        ativado = true;
        grabHandle.enabled = true;
    }

     float GetJointLinearX()
    {
        Vector3 worldAnchor = joint.transform.TransformPoint(joint.anchor);
        Vector3 connectedAnchor = joint.connectedAnchor;
        Vector3 delta = worldAnchor - connectedAnchor;
        Vector3 axisX = joint.transform.TransformDirection(Vector3.right);
        float displacementX = Vector3.Dot(delta, axisX);
        return displacementX;
    }

    void Update()
    {
        float abertura = Mathf.Abs(GetJointLinearX());

        if (!isOpen && abertura >= 0.6)
        {
            isOpen = true;
            teleporte.enabled = true;
            outline.OutlineWidth = 0f;
        }
        else if (isOpen && abertura < 0.6)
        {
            isOpen = false;
            teleporte.enabled = false;
        }
    }

}
