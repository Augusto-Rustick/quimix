using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach este script no béquer.
/// Gerencia o nível de líquido recebido dos tubos de ensaio.
/// 
/// RN01: A soma do líquido dos 3 tubos não pode ultrapassar a capacidade do béquer.
///        O béquer sobe o mesh de líquido conforme recebe líquido dos tubos.
/// RN02: A cor do líquido no béquer é a média simples das cores dos tubos já derramados.
/// </summary>
public class BeckerLiquido : MonoBehaviour
{
    [Header("Mesh do Líquido do Béquer")]
    [Tooltip("Objeto filho que representa o mesh do líquido dentro do béquer")]
    [SerializeField] private Transform meshLiquidoBecker;

    [Tooltip("Escala Y do mesh quando o béquer está CHEIO (capacidade máxima)")]
    [SerializeField] private float escalaYCheia = 1f;

    [Tooltip("Escala Y do mesh quando o béquer está VAZIO")]
    [SerializeField] private float escalaYVazia = 0f;

    [Header("Tubos de Ensaio (RN01)")]
    [Tooltip("Arraste aqui os 3 scripts DerramarLiquido de cada tubo")]
    [SerializeField] private DerramarLiquido[] tubos = new DerramarLiquido[3];

    [Tooltip("Proporção inicial de cada tubo (0 a 1). Ex: 0.5 = tubo meio cheio)")]
    [SerializeField] private float[] proporcaoInicialTubos = new float[] { 0.5f, 0.5f, 0.5f };

    [Header("Porta (destranca ao encher)")]
    [Tooltip("Arraste aqui o script EventosPorta da porta")]
    [SerializeField] private EventosPorta porta;

    [Header("Cor do Líquido (RN02)")]
    [Tooltip("Nome da propriedade de cor base no shader. '_BaseColor' (URP) ou '_Color' (Built-in).")]
    [SerializeField] private string propriedadeCor = "_BaseColor";

    // Proporção atual de líquido no béquer (0 = vazio, 1 = cheio)
    private float proporcaoBecker = 0f;

    // Capacidade máxima do béquer em "unidades de proporção"
    private float capacidadeMaxima = 1f;

    // Renderer e material instanciado do mesh do líquido do béquer
    private Renderer rendererLiquidoBecker;
    private Material materialInstanciaBecker;

    // Cores base e de emissão registradas (uma entrada por tubo)
    private List<Color> coresBase      = new List<Color>();
    private List<Color> coresEmissao   = new List<Color>();
    private HashSet<int> tubosJaRegistrados = new HashSet<int>();

    void Start()
    {
        if (meshLiquidoBecker != null)
        {
            rendererLiquidoBecker = meshLiquidoBecker.GetComponent<Renderer>();
            if (rendererLiquidoBecker != null)
                materialInstanciaBecker = rendererLiquidoBecker.material; // cria instância uma única vez
            else
                Debug.LogWarning("[BeckerLiquido] Nenhum Renderer encontrado no meshLiquidoBecker.");
        }

        InicializarTubos();
        CalcularCapacidadeMaxima();
        AtualizarMeshBecker();
    }

    /// <summary>
    /// Inicializa a escala Y dos meshes dos tubos com base nas proporções iniciais configuradas.
    /// </summary>
    void InicializarTubos()
    {
        for (int i = 0; i < tubos.Length; i++)
        {
            if (tubos[i] == null) continue;
            float propInicial = i < proporcaoInicialTubos.Length ? proporcaoInicialTubos[i] : 1f;
            propInicial = Mathf.Clamp01(propInicial);
            tubos[i].DefinirProporcaoInicial(propInicial);
        }
    }

