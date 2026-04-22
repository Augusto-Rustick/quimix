using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach em cada botão da sala 2.
/// RN05: o botão do meio (isBotaoDoMeio = true) deve ser pressionado 3x para desbloquear a porta.
/// Ao desbloquear: Outline na porta é ativado e som de vitória toca.
/// O Outline some quando a porta abrir (abertura >= 0.6).
/// </summary>
public class BotaoPressionado : MonoBehaviour
{
    public int numBotao;

    [Header("Puzzle (só preencha no botão do meio)")]
    [Tooltip("Marque apenas no botão do meio")]
    [SerializeField] private bool isBotaoDoMeio = false;

    [Tooltip("Quantas pressões para desbloquear a porta")]
    [SerializeField] private int pressoesNecessarias = 3;

    [Tooltip("Referência ao GameObject da porta (que tem EventosPortaCorrer e Outline)")]
    [SerializeField] private EventosPortaCorrer porta;

    [Header("Áudio")]
    [Tooltip("Som de vitória ao desbloquear a porta. Deixe vazio para usar placeholder.")]
    [SerializeField] private AudioClip clipVitoria;

    // --- estado interno ---
    private int contadorPressoes = 0;
    private bool desbloqueado = false;
    private AudioSource audioSource;
    private Outline outlinePorta;

    void Start()
    {
        GetComponent<XRSimpleInteractable>().selectEntered.AddListener(x => Pressionei());

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (clipVitoria == null)
            clipVitoria = GerarClipVitoria();

        // Busca o Outline direto na porta — basta adicionar o componente Outline nela
        if (porta != null)
        {
            outlinePorta = porta.GetComponent<Outline>();
            if (outlinePorta != null)
                outlinePorta.OutlineWidth = 0f;
            else
                Debug.LogWarning("[BotaoPressionado] Outline não encontrado na porta. Adicione o componente Outline no GameObject da porta.");
        }
    }

    void Update()
    {
        if (!desbloqueado || outlinePorta == null) return;

        if (outlinePorta.OutlineWidth > 0f && porta.EstaAberta)
        {
            outlinePorta.OutlineWidth = 0f;
            Debug.Log("[BotaoPressionado] Porta abriu — Outline removido.");
        }
    }

    public void Pressionei()
    {
        print("PRESS " + numBotao);

        if (!isBotaoDoMeio || desbloqueado) return;

        contadorPressoes++;
        Debug.Log($"[BotaoPressionado] Pressão {contadorPressoes}/{pressoesNecessarias}");

        if (contadorPressoes >= pressoesNecessarias)
            Desbloquear();
    }

    void Desbloquear()
    {
        desbloqueado = true;
        Debug.Log("[BotaoPressionado] Porta desbloqueada!");

        if (outlinePorta != null)
            outlinePorta.OutlineWidth = 5f;

        audioSource.PlayOneShot(clipVitoria);
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
}