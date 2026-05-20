using Jotunn.Configs;
using Jotunn.Managers;
using PersonalGateway.Config;
using PersonalGateway.Items;

namespace PersonalGateway.Skills
{
    internal static class BifrostSkill
    {
        public const string Identifier = "studio.tribus.personalgateway.skill.bifrost";
        public static Skills.SkillType SkillType { get; private set; }

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
            if (player == null || player.m_skills == null) return 0f;
            return player.m_skills.GetSkillLevel(SkillType);
        }

        public static float GetRangeMeters(Player player)
        {
            var level = UnityEngine.Mathf.Max(1f, GetSkillLevel(player));
            var max = (float)System.Math.Max(1, GatewayConfig.MaxSkillLevel.Value);
            var range = GatewayConfig.MaxTeleportRangeMeters.Value * (level / max);
            return range;
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
    }
}
