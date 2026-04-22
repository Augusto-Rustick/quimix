using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// RN06: Quando o jogador chegar no chão 3 (fora do laboratório), o jogo termina.
/// Exibe mensagem de vitória com fade in + confete 3D ao redor do jogador.
/// </summary>
public class ShowMessageNaArea : MonoBehaviour
{
    [Header("Referências do Jogador")]
    public Transform playerCamera;
    public LocomotionMediator locomotionSystem;
    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;

    [Header("UI de Vitória")]
    public GameObject messageObject;
    [Tooltip("Canvas da mensagem — deve estar em modo World Space")]
    public Canvas canvasMensagem;
    [Tooltip("Distância do Canvas à frente da câmera do jogador")]
    [SerializeField] private float distanciaCanvas = 1.5f;
    [Tooltip("Opcional: Image de fundo do painel para o fade.")]
    public Image painelFundo;

    [Header("Confete")]
    [Tooltip("Quantos emissores de confete spawnar ao redor do jogador")]
    [SerializeField] private int quantidadeEmissores = 6;
    [Tooltip("Raio do círculo de emissores ao redor do jogador")]
    [SerializeField] private float raioConfete = 1.5f;
    [Tooltip("Duração total do confete em segundos")]
    [SerializeField] private float duracaoConfete = 4f;

    [Header("Áudio")]
    [SerializeField] private AudioClip clipVitoria;

    public float maxDistance = 3f;
    private bool triggered = false;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (clipVitoria == null)
            clipVitoria = GerarClipVitoria();
        audioSource.clip = clipVitoria;

