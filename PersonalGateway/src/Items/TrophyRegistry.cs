using System.Collections.Generic;
using PersonalGateway.Config;

namespace PersonalGateway.Items
{
    internal static class TrophyRegistry
    {
        private static HashSet<string> _uncommon;
        private static HashSet<string> _rare;
        private static HashSet<string> _boss;

        public static bool IsTrophy(ItemDrop.ItemData item)
        {
            return item != null
                && item.m_shared != null
                && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
        }

        public static float GetXp(ItemDrop.ItemData item)
        {
            if (!IsTrophy(item)) return 0f;
            EnsureLoaded();
            var prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            if (prefabName == null) return GatewayConfig.XpCommon.Value;

            if (_boss.Contains(prefabName)) return GatewayConfig.XpBoss.Value;
            if (_rare.Contains(prefabName)) return GatewayConfig.XpRare.Value;
            if (_uncommon.Contains(prefabName)) return GatewayConfig.XpUncommon.Value;
            return GatewayConfig.XpCommon.Value;
        }

        public static void Invalidate()
        {
            _uncommon = null;
            _rare = null;
            _boss = null;
        }

        private static void EnsureLoaded()
        {
            if (_uncommon == null) _uncommon = GatewayConfig.ParseSet(GatewayConfig.TrophyTierUncommon);
            if (_rare == null) _rare = GatewayConfig.ParseSet(GatewayConfig.TrophyTierRare);
            if (_boss == null) _boss = GatewayConfig.ParseSet(GatewayConfig.TrophyTierBoss);
        }
    }
}
