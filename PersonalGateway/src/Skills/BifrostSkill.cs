using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using PersonalGateway.Config;
using PersonalGateway.Items;

namespace PersonalGateway.Skills
{
    internal static class BifrostSkill
    {
        public const string Identifier = "studio.tribus.personalgateway.skill.bifrost";
        public static global::Skills.SkillType SkillType { get; private set; }

        private static float _detectedWorldDiameter;
        private static bool _detectionLogged;

        public static void Register()
        {
            var skill = new SkillConfig
            {
                Identifier = Identifier,
                Name = "$bifrost_skill_name",
                Description = "$bifrost_skill_desc",
                IncreaseStep = 1f,
                Icon = StarIconBuilder.CreateStarSprite(64)
            };
            SkillType = SkillManager.Instance.AddSkill(skill);
        }

        public static float GetSkillLevel(Player player)
        {
            if (player == null) return 0f;
            var skills = Traverse.Create(player).Field("m_skills").GetValue<global::Skills>();
            if (skills == null) return 0f;
            return skills.GetSkillLevel(SkillType);
        }

        public static float GetRangeMeters(Player player)
        {
            var level = UnityEngine.Mathf.Max(1f, GetSkillLevel(player));
            var max = (float)System.Math.Max(1, GatewayConfig.MaxSkillLevel.Value);
            float maxRange = GatewayConfig.MaxTeleportRangeMeters.Value;
            if (GatewayConfig.AutoDetectWorldSize.Value)
            {
                float detected = TryDetectWorldDiameter();
                if (detected > 0f) maxRange = detected;
            }
            return maxRange * (level / max);
        }

        public static bool IsMaxed(Player player)
        {
            return GetSkillLevel(player) >= GatewayConfig.MaxSkillLevel.Value;
        }

        public static void AwardXp(Player player, float xp)
        {
            if (player == null || xp <= 0f) return;
            player.RaiseSkill(SkillType, xp);
        }

        private static float TryDetectWorldDiameter()
        {
            if (_detectedWorldDiameter > 0f) return _detectedWorldDiameter;

            var gen = WorldGenerator.instance;
            if (gen == null) return 0f;

            string[] radiusFieldNames = { "WorldRadius", "worldSize", "m_worldRadius", "m_worldSize" };
            foreach (var name in radiusFieldNames)
            {
                var field = AccessTools.Field(typeof(WorldGenerator), name);
                if (field == null) continue;
                object value = field.IsStatic ? field.GetValue(null) : field.GetValue(gen);
                if (value is float f && f > 0f)
                {
                    // Assume the field is a radius (Valheim's WorldRadius is 10000). Diameter = 2 * radius.
                    _detectedWorldDiameter = f * 2f;
                    if (!_detectionLogged)
                    {
                        PersonalGatewayPlugin.Log?.LogInfo($"[Bifrost] World size detected via WorldGenerator.{name}: radius={f}, diameter={_detectedWorldDiameter}.");
                        _detectionLogged = true;
                    }
                    return _detectedWorldDiameter;
                }
            }
            return 0f;
        }
    }
}
