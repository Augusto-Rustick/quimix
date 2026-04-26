using UnityEngine;
using System.Collections.Generic;

public class BeckerLiquido : MonoBehaviour
{
    [Header("Mesh do Líquido do Béquer")]
    [Tooltip("Objeto filho que representa o mesh do líquido dentro do béquer")]
    [SerializeField] private Transform meshLiquidoBecker;

    [Tooltip("Escala Y do mesh quando o béquer está CHEIO (3 unidades)")]
    [SerializeField] private float escalaYCheia = 1f;

    [Tooltip("Escala Y do mesh quando o béquer está VAZIO")]
    [SerializeField] private float escalaYVazia = 0f;

    [Header("Porta (destranca ao receber primeira unidade)")]
    [Tooltip("Arraste aqui o script EventosPorta da porta")]
    [SerializeField] private EventosPorta porta;

    [Header("Cor do Líquido (RN02)")]
    [Tooltip("Nome da propriedade de cor base no shader. '_BaseColor' (URP) ou '_Color' (Built-in).")]
    [SerializeField] private string propriedadeCor = "_BaseColor";

    // Unidades atuais no béquer (0–3)
    private int unidadesBecker = 0;

    public bool PodeReceberMais => unidadesBecker < 3;

    private Renderer rendererLiquidoBecker;
    private Material materialInstanciaBecker;

    private List<Color> coresBase    = new List<Color>();
    private List<Color> coresEmissao = new List<Color>();
    private HashSet<int> tubosJaRegistrados = new HashSet<int>();

    void Start()
    {
        if (meshLiquidoBecker != null)
        {
            rendererLiquidoBecker = meshLiquidoBecker.GetComponent<Renderer>();
            if (rendererLiquidoBecker != null)
                materialInstanciaBecker = rendererLiquidoBecker.material;
            else
                Debug.LogWarning("[BeckerLiquido] Nenhum Renderer encontrado no meshLiquidoBecker.");
        }

        AtualizarMeshBecker();
    }

    /// <summary>
    /// Chamado por DerramarLiquido ao transferir uma unidade inteira.
    /// Retorna false se o béquer já está cheio (3 unidades).
    /// </summary>
    public bool ReceberUnidade(Color corBase, Color corEmissao, int tuboInstanceID)
    {
        if (unidadesBecker >= 3) return false;

        RegistrarCorTubo(corBase, corEmissao, tuboInstanceID);
        unidadesBecker++;
        AtualizarMeshBecker();

        if (unidadesBecker >= 3)
            Debug.Log("[BeckerLiquido] Béquer cheio!");

        if (unidadesBecker == 1 && porta != null)
            porta.Destrancar();

        return true;
    }

    void RegistrarCorTubo(Color corBase, Color corEmissao, int tuboInstanceID)
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

    void AtualizarCorBecker()
    {
        if (materialInstanciaBecker == null || coresBase.Count == 0) return;

        Color baseMix    = MediaCores(coresBase);
        Color emissaoMix = MediaCores(coresEmissao);

        if (materialInstanciaBecker.HasProperty(propriedadeCor))
            materialInstanciaBecker.SetColor(propriedadeCor, baseMix);
        else
            materialInstanciaBecker.color = baseMix;

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

        bool vazio = unidadesBecker <= 0;
        meshLiquidoBecker.gameObject.SetActive(!vazio);

        if (!vazio)
        {
            Vector3 escala = meshLiquidoBecker.localScale;
            escala.y = Mathf.Lerp(escalaYVazia, escalaYCheia, unidadesBecker / 3f);
            meshLiquidoBecker.localScale = escala;
        }
    }

    public void Resetar()
    {
        unidadesBecker = 0;
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
            $"Béquer: {unidadesBecker}/3 unidades | Tubos misturados: {coresBase?.Count ?? 0}"
        );
    }
#endif
}
