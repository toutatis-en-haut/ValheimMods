using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace SpawnPointGateways.SpawnPoints
{
    /// <summary>
    /// Per-player, per-world list of every position the local player has marked as a
    /// spawn point. Persists in <see cref="Player.m_customData"/> under a single string
    /// key so the data travels with the character save without us inventing a new file
    /// format. Each entry is prefixed with the world UID so a character that has played
    /// multiple worlds only sees the markers for the world it is currently in.
    /// </summary>
    internal static class SpawnPointRegistry
    {
        public const string CustomDataKey = "spg_spawn_points_v2";

        private const float DedupeRadiusMeters = 2.5f;
        private const ulong NoWorld = 0UL;

        private struct Entry
        {
            public ulong WorldUid;
            public Vector3 Pos;
        }

        private static readonly List<Entry> _all = new List<Entry>();
        private static Player _cachedPlayer;

        /// <summary>Returns the spawn points visible in the current world only.</summary>
        public static List<Vector3> GetAll(Player player)
        {
            EnsureLoaded(player);
            ulong currentWorld = TryGetWorldUid();
            var result = new List<Vector3>();
            for (int i = 0; i < _all.Count; i++)
            {
                var e = _all[i];
                if (currentWorld == NoWorld || e.WorldUid == NoWorld || e.WorldUid == currentWorld)
                {
                    result.Add(e.Pos);
                }
            }
            return result;
        }

        public static void Record(Player player, Vector3 worldPos)
        {
            if (player == null) return;
            EnsureLoaded(player);

            ulong worldUid = TryGetWorldUid();

            for (int i = 0; i < _all.Count; i++)
            {
                var e = _all[i];
                bool sameWorld = e.WorldUid == worldUid || e.WorldUid == NoWorld || worldUid == NoWorld;
                if (!sameWorld) continue;
                if (Vector2.Distance(
                        new Vector2(e.Pos.x, e.Pos.z),
                        new Vector2(worldPos.x, worldPos.z)) <= DedupeRadiusMeters)
                {
                    _all[i] = new Entry { WorldUid = worldUid, Pos = worldPos };
                    Save(player);
                    return;
                }
            }

            _all.Add(new Entry { WorldUid = worldUid, Pos = worldPos });
            Save(player);
            SpawnPointGatewaysPlugin.Log?.LogInfo(
                $"[SPG] Recorded spawn point #{_all.Count} (world {worldUid}) at ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1}).");
        }

        public static void InvalidateCacheFor(Player player)
        {
            if (_cachedPlayer == player) return;
            _all.Clear();
            _cachedPlayer = null;
        }

        private static void EnsureLoaded(Player player)
        {
            if (player == null) return;
            if (_cachedPlayer == player) return;

            _all.Clear();
            _cachedPlayer = player;

            var data = GetCustomData(player);
            if (data == null) return;

            if (data.TryGetValue(CustomDataKey, out string raw) && !string.IsNullOrEmpty(raw))
            {
                ParseV2(raw);
            }
            else if (data.TryGetValue("spg_spawn_points_v1", out string rawV1) && !string.IsNullOrEmpty(rawV1))
            {
                ParseV1(rawV1);
                Save(player);
            }

            SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Loaded {_all.Count} remembered spawn point(s) for player.");
        }

        private static void ParseV2(string raw)
        {
            foreach (var entry in raw.Split('|'))
            {
                var parts = entry.Split(',');
                if (parts.Length != 4) continue;
                if (!ulong.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong w)) continue;
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;
                _all.Add(new Entry { WorldUid = w, Pos = new Vector3(x, y, z) });
            }
        }

        private static void ParseV1(string raw)
        {
            // v1 entries were just "x,y,z" with no world UID. We don't know which world
            // they came from, so tag them as NoWorld — they will still show up everywhere
            // (best-effort migration for the handful of v0.1.x testers).
            foreach (var entry in raw.Split('|'))
            {
                var parts = entry.Split(',');
                if (parts.Length != 3) continue;
                if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;
                _all.Add(new Entry { WorldUid = NoWorld, Pos = new Vector3(x, y, z) });
            }
            SpawnPointGatewaysPlugin.Log?.LogInfo($"[SPG] Migrated {_all.Count} entries from v1 storage.");
        }

        private static void Save(Player player)
        {
            if (player == null) return;
            var data = GetCustomData(player);
            if (data == null) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _all.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var e = _all[i];
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "{0},{1:R},{2:R},{3:R}", e.WorldUid, e.Pos.x, e.Pos.y, e.Pos.z);
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

        private static ulong TryGetWorldUid()
        {
            try
            {
                if (ZNet.instance == null) return NoWorld;
                var method = AccessTools.Method(typeof(ZNet), "GetWorldUID");
                if (method == null) return NoWorld;
                var result = method.Invoke(ZNet.instance, System.Array.Empty<object>());
                if (result is long l) return unchecked((ulong)l);
                if (result is ulong u) return u;
                if (result is int i) return unchecked((ulong)i);
                if (result is uint ui) return ui;
            }
            catch (System.Exception ex)
            {
                SpawnPointGatewaysPlugin.Log?.LogWarning($"[SPG] Could not read world UID: {ex.Message}");
            }
            return NoWorld;
        }
    }
}
