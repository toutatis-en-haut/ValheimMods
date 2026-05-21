using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SpawnPointGateways.SpawnPoints
{
    /// <summary>
    /// Per-player list of every position that has ever been "set as spawn" for the local player.
    /// Persists in <see cref="Player.m_customData"/> under a single string key so it travels with
    /// the character save without us inventing a new file format.
    /// </summary>
    internal static class SpawnPointRegistry
    {
        public const string CustomDataKey = "spg_spawn_points_v1";

        private const float DedupeRadiusMeters = 2.5f;

        private static readonly List<Vector3> _cache = new List<Vector3>();
        private static Player _cachedPlayer;

        public static IReadOnlyList<Vector3> GetAll(Player player)
        {
            EnsureLoaded(player);
            return _cache;
        }

        public static void Record(Player player, Vector3 worldPos)
        {
            if (player == null) return;
            EnsureLoaded(player);

            for (int i = 0; i < _cache.Count; i++)
            {
                if (Vector2.Distance(
                        new Vector2(_cache[i].x, _cache[i].z),
                        new Vector2(worldPos.x, worldPos.z)) <= DedupeRadiusMeters)
                {
                    _cache[i] = worldPos;
                    Save(player);
                    return;
                }
            }

            _cache.Add(worldPos);
            Save(player);
            SpawnPointGatewaysPlugin.Log?.LogInfo(
                $"[SPG] Recorded spawn point #{_cache.Count} at ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1}).");
        }

        public static void InvalidateCacheFor(Player player)
        {
            if (_cachedPlayer == player) return;
            _cache.Clear();
            _cachedPlayer = null;
        }

        private static void EnsureLoaded(Player player)
        {
            if (player == null) return;
            if (_cachedPlayer == player) return;

            _cache.Clear();
            _cachedPlayer = player;

            var data = GetCustomData(player);
            if (data == null || !data.TryGetValue(CustomDataKey, out string raw) || string.IsNullOrEmpty(raw))
            {
                return;
            }

            foreach (var entry in raw.Split('|'))
            {
                var parts = entry.Split(',');
                if (parts.Length != 3) continue;
                if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)) continue;
                _cache.Add(new Vector3(x, y, z));
            }

            SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Loaded {_cache.Count} remembered spawn point(s) for player.");
        }

        private static void Save(Player player)
        {
            if (player == null) return;
            var data = GetCustomData(player);
            if (data == null) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _cache.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var v = _cache[i];
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                    "{0:R},{1:R},{2:R}", v.x, v.y, v.z);
            }
            data[CustomDataKey] = sb.ToString();
        }

        private static Dictionary<string, string> GetCustomData(Player player)
        {
            var field = AccessTools.Field(typeof(Player), "m_customData");
            if (field == null)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning("[SPG] Player.m_customData field not found; spawn points will not persist.");
                return null;
            }
            var value = field.GetValue(player) as Dictionary<string, string>;
            if (value == null)
            {
                value = new Dictionary<string, string>();
                field.SetValue(player, value);
            }
            return value;
        }
    }
}
