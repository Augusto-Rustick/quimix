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

    [Header("Áudio")]
    [Tooltip("Som de vitória ao desbloquear a porta. Deixe vazio para usar placeholder.")]
    [SerializeField] private AudioClip clipVitoria;

    private AudioSource audioSource;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (clipVitoria == null)
            clipVitoria = GerarClipVitoria();
    }

    public void Destrancar()
    {
        if (!trancado) return;
        trancado = false;
        hinge.limits = limitesOriginais; // restaura -120 / 1
        grabPorta.enabled = true;

        if (outlinePorta != null)
            outlinePorta.OutlineWidth = 5f;

        audioSource.PlayOneShot(clipVitoria);

        Debug.Log("[EventosPorta] Porta destrancada!");
    }

    private AudioClip GerarClipVitoria()
    {
        int sampleRate = 44100;
        float duracao = 0.8f;
        int samples = Mathf.RoundToInt(sampleRate * duracao);
        AudioClip clip = AudioClip.Create("VitoriaPlaceholder", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        float[] frequencias = { 523f, 659f, 784f };
        float duracaoNota = duracao / frequencias.Length;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            int notaIndex = Mathf.Min((int)(t / duracaoNota), frequencias.Length - 1);
            float tNota = (t % duracaoNota) / duracaoNota;
            float envelope = Mathf.Sin(Mathf.PI * tNota);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequencias[notaIndex] * t) * envelope * 0.4f;
        }

        clip.SetData(data, 0);
        return clip;
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