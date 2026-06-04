using ExtendedStorage.Storage;
using HarmonyLib;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(Container), nameof(Container.GetInventory))]
    internal static class ContainerGetInventoryPatch
    {
        // Route every GetInventory() caller (Take All, shift-click, hover
        // text, third-party display mods) through the active tab. Tab 0 is
        // still saved by vanilla Container.Save via m_inventory directly,
        // so this redirect does not break persistence.
        [HarmonyPostfix]
        private static void Postfix(Container __instance, ref Inventory __result)
        {
            var cab = __instance.GetComponent<CabinetContainer>();
            if (cab == null || !cab.IsReady) return;

            var active = cab.GetTab(cab.Storage.ActiveTab);
            if (active != null) __result = active;
        }
    }
}
