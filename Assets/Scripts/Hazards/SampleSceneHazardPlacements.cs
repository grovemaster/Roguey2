using UnityEngine;

namespace JRogue.Hazards
{
    /// <summary>
    /// Registers QA hazard placements for SampleScene when linked definitions are unset.
    /// Lava 2×2 pool at (6–7, 1–2); poison gas corridor at y = 4, x = 3–8.
    /// </summary>
    public sealed class SampleSceneHazardPlacements : MonoBehaviour
    {
        [SerializeField] EnvironmentalHazardDefinition lava;
        [SerializeField] EnvironmentalHazardDefinition poisonGas;

        void Awake()
        {
            if (HazardService.Instance == null)
            {
                var svcGo = new GameObject("HazardService");
                svcGo.transform.SetParent(transform);
                svcGo.AddComponent<HazardService>();
            }
        }

        void Start()
        {
            lava ??= Resources.Load<EnvironmentalHazardDefinition>("Hazards/EnvironmentalHazard_Lava");
            poisonGas ??= Resources.Load<EnvironmentalHazardDefinition>("Hazards/EnvironmentalHazard_PoisonGas");

            if (HazardService.Instance == null)
                return;

            if (lava != null)
            {
                for (int x = 6; x <= 7; x++)
                {
                    for (int y = 1; y <= 2; y++)
                        HazardService.Instance.Register(new Vector3Int(x, y, 0), lava);
                }
            }

            if (poisonGas != null)
            {
                for (int x = 3; x <= 8; x++)
                    HazardService.Instance.Register(new Vector3Int(x, 4, 0), poisonGas);
            }
        }
    }
}
