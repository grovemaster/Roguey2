#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Rebuilds town NPC prefabs derived from <see cref="NpcDialogPackCreator.RefreshHumanNpcBasePrefab"/>.
    /// Required after HumanPlayer or HumanNpc base changes so variant overrides stay valid.
    /// </summary>
    public static class TownNpcPrefabRebuild
    {
        public static void RebuildAllHumanDerived()
        {
            GameObject humanNpcBase = NpcDialogPackCreator.RefreshHumanNpcBasePrefab();
            if (humanNpcBase == null)
            {
                Debug.LogError("[TownNpcRebuild] Could not refresh HumanNpc base prefab.");
                return;
            }

            NpcDialogPackCreator.RebuildDialogTownNpcPrefabs(humanNpcBase);
            TownPackCreator.RebuildDemoHostNpcPrefab(humanNpcBase);
            ShopNpcPackCreator.RebuildTownNpcPrefabs(humanNpcBase);
            MageSpellPackCreator.RebuildTownNpcPrefabs(humanNpcBase);
            FairyMerchantPackCreator.RebuildTownNpcPrefab(humanNpcBase);
            BeastmanSoulBeastPackCreator.RebuildTownNpcPrefab(humanNpcBase);

            AssetDatabase.SaveAssets();
            Debug.Log("[TownNpcRebuild] Rebuilt all Human-derived town NPC prefabs.");
        }
    }
}
#endif
