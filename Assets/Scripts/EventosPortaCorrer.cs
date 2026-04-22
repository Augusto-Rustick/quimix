using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class EventosPortaCorrer : MonoBehaviour
{
    private bool isOpen = false;
    private ConfigurableJoint joint;
    public TeleportationArea teleporte;

    /// <summary>
    /// Lido pelo BotaoPressionado para saber quando remover o Outline da porta.
    /// </summary>
    public bool EstaAberta => isOpen;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
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
        }
        else if (isOpen && abertura < 0.6)
        {
            isOpen = false;
            teleporte.enabled = false;
        }
    }
}