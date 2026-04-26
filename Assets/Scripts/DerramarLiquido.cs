using UnityEngine;

public class DerramarLiquido : MonoBehaviour
{
    [Header("Configuração de Derramamento")]
    [SerializeField] private float anguloMin = 100f;

    [Header("Líquido do Tubo")]
    [Tooltip("Objeto filho que representa o mesh do líquido dentro do tubo")]
    [SerializeField] private Transform meshLiquidoTubo;

    [Tooltip("Escala Y do mesh quando o tubo está CHEIO (3 unidades).")]
    [SerializeField] private float escalaYMaxima = 1f;

    [Tooltip("Escala Y do mesh quando o tubo está VAZIO.")]
    [SerializeField] private float escalaYMinima = 0f;

    [Tooltip("Velocidade de esvaziamento em unidades por segundo (cada tubo tem 3 unidades). Ex: 0.5 = 2s por unidade.")]
    [SerializeField] private float velocidadeDerramar = 0.5f;

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
    private int unidadesRestantes = 3;
    private float acumulador = 0f;    // acumula fração da próxima unidade (0–1)
    private float delayRestante = 0f; // countdown antes do primeiro drip

    private bool inZonaDerramar = false;
    private bool estahDerramando = false;
    private AudioSource audioSource;
    private Outline outline;
    private Renderer rendererLiquidoTubo;
    private Material materialInstancia;

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

    public int UnidadesRestantes => unidadesRestantes;
    public float ProporcaoAtual => unidadesRestantes / 3f;
    public bool EstaVazio => unidadesRestantes <= 0;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (clipDerramar == null)
            clipDerramar = GerarClipPlaceholder();

        audioSource.clip = clipDerramar;

        outline = GetComponent<Outline>();

        if (meshLiquidoTubo != null)
        {
            rendererLiquidoTubo = meshLiquidoTubo.GetComponent<Renderer>();
            if (rendererLiquidoTubo != null)
                materialInstancia = rendererLiquidoTubo.material;
            else
                Debug.LogWarning($"[{gameObject.name}] Nenhum Renderer encontrado no meshLiquidoTubo. A cor não será lida.");
        }

        AtualizarMeshTubo();
    }

    void Update()
    {
        if (!inZonaDerramar)
        {
            if (estahDerramando)
                PararDerramar();
            return;
        }

        float angulo = Vector3.Angle(transform.up, Vector3.up);
        bool deveriaEstarDerramando = angulo > anguloMin && !EstaVazio && beckerAlvo != null && beckerAlvo.PodeReceberMais;

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
        delayRestante = 1f;
        audioSource.Play();
        Debug.Log($"[{gameObject.name}] Começou a derramar. Unidades restantes: {unidadesRestantes}");
    }

    void PararDerramar()
    {
        estahDerramando = false;
        acumulador = 0f;
        delayRestante = 0f;
        audioSource.Stop();
        Debug.Log($"[{gameObject.name}] Parou de derramar. Unidades restantes: {unidadesRestantes}");
    }

    void ProcessarDerramamento()
    {
        if (beckerAlvo == null) return;

        // Béquer cheio: para imediatamente sem transferir
        if (!beckerAlvo.PodeReceberMais)
        {
            PararDerramar();
            return;
        }

        // Delay de 1s antes do primeiro drip
        if (delayRestante > 0f)
        {
            delayRestante -= Time.deltaTime;
            return;
        }

        acumulador += velocidadeDerramar * Time.deltaTime;

        // Loop: drena todas as unidades devidas no frame (evita acúmulo em frames lentos)
        while (acumulador >= 1f && !EstaVazio && beckerAlvo.PodeReceberMais)
        {
            acumulador -= 1f;
            bool aceitou = beckerAlvo.ReceberUnidade(CorLiquido, EmissaoLiquido, gameObject.GetInstanceID());
            if (aceitou)
            {
                unidadesRestantes = Mathf.Max(0, unidadesRestantes - 1);
                AtualizarMeshTubo();
                Debug.Log($"[{gameObject.name}] Transferiu 1 unidade. Restam: {unidadesRestantes}");
            }
            else break;
        }

        if (EstaVazio)
        {
            Debug.Log($"[{gameObject.name}] Tubo vazio!");
            PararDerramar();
        }
    }

    void AtualizarMeshTubo()
    {
        if (meshLiquidoTubo == null) return;

        bool vazio = unidadesRestantes <= 0;
        meshLiquidoTubo.gameObject.SetActive(!vazio);

        if (!vazio)
        {
            Vector3 escala = meshLiquidoTubo.localScale;
            escala.y = Mathf.Lerp(escalaYMinima, escalaYMaxima, unidadesRestantes / 3f);
            meshLiquidoTubo.localScale = escala;
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

    private AudioClip GerarClipPlaceholder()
    {
        int sampleRate = 44100;
        float duracao = 1f;
        int samples = Mathf.RoundToInt(sampleRate * duracao);
        AudioClip clip = AudioClip.Create("DerramarPlaceholder", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t / duracao);
            data[i] = Random.Range(-0.15f, 0.15f) * envelope;
        }

        clip.SetData(data, 0);
        return clip;
    }
}