        // Garante que a mensagem começa invisível
        if (messageObject != null)
            messageObject.SetActive(false);
    }

    void Update()
    {
        if (triggered) return;

        Ray ray = new Ray(playerCamera.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject == gameObject)
                StartCoroutine(TriggerVitoria());
        }
    }

    IEnumerator TriggerVitoria()
    {
        triggered = true;

        locomotionSystem.enabled = false;
        leftRay.enabled  = false;
        rightRay.enabled = false;

        audioSource.Play();
        SpawnarConfete();
        PosicionarCanvasNaFrente();
        yield return StartCoroutine(FadeInMensagem());

        Time.timeScale = 0;
    }

    // ── Posicionamento do Canvas ──────────────────────────────────────────────

   void PosicionarCanvasNaFrente()
{
    Transform alvo = canvasMensagem != null
        ? canvasMensagem.transform
        : messageObject?.transform;

    if (alvo == null) return;

    // Desparentar temporariamente para mover livre
    alvo.SetParent(null);

    Vector3 frente = playerCamera.forward;
    frente.y = 0f; // opcional: mantém na altura dos olhos
    frente.Normalize();

    alvo.position = playerCamera.position + frente * distanciaCanvas;
    alvo.rotation = Quaternion.LookRotation(playerCamera.forward, Vector3.up);
}

    // ── Fade In ──────────────────────────────────────────────────────────────

    IEnumerator FadeInMensagem()
    {
        messageObject.SetActive(true);

        var textoTMP = messageObject.GetComponentInChildren<TMPro.TMP_Text>();
        var textoLegacy = messageObject.GetComponentInChildren<Text>();

        Debug.Log($"[Vitoria] TMP encontrado: {textoTMP != null} | Legacy encontrado: {textoLegacy != null}");
        Debug.Log($"[Vitoria] Canvas posição: {canvasMensagem?.transform.position} | Câmera posição: {playerCamera.position}");
        Debug.Log($"[Vitoria] messageObject ativo: {messageObject.activeSelf}");

        if (textoTMP != null)
        {
            Debug.Log($"[Vitoria] TMP cor atual: {textoTMP.color} | alpha: {textoTMP.color.a}");
            textoTMP.color = new Color(textoTMP.color.r, textoTMP.color.g, textoTMP.color.b, 0f);
        }
        if (textoLegacy != null)
        {
            Color c = textoLegacy.color;
            textoLegacy.color = new Color(c.r, c.g, c.b, 0f);
        }
        if (painelFundo != null)
        {
            Color c = painelFundo.color;
            painelFundo.color = new Color(c.r, c.g, c.b, 0f);
        }

        yield return null;

        float duracao = 1.2f;
        float t = 0f;

        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / duracao);

            if (textoTMP != null)
            {
                textoTMP.color = new Color(textoTMP.color.r, textoTMP.color.g, textoTMP.color.b, alpha);
                textoTMP.SetAllDirty();
            }
            if (textoLegacy != null)
            {
                Color c = textoLegacy.color;
                textoLegacy.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (painelFundo != null)
            {
                Color c = painelFundo.color;
                painelFundo.color = new Color(c.r, c.g, c.b, alpha);
            }

            yield return null;
        }

        Debug.Log($"[Vitoria] Fade concluído. TMP alpha final: {textoTMP?.color.a}");
    }

    // ── Confete ───────────────────────────────────────────────────────────────

    void SpawnarConfete()
    {
        Color[] cores = {
            new Color(1f, 0.2f, 0.2f),   // vermelho
            new Color(0.2f, 0.8f, 0.2f), // verde
            new Color(0.2f, 0.4f, 1f),   // azul
            new Color(1f, 0.9f, 0.1f),   // amarelo
            new Color(1f, 0.4f, 0f),     // laranja
            new Color(0.8f, 0.2f, 1f),   // roxo
        };

        Vector3 origem = playerCamera.position;

        for (int i = 0; i < quantidadeEmissores; i++)
        {
            // Distribui em círculo ao redor do jogador
            float angulo = (360f / quantidadeEmissores) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * raioConfete;
            Vector3 posEmissor = origem + offset;

            GameObject emissor = new GameObject($"Confete_{i}");
            emissor.transform.position = posEmissor;

            var ps = emissor.AddComponent<ParticleSystem>();

            // CRÍTICO: para o sistema imediatamente — o Unity inicia automaticamente
            // ao fazer AddComponent, e setar duration com sistema rodando causa erro
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // --- Main ---
            var main = ps.main;
            main.duration           = duracaoConfete;
            main.loop               = false;
            main.startLifetime      = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            main.startSpeed         = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize          = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor         = new ParticleSystem.MinMaxGradient(
                                          cores[i % cores.Length],
                                          cores[(i + 2) % cores.Length]);
            main.gravityModifier    = 0.4f;
            main.simulationSpace    = ParticleSystemSimulationSpace.World;
            main.maxParticles       = 300;

            // --- Emission ---
            var emission = ps.emission;
            emission.rateOverTime = 80f;

            // --- Shape: cone apontando pra cima ---
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 25f;
            shape.radius    = 0.1f;

            // Aponta o emissor levemente para o centro (efeito de "explosão" convergindo)
            Vector3 direcaoCentro = (origem - posEmissor).normalized + Vector3.up * 1.5f;
            emissor.transform.rotation = Quaternion.LookRotation(direcaoCentro);

            // --- Rotation over Lifetime (gira os pedacinhos) ---
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z       = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

            // --- Renderer: quad achatado (parece confete) ---
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode    = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale   = 2.5f;
            // Shader compatível com URP — tenta Particles/Simple Lit, fallback para Unlit/Color
            Shader shaderConfete = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                ?? Shader.Find("Particles/Simple Lit")
                                ?? Shader.Find("Unlit/Color");
            renderer.material = new Material(shaderConfete);
            renderer.material.color = cores[i % cores.Length];

            ps.Play();
            Destroy(emissor, duracaoConfete + 4f); // limpa depois que acabar
        }
    }

    // ── Placeholder de áudio ──────────────────────────────────────────────────

    AudioClip GerarClipVitoria()
    {
        int sampleRate = 44100;
        float duracao  = 1.2f;
        int samples    = Mathf.RoundToInt(sampleRate * duracao);
        AudioClip clip = AudioClip.Create("VitoriaFimDeJogo", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        float[] freqs = { 523f, 659f, 784f, 1047f }; // dó mi sol dó (oitava acima)
        float duracaoNota = duracao / freqs.Length;

        for (int i = 0; i < samples; i++)
        {
            float t       = (float)i / sampleRate;
            int   nota    = Mathf.Min((int)(t / duracaoNota), freqs.Length - 1);
            float tNota   = (t % duracaoNota) / duracaoNota;
            float envelope= Mathf.Sin(Mathf.PI * tNota);
            data[i]       = Mathf.Sin(2f * Mathf.PI * freqs[nota] * t) * envelope * 0.4f;
        }

        clip.SetData(data, 0);
        return clip;
    }
}