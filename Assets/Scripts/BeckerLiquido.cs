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

    [Header("Cor do Líquido (RN02)")]
    [Tooltip("Nome da propriedade de cor base no shader. '_BaseColor' (URP) ou '_Color' (Built-in).")]
    [SerializeField] private string propriedadeCor = "_BaseColor";

    // Unidades atuais no béquer (0–3)
    private int unidadesBecker = 0;

    public bool PodeReceberMais => unidadesBecker < 3;
    public int TotalUnidades => unidadesBecker;

    // Rastreia quantas unidades cada tubo contribuiu (tuboInstanceID → unidades)
    private Dictionary<int, int> unidadesPorTubo = new Dictionary<int, int>();

    private Renderer rendererLiquidoBecker;
    private Material materialInstanciaBecker;

    private List<Color> coresBase    = new List<Color>();
    private List<Color> coresEmissao = new List<Color>();

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

        // Rastreia por tubo (permite múltiplas unidades do mesmo tubo)
        if (!unidadesPorTubo.ContainsKey(tuboInstanceID))
        {
            unidadesPorTubo[tuboInstanceID] = 0;
            // Registra a cor na primeira unidade deste tubo
            coresBase.Add(corBase);
            coresEmissao.Add(corEmissao);
            AtualizarCorBecker();
        }
        unidadesPorTubo[tuboInstanceID]++;

        unidadesBecker++;
        AtualizarMeshBecker();

        if (unidadesBecker >= 3)
            Debug.Log($"[{name}] Béquer cheio!");

        Debug.Log($"[{name}] +1 unidade do tubo {tuboInstanceID}. Total: {unidadesBecker}/3");
        return true;
    }

    /// <summary>
    /// Retorna quantas unidades o tubo com o instanceID dado contribuiu para este béquer.
    /// </summary>
    public int GetUnidadesDeTubo(int tuboInstanceID)
    {
        return unidadesPorTubo.TryGetValue(tuboInstanceID, out int u) ? u : 0;
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
        unidadesPorTubo.Clear();
        coresBase.Clear();
        coresEmissao.Clear();
        AtualizarMeshBecker();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.3f,
            $"Béquer: {unidadesBecker}/3 unidades"
        );
    }
#endif
}
