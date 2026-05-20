using HarmonyLib;
using PersonalGateway.Config;
using PersonalGateway.Items;
using PersonalGateway.Skills;
using PersonalGateway.State;
using UnityEngine;

namespace PersonalGateway.Teleport
{
    internal static class TeleportController
    {
        private static float _lastClickTime = -10f;
        private static Vector3 _lastClickWorld;
        private static bool _firstClickArmed;

        public static void Tick()
        {
            if (GatewayState.Phase != ArmingPhase.AwaitingDestination) return;

            var player = Player.m_localPlayer;
            var minimap = Minimap.instance;
            if (player == null || minimap == null) return;

            if (!BifrostTotem.PlayerHasTotem(player))
            {
                GatewayState.Reset();
                return;
            }

            if (GatewayState.SelectedTrophy == null
                || player.GetInventory() == null
                || !player.GetInventory().ContainsItem(GatewayState.SelectedTrophy))
            {
                player.Message(MessageHud.MessageType.Center, "$bifrost_msg_trophy_lost");
                GatewayState.Reset();
                return;
            }

            if (minimap.m_largeRoot == null || !minimap.m_largeRoot.activeSelf) return;
            if (!ModifierHeld()) return;

            int btn = GatewayConfig.TeleportMouseButton.Value;
            if (!Input.GetMouseButtonDown(btn)) return;

            if (!TryMouseToWorld(minimap, out Vector3 worldPos)) return;

            float now = Time.unscaledTime;
            float window = Mathf.Max(0.05f, GatewayConfig.DoubleClickWindowSeconds.Value);

            if (_firstClickArmed
                && (now - _lastClickTime) <= window
                && Vector3.Distance(worldPos, _lastClickWorld) < 50f)
            {
                _firstClickArmed = false;
                TryTeleport(player, worldPos);
            }
            else
            {
                _firstClickArmed = true;
                _lastClickTime = now;
                _lastClickWorld = worldPos;
            }
        }

        private static bool ModifierHeld()
        {
            return Input.GetKey(GatewayConfig.TeleportModifierKey.Value);
        }

        private static bool TryMouseToWorld(Minimap minimap, out Vector3 world)
        {
            world = Vector3.zero;
            if (minimap.m_mapImageLarge == null) return false;
            var rt = (RectTransform)minimap.m_mapImageLarge.transform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Input.mousePosition, null, out Vector2 local))
                return false;

            var size = rt.rect.size;
            if (size.x <= 0f || size.y <= 0f) return false;
            if (Mathf.Abs(local.x) > size.x * 0.5f || Mathf.Abs(local.y) > size.y * 0.5f) return false;

            float visX = (local.x / size.x) + 0.5f;
            float visY = (local.y / size.y) + 0.5f;
            var uv = minimap.m_mapImageLarge.uvRect;
            float mx = uv.x + visX * uv.width;
            float my = uv.y + visY * uv.height;

            world = MapPointToWorld(minimap, mx, my);
            return true;
        }

        private static Vector3 MapPointToWorld(Minimap minimap, float mx, float my)
        {
            float worldX = (mx - 0.5f) * minimap.m_textureSize * minimap.m_pixelSize;
            float worldZ = (my - 0.5f) * minimap.m_textureSize * minimap.m_pixelSize;
            return new Vector3(worldX, 0f, worldZ);
        }

        private static void TryTeleport(Player player, Vector3 dest)
        {
            float horizDist = Vector2.Distance(
                new Vector2(player.transform.position.x, player.transform.position.z),
                new Vector2(dest.x, dest.z));

            bool maxed = BifrostSkill.IsMaxed(player);
            float range = BifrostSkill.GetRangeMeters(player);
            if (!maxed && horizDist > range)
            {
                player.Message(MessageHud.MessageType.Center, "$bifrost_msg_too_far");
                return;
            }

            if (!IsExplored(dest))
            {
                player.Message(MessageHud.MessageType.Center, "$bifrost_msg_unknown");
                return;
            }

            float groundY = 50f;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(dest, out float gh))
            {
                groundY = gh + 0.5f;
            }
            dest.y = groundY;

            var trophy = GatewayState.SelectedTrophy;
            string trophyName = trophy != null && trophy.m_shared != null
                ? Localization.instance.Localize(trophy.m_shared.m_name)
                : string.Empty;
            float xp = TrophyRegistry.GetXp(trophy);

            if (trophy != null && player.GetInventory() != null)
            {
                player.GetInventory().RemoveItem(trophy, 1);
            }

            player.TeleportTo(dest, player.transform.rotation, distantTeleport: true);
            BifrostSkill.AwardXp(player, xp);
            GatewayState.Reset();

            if (Minimap.instance != null) Minimap.instance.SetMapMode(Minimap.MapMode.Small);

            var msg = Localization.instance.Localize("$bifrost_msg_teleported", trophyName);
            player.Message(MessageHud.MessageType.Center, msg);
        }

        private static bool IsExplored(Vector3 worldPos)
        {
            var minimap = Minimap.instance;
            if (minimap == null) return false;
            int textureSize = minimap.m_textureSize;
            float pixelSize = minimap.m_pixelSize;
            if (textureSize <= 0 || pixelSize <= 0f) return false;
            int half = textureSize / 2;
            int mx = Mathf.FloorToInt(worldPos.x / pixelSize) + half;
            int my = Mathf.FloorToInt(worldPos.z / pixelSize) + half;
            if (mx < 0 || my < 0 || mx >= textureSize || my >= textureSize) return false;

            var field = AccessTools.Field(typeof(Minimap), "m_explored");
            if (field == null) return true;
            var arr = field.GetValue(minimap) as bool[];
            if (arr == null) return true;
            int idx = my * textureSize + mx;
            if (idx < 0 || idx >= arr.Length) return false;
            return arr[idx];
        }
    }
}
