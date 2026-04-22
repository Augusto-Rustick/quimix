using UnityEngine;

/// <summary>
/// Attach este script em cada tubo de ensaio.
/// Requer: AudioSource no mesmo GameObject, e referência ao BeckerLiquido da cena.
/// </summary>
public class DerramarLiquido : MonoBehaviour
{
    [Header("Configuração de Derramamento")]
    [SerializeField] private float anguloMin = 100f;

    [Header("Líquido do Tubo")]
    [Tooltip("Objeto filho que representa o mesh do líquido dentro do tubo")]
    [SerializeField] private Transform meshLiquidoTubo;

    [Tooltip("Escala Y máxima do mesh (= tubo cheio). Ajuste conforme seu modelo.")]
    [SerializeField] private float escalaYMaxima = 1f;

    [Tooltip("Escala Y mínima do mesh (= tubo vazio).")]
    [SerializeField] private float escalaYMinima = 0f;

    [Tooltip("Velocidade de esvaziamento do tubo ao derramar (unidades de escala Y por segundo)")]
    [SerializeField] private float velocidadeDerramar = 0.3f;

    [Header("Béquer Alvo")]
    [Tooltip("Referência ao script do béquer que receberá o líquido")]
    [SerializeField] private BeckerLiquido beckerAlvo;

    [Header("Áudio")]
    [Tooltip("AudioClip do som de líquido derramando. Deixe vazio para usar beep de placeholder.")]
    [SerializeField] private AudioClip clipDerramar;

    [Header("Cor do Líquido (RN02)")]
    [Tooltip("Nome da propriedade de cor no shader. '_BaseColor' para URP, '_Color' para Built-in.")]
    [SerializeField] private string propriedadeCor = "_BaseColor";

    // --- estado interno ---
    private bool inZonaDerramar = false;
    private bool estahDerramando = false;
    private AudioSource audioSource;
    private Outline outline;
    private Renderer rendererLiquidoTubo;
    private Material materialInstancia; // instância cacheada, evita criar nova a cada frame

    /// <summary>
    /// Cor atual do líquido deste tubo, lida do material instanciado do meshLiquidoTubo.
    /// Lê _BaseColor (cor base) com fallback para .color.
    /// </summary>
    public Color CorLiquido
    {
        get
        {
            if (materialInstancia == null) return Color.white;
            if (materialInstancia.HasProperty(propriedadeCor))
                return materialInstancia.GetColor(propriedadeCor);
            return materialInstancia.color;
        }
    }

    /// <summary>
    /// Cor de emissão do líquido deste tubo (propriedade _EmissionColor do URP/Lit).
    /// Retorna Color.black se o material não tiver emissão.
    /// </summary>
    public Color EmissaoLiquido
    {
        get
        {
            if (materialInstancia == null) return Color.black;
            if (materialInstancia.HasProperty("_EmissionColor"))
                return materialInstancia.GetColor("_EmissionColor");
            return Color.black;
        }
    }

    // Proporção atual de líquido no tubo (0 = vazio, 1 = cheio)
    public float ProporcaoAtual => meshLiquidoTubo != null
        ? Mathf.InverseLerp(escalaYMinima, escalaYMaxima, meshLiquidoTubo.localScale.y)
        : 0f;

    public bool EstaVazio => ProporcaoAtual <= 0.001f;

    /// <summary>
    /// Chamado pelo BeckerLiquido no Start para definir o nível inicial do tubo.
    /// proporcao: valor entre 0 (vazio) e 1 (cheio).
    /// </summary>
    public void DefinirProporcaoInicial(float proporcao)
    {
        if (meshLiquidoTubo == null) return;
        proporcao = Mathf.Clamp01(proporcao);
        Vector3 escala = meshLiquidoTubo.localScale;
        escala.y = Mathf.Lerp(escalaYMinima, escalaYMaxima, proporcao);
        meshLiquidoTubo.localScale = escala;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // Se nenhum clip foi atribuído, gera um beep de placeholder em runtime
        if (clipDerramar == null)
            clipDerramar = GerarClipPlaceholder();

        audioSource.clip = clipDerramar;

        outline = GetComponent<Outline>();

        // Busca o Renderer automaticamente no mesmo objeto do mesh do líquido
        // e cacheia a instância do material (evita criar nova instância a cada acesso)
        if (meshLiquidoTubo != null)
        {
            rendererLiquidoTubo = meshLiquidoTubo.GetComponent<Renderer>();
            if (rendererLiquidoTubo != null)
                materialInstancia = rendererLiquidoTubo.material; // .material já cria a instância
            else
                Debug.LogWarning($"[{gameObject.name}] Nenhum Renderer encontrado no meshLiquidoTubo. A cor não será lida.");
        }
    }

