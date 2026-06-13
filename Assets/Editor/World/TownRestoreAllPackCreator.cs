#if UNITY_EDITOR
using UnityEditor;

namespace JRogue.Editor.World
{
    /// <summary>Runs all town pack creators in dependency order (idempotent).</summary>
    public static class TownRestoreAllPackCreator
    {
        [MenuItem("JRogue/Town/Restore All Town Packs")]
        public static void RestoreAllTownPacks()
        {
            NpcDialogPackCreator.CreateNpcDialogPack();
            TownPackCreator.CreateTownTestData();
            ShopNpcPackCreator.CreateShopNpcPack();
            DragonianElderPackCreator.CreateDragonianElderPack();
            DragonianSpellPackCreator.CreateDragonianSpellPack();
            ShamanBarbarianPackCreator.CreateShamanBarbarianPack();
            FairyMerchantPackCreator.CreateFairyMerchantPack();
            BeastmanSoulBeastPackCreator.CreateBeastmanSoulBeastPack();
            FleshmetalForgemasterPackCreator.CreateFleshmetalForgemasterPack();
            MageSpellPackCreator.CreateHumanMageSpellPack();
            TownNpcPrefabRebuild.RebuildAllHumanDerived();
            TownTorchPackCreator.PlaceTownTorches();
            TownPackCreator.ApplyTownPlazaMarkerLayout();
            TownPackCreator.FixTownTestScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[TownRestore] All town packs restored.");
        }

        /// <summary>Entry point for Unity batchmode: -executeMethod JRogue.Editor.World.TownRestoreAllPackCreator.RestoreAllTownPacksBatch</summary>
        public static void RestoreAllTownPacksBatch()
        {
            RestoreAllTownPacks();
            EditorApplication.Exit(0);
        }
    }
}
#endif
