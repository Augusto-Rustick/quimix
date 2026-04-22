using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

using UnityEngine.XR.Interaction.Toolkit.Interactables; 
public class EventosPorta : MonoBehaviour
{
    private bool isOpen = false;
    private HingeJoint hinge;
    public TeleportationArea teleporte;

    public XRGrabInteractable grabPorta; 
    void Start()
    {
        hinge = GetComponent<HingeJoint>();
    }
    void Update()
    {
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