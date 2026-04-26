using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

using UnityEngine.XR.Interaction.Toolkit.Interactables; 
public class EventosPorta : MonoBehaviour
{
    private bool isOpen = false;
    private bool trancado = true;
    private HingeJoint hinge;
    private JointLimits limitesOriginais;
    private Outline outlinePorta;
    public TeleportationArea teleporte;

    public XRGrabInteractable grabPorta;
    void Start()
    {
        hinge = GetComponent<HingeJoint>();
        limitesOriginais = hinge.limits;

        // Trava o joint zerando os limites de rotação
        JointLimits travado = hinge.limits;
        travado.min = 0;
        travado.max = 0;
        hinge.limits = travado;

        grabPorta.enabled = false;

        outlinePorta = GetComponent<Outline>();
        if (outlinePorta != null)
            outlinePorta.OutlineWidth = 0f;
    }

    public void Destrancar()
    {
        if (!trancado) return;
        trancado = false;
        hinge.limits = limitesOriginais; // restaura -120 / 1
        grabPorta.enabled = true;

        if (outlinePorta != null)
            outlinePorta.OutlineWidth = 5f;

        Debug.Log("[EventosPorta] Porta destrancada!");
    }
    void Update()
    {
        if (trancado) return;

        float angle = hinge.angle;

        // abriu
        if (!isOpen && angle <= -40)
        {
            isOpen = true;
            teleporte.enabled = true;
            grabPorta.enabled = true;
        }
        else
        {
            // Porta fechou
            // por causa da precisao do float, tive que testar com 1 grau a menos
            if (isOpen && angle > -40)
            {
                isOpen = false;
                teleporte.enabled = false;
                grabPorta.enabled = false; 
            }
        }
    }
}