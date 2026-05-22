using HarmonyLib;
using SpawnPointGateways.Config;
using SpawnPointGateways.Items;
using SpawnPointGateways.SpawnPoints;
using SpawnPointGateways.State;
using SpawnPointGateways.UI;
using UnityEngine;

namespace SpawnPointGateways.Teleport
{
    /// <summary>
    /// Drives the AwaitingDestination phase: lets the player click a spawn-point
    /// marker on the large map to commit a teleport. Clicks that don't land on a
    /// marker are ignored. Esc / right-click cancels.
    /// </summary>
    internal static class TeleportController
    {
        private const float PhaseSettleSeconds = 0.20f;

        private static bool _pendingMapClose;
        private static bool _mapWasOpenSinceArmed;

        public static void Tick()
        {
            if (_pendingMapClose && !Input.GetMouseButton(0))
            {
                _pendingMapClose = false;
                if (Minimap.instance != null) Minimap.instance.SetMapMode(Minimap.MapMode.Small);
            }

            if (GatewayState.Phase != ArmingPhase.AwaitingDestination)
            {
                _mapWasOpenSinceArmed = false;
                return;
            }

            var player = Player.m_localPlayer;
            var minimap = Minimap.instance;
            if (player == null || minimap == null) return;

            if (!BifrostCharm.PlayerHasCharm(player))
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo("[SPG] Charm no longer in inventory; cancelling.");
                Cancel();
                return;
            }

            bool largeMapOpen = minimap.m_largeRoot != null && minimap.m_largeRoot.activeSelf;
            if (largeMapOpen)
            {
                _mapWasOpenSinceArmed = true;
            }
            else if (_mapWasOpenSinceArmed)
            {
                // Player closed the map after we armed → treat as cancel.
                SpawnPointGatewaysPlugin.Log?.LogInfo("[SPG] Map closed while armed: cancelling.");
                player.Message(MessageHud.MessageType.Center, "$bifrost_charm_msg_cancelled");
                Cancel();
                return;
            }
            else
            {
                // Map hasn't opened yet (settle window between charm click and map open).
                return;
            }

            // Brief settle window so the right-click that triggered activation isn't
            // read as an immediate cancel.
            if (GatewayState.SecondsInPhase < PhaseSettleSeconds) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo("[SPG] Esc cancel.");
                player.Message(MessageHud.MessageType.Center, "$bifrost_charm_msg_cancelled");
                Cancel();
                return;
            }

            if (Input.GetMouseButtonDown(1) && IsMouseOverMap(minimap))
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo("[SPG] Right-click on map: cancel.");
                player.Message(MessageHud.MessageType.Center, "$bifrost_charm_msg_cancelled");
                Cancel();
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;
            if (!IsMouseOverMap(minimap)) return;