    void Update()
    {
        // Só processa se estiver na zona de derramar
        if (!inZonaDerramar)
        {
            if (estahDerramando)
                PararDerramar();
            return;
        }

        float angulo = Vector3.Angle(transform.up, Vector3.up);
        bool deveriaEstarDerramando = angulo > anguloMin && !EstaVazio;

        if (deveriaEstarDerramando && !estahDerramando)
            IniciarDerramar();
        else if (!deveriaEstarDerramando && estahDerramando)
            PararDerramar();

        if (estahDerramando)
            ProcessarDerramamento();
    }

    void IniciarDerramar()
    {
        estahDerramando = true;
        audioSource.Play();
        beckerAlvo?.RegistrarCorTubo(CorLiquido, EmissaoLiquido, gameObject.GetInstanceID());
        Debug.Log($"[{gameObject.name}] Começou a derramar. Cor: {CorLiquido}");
    }

    void PararDerramar()
    {
        estahDerramando = false;
        audioSource.Stop();
        Debug.Log($"[{gameObject.name}] Parou de derramar.");
    }

    void ProcessarDerramamento()
    {
        if (meshLiquidoTubo == null || beckerAlvo == null) return;

        // Quanto vai diminuir no tubo neste frame
        float deltaDiminuir = velocidadeDerramar * Time.deltaTime;

        // Limita para não passar do escalaYMinima
        Vector3 escala = meshLiquidoTubo.localScale;
        float novaEscalaY = Mathf.Max(escalaYMinima, escala.y - deltaDiminuir);
        float diminuiuReal = escala.y - novaEscalaY; // o quanto realmente diminuiu

        // Aplica no tubo
        escala.y = novaEscalaY;
        meshLiquidoTubo.localScale = escala;

        // Converte diminuição de escala em proporção e manda pro béquer
        float proporcaoTransferida = diminuiuReal / (escalaYMaxima - escalaYMinima);
        beckerAlvo.ReceberLiquido(proporcaoTransferida);

        // Se esvaziou, para de derramar
        if (EstaVazio)
        {
            Debug.Log($"[{gameObject.name}] Tubo vazio!");
            meshLiquidoTubo.gameObject.SetActive(false);
            PararDerramar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ZonaDerramar"))
        {
            if (outline != null) outline.OutlineWidth = 5f;
            inZonaDerramar = true;
            Debug.Log($"[{gameObject.name}] Entrou na ZonaDerramar.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ZonaDerramar"))
        {
            if (outline != null) outline.OutlineWidth = 0f;
            inZonaDerramar = false;
            Debug.Log($"[{gameObject.name}] Saiu da ZonaDerramar.");
        }
    }

    /// <summary>
    /// Gera um AudioClip simples de "borbulha/água" como placeholder em runtime.
    /// Substitua pelo seu AudioClip real no Inspector.
    /// </summary>
    private AudioClip GerarClipPlaceholder()
    {
        int sampleRate = 44100;
        float duracao = 1f;
        int samples = Mathf.RoundToInt(sampleRate * duracao);
        AudioClip clip = AudioClip.Create("DerramarPlaceholder", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            // Ruído branco suavizado simula som de líquido
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t / duracao); // fade in/out
            data[i] = Random.Range(-0.15f, 0.15f) * envelope;
        }

        clip.SetData(data, 0);
        return clip;
    }
}