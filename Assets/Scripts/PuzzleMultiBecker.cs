using UnityEngine;

public class PuzzleMultiBecker : MonoBehaviour
{
    [System.Serializable]
    public class RequisitoTubo
    {
        [Tooltip("Tubo que deve contribuir para este béquer")]
        public DerramarLiquido tubo;
        [Tooltip("Quantas unidades deste tubo são necessárias")]
        public int unidades;
    }

    [System.Serializable]
    public class ConfigBecker
    {
        [Tooltip("Béquer a verificar")]
        public BeckerLiquido becker;
        [Tooltip("Lista de tubos e quantidades necessárias")]
        public RequisitoTubo[] tubosNecessarios;
    }

    [Header("Configuração do Puzzle")]
    [Tooltip("Um entry por béquer — arraste o béquer e defina os tubos necessários")]
    public ConfigBecker[] beckers;

    [Header("Porta")]
    public EventosPorta porta;

    private bool resolvido = false;

    void Update()
    {
        if (resolvido) return;

        if (TodosCorretos())
        {
            resolvido = true;
            Debug.Log("[PuzzleMultiBecker] Puzzle resolvido!");
            porta.Destrancar();
        }
    }

    bool TodosCorretos()
    {
        foreach (var cfg in beckers)
        {
            if (cfg.becker == null) return false;

            foreach (var req in cfg.tubosNecessarios)
            {
                if (req.tubo == null) return false;

                int contribuido = cfg.becker.GetUnidadesDeTubo(req.tubo.gameObject.GetInstanceID());
                if (contribuido != req.unidades)
                    return false;
            }

            // Béquer não pode ter unidades extras (total deve bater)
            int totalEsperado = 0;
            foreach (var req in cfg.tubosNecessarios)
                totalEsperado += req.unidades;

            if (cfg.becker.TotalUnidades != totalEsperado)
                return false;
        }

        return true;
    }
}