            if (TryFindMarkerUnderMouse(minimap, out Vector3 worldDest))
            {
                TryTeleport(player, worldDest);
            }
            else
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo("[SPG] Click ignored: not on a spawn-point marker.");
            }
        }

        private static void Cancel()
        {
            GatewayState.Reset();
            TeleportCursor.ForceReset();
            SpawnMarkerOverlay.Hide();
            _mapWasOpenSinceArmed = false;
        }

        private static bool IsMouseOverMap(Minimap minimap)
        {
            if (minimap.m_mapImageLarge == null) return false;
            var rt = (RectTransform)minimap.m_mapImageLarge.transform;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition);
        }

        private static bool TryFindMarkerUnderMouse(Minimap minimap, out Vector3 worldDest)
        {
            worldDest = Vector3.zero;
            var markers = SpawnMarkerOverlay.ActiveMarkers;
            if (markers == null || markers.Count == 0) return false;
            if (minimap.m_mapImageLarge == null) return false;

            var mapRt = (RectTransform)minimap.m_mapImageLarge.transform;
            var canvas = mapRt.GetComponentInParent<Canvas>();
            Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRt, Input.mousePosition, uiCam, out Vector2 mouseLocal))
            {
                return false;
            }

            int radius = Mathf.Max(4, GatewayConfig.MarkerRadiusPixels.Value);
            // Forgiving click hit-zone: a touch larger than the visible disc.
            int slop = Mathf.Max(4, Mathf.RoundToInt(radius * 0.4f));
            float hitRadius = radius + slop;
            float hitRadiusSq = hitRadius * hitRadius;

            float closestSq = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < markers.Count; i++)
            {
                var rt = markers[i];
                if (rt == null || !rt.gameObject.activeInHierarchy) continue;

                Vector2 center = rt.anchoredPosition;
                float dx = mouseLocal.x - center.x;
                float dy = mouseLocal.y - center.y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= hitRadiusSq && d2 < closestSq)
                {
                    closestSq = d2;
                    closestIndex = i;
                }
            }

            if (closestIndex < 0) return false;
            return SpawnMarkerOverlay.TryGetMarkerWorldPos(closestIndex, out worldDest);
        }

        private static void TryTeleport(Player player, Vector3 dest)
        {
            // Re-check resin at the moment of commit — the player could have dropped or
            // used some between arming and clicking a marker.
            var inv = player.GetInventory();
            int needed = Mathf.Max(0, GatewayConfig.ResinCost.Value);
            int have = ResinHelper.Count(inv);
            if (have < needed)
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Teleport aborted: resin dropped to {have}/{needed}.");
                var template = Localization.instance != null
                    ? Localization.instance.Localize("$bifrost_charm_msg_no_resin")
                    : "Not enough resin ({0} needed).";
                player.Message(MessageHud.MessageType.Center, template.Replace("{0}", needed.ToString()));
                Cancel();
                return;
            }

            ResinHelper.Remove(inv, needed);

            // Trust the stored Y — it came from PlayerProfile.SetCustomSpawnPoint, which is
            // Valheim's own canonical respawn position for the bed (accounts for floors and
            // elevation). Re-resolving via terrain height would drop us under built floors.
            // Only fall back to ResolveSafeY if the stored Y is clearly bogus.
            if (float.IsNaN(dest.y) || float.IsInfinity(dest.y) || dest.y < -1000f)
            {
                SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Stored Y ({dest.y}) looks invalid; resolving from terrain.");
                dest.y = ResolveSafeY(dest);
            }

            SpawnPointGatewaysPlugin.Log?.LogInfo(
                $"[SPG] Teleport: ({dest.x:F1}, {dest.y:F1}, {dest.z:F1}); {needed} Resin consumed.");

            player.TeleportTo(dest, player.transform.rotation, distantTeleport: true);

            GatewayState.Reset();
            TeleportCursor.ForceReset();
            SpawnMarkerOverlay.Hide();
            _pendingMapClose = true;

            player.Message(MessageHud.MessageType.Center, "$bifrost_charm_msg_teleported");
        }

        private static float ResolveSafeY(Vector3 dest)
        {
            const float clearance = 1.0f;
            float terrainY = float.MinValue;
            float waterY = 30f;

            try
            {
                var gen = WorldGenerator.instance;
                if (gen != null)
                {
                    var getHeight = AccessTools.Method(typeof(WorldGenerator), "GetHeight", new[] { typeof(float), typeof(float) });
                    if (getHeight != null)
                    {
                        var result = getHeight.Invoke(gen, new object[] { dest.x, dest.z });
                        if (result is float h) terrainY = h;
                    }
                }
            }
            catch (System.Exception ex)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning($"[SPG] WorldGenerator.GetHeight failed: {ex.Message}");
            }

            try
            {
                var zs = ZoneSystem.instance;
                if (zs != null)
                {
                    var field = AccessTools.Field(typeof(ZoneSystem), "m_waterLevel");
                    if (field != null && field.GetValue(zs) is float w) waterY = w;

                    if (zs.GetGroundHeight(dest, out float zoneH) && zoneH > terrainY)
                    {
                        terrainY = zoneH;
                    }
                }
            }
            catch (System.Exception ex)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning($"[SPG] ZoneSystem ground check failed: {ex.Message}");
            }

            if (terrainY == float.MinValue) terrainY = waterY;
            return Mathf.Max(terrainY, waterY) + clearance;
        }
    }
}