    /// <summary>
    /// A capacidade máxima do béquer é a soma das proporções iniciais dos tubos.
    /// </summary>
    void CalcularCapacidadeMaxima()
    {
        capacidadeMaxima = 0f;
        foreach (float p in proporcaoInicialTubos)
            capacidadeMaxima += Mathf.Clamp01(p);

        if (capacidadeMaxima <= 0f)
            capacidadeMaxima = 1f;

        Debug.Log($"[BeckerLiquido] Capacidade máxima: {capacidadeMaxima:F2}");
    }

    /// <summary>
    /// Chamado por DerramarLiquido a cada frame que está derramando.
    /// </summary>
    public void ReceberLiquido(float proporcaoTransferida)
    {
        if (proporcaoTransferida <= 0f) return;

        float incrementoBecker = proporcaoTransferida / capacidadeMaxima;
        proporcaoBecker = Mathf.Clamp01(proporcaoBecker + incrementoBecker);
        AtualizarMeshBecker();

        if (proporcaoBecker >= 1f)
            Debug.Log("[BeckerLiquido] Béquer cheio!");

        if (proporcaoBecker > 0f && porta != null)
            porta.Destrancar();
    }

    /// <summary>
    /// RN02 — Chamado por DerramarLiquido quando um tubo começa a derramar pela primeira vez.
    /// Registra cor base + emissão do tubo e recalcula a mistura no béquer.
    /// </summary>
    public void RegistrarCorTubo(Color corBase, Color corEmissao, int tuboInstanceID = 0)
    {
        if (tuboInstanceID != 0 && tubosJaRegistrados.Contains(tuboInstanceID))
            return;

        if (tuboInstanceID != 0)
            tubosJaRegistrados.Add(tuboInstanceID);

        coresBase.Add(corBase);
        coresEmissao.Add(corEmissao);
        AtualizarCorBecker();

        Debug.Log($"[BeckerLiquido] Cor registrada — Base: {corBase}  Emissão: {corEmissao}. Tubos misturados: {coresBase.Count}");
    }

    /// <summary>
    /// Calcula a média simples de todas as cores registradas e aplica _BaseColor e _EmissionColor.
    /// </summary>
    void AtualizarCorBecker()
    {
        if (materialInstanciaBecker == null || coresBase.Count == 0) return;

        Color baseMix    = MediaCores(coresBase);
        Color emissaoMix = MediaCores(coresEmissao);

        // Aplica cor base
        if (materialInstanciaBecker.HasProperty(propriedadeCor))
            materialInstanciaBecker.SetColor(propriedadeCor, baseMix);
        else
            materialInstanciaBecker.color = baseMix;

        // Aplica emissão (só se o material tiver a propriedade)
        if (materialInstanciaBecker.HasProperty("_EmissionColor"))
            materialInstanciaBecker.SetColor("_EmissionColor", emissaoMix);

        Debug.Log($"[BeckerLiquido] Base misturada: {baseMix} | Emissão misturada: {emissaoMix}");
    }

    static Color MediaCores(List<Color> lista)
    {
        float r = 0, g = 0, b = 0, a = 0;
        foreach (var c in lista) { r += c.r; g += c.g; b += c.b; a += c.a; }
        int n = lista.Count;
        return new Color(r / n, g / n, b / n, a / n);
    }

    void AtualizarMeshBecker()
    {
        if (meshLiquidoBecker == null) return;

        bool vazio = proporcaoBecker <= 0f;
        meshLiquidoBecker.gameObject.SetActive(!vazio);

        if (!vazio)
        {
            Vector3 escala = meshLiquidoBecker.localScale;
            escala.y = Mathf.Lerp(escalaYVazia, escalaYCheia, proporcaoBecker);
            meshLiquidoBecker.localScale = escala;
        }
    }

    public void Resetar()
    {
        proporcaoBecker = 0f;
        coresBase.Clear();
        coresEmissao.Clear();
        tubosJaRegistrados.Clear();
        AtualizarMeshBecker();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.3f,
            $"Béquer: {proporcaoBecker * 100f:F0}% | Tubos misturados: {coresBase?.Count ?? 0}"
        );
    }
#endif
}
