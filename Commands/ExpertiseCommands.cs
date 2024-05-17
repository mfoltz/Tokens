using Cobalt.Hooks;
using Cobalt.Systems.Expertise;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Expertise.WeaponStats;
using static Cobalt.Systems.Sanguimancy.BloodStats;

namespace Cobalt.Commands
{
    public static class ExpertiseCommands
    {
        [Command(name: "getMasteryProgress", shortHand: "gmp", adminOnly: false, usage: ".gmp", description: "Display your current mastery progress.")]
        public static void GetMasteryCommand(ChatCommandContext ctx)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weaponGuid = equipment.WeaponSlot.SlotEntity._Entity.Read<PrefabGUID>();
            string weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(weaponGuid).ToString();

            IWeaponMasteryHandler handler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
            if (handler == null)
            {
                ctx.Reply($"No mastery handler found for {weaponType}.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            var masteryData = handler.GetExperienceData(steamID);

            // masteryData.Key represents the level, and masteryData.Value represents the experience.
            if (masteryData.Key > 0 || masteryData.Value > 0)
            {
                ctx.Reply($"Your expertise is <color=yellow>{masteryData.Key}</color> (<color=white>{ExpertiseSystem.GetLevelProgress(steamID, handler)}%</color>) with {weaponType}.");
            }
            else
            {
                ctx.Reply($"You haven't gained any expertise for {weaponType} yet.");
            }
        }

        [Command(name: "logExpertiseProgress", shortHand: "lep", adminOnly: false, usage: ".lep", description: "Toggles mastery progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["CombatLogging"] = !bools["CombatLogging"];
            }
            ctx.Reply($"Weapon expertise logging is now {(bools["CombatLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "chooseWeaponStat", shortHand: "cws", adminOnly: true, usage: ".cws <Stat>", description: "Choose a weapon stat to enhance based on your weapon mastery.")]
        public static void ChooseWeaponStat(ChatCommandContext ctx, string statChoice)
        {
            string statType = statChoice.ToLower();
            // If not, try parsing it from the string representation
            if (!Enum.TryParse<WeaponStatManager.WeaponStatType>(statType, true, out _))
            {
                ctx.Reply("Invalid weapon stat choice, use .lws to see options.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            Entity weapon = equipment.WeaponSlot.SlotEntity._Entity;
            PrefabGUID prefabGUID;
            if (weapon.Equals(Entity.Null))
            {
                prefabGUID = new(0);
            }
            else
            {
                prefabGUID = weapon.Read<PrefabGUID>();
            }
            // Ensure that there is a dictionary for the player's stats
            if (!Core.DataStructures.PlayerWeaponChoices.TryGetValue(steamID, out var _))
            {
                Dictionary<string, List<string>> weaponsStats = [];
                Core.DataStructures.PlayerWeaponChoices[steamID] = weaponsStats;
            }
            string weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(prefabGUID).ToString();
            // Ensure that there are stats registered for the specific weapon

            // Choose a stat for the specific weapon stats instance
            if (PlayerWeaponUtilities.ChooseStat(steamID, weaponType, statType))
            {
                ctx.Reply($"Stat {statType} has been chosen for {weaponType}.");
                Core.DataStructures.SavePlayerWeaponChoices();
            }
            else
            {
                ctx.Reply("You have already chosen two stats for this weapon.");
            }
        }

        [Command(name: "resetWeaponStats", shortHand: "rws", adminOnly: false, usage: ".rws", description: "Reset the stat choices for a player's currently equipped weapon stats.")]
        public static void ResetWeaponStats(ChatCommandContext ctx)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlot.SlotEntity._Entity.Read<PrefabGUID>();
            string weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(weapon).ToString();

            UnitStatsOverride.RemoveWeaponBonuses(character, weaponType);
            PlayerWeaponUtilities.ResetChosenStats(steamID, weaponType);
            //Core.DataStructures.SavePlayerWeaponChoices();
            ctx.Reply("Your weapon stats have been reset for the currently equipped weapon.");
        }

        [Command(name: "setWeaponMastery", shortHand: "swm", adminOnly: true, usage: ".swm [Weapon] [Level]", description: "Sets your weapon mastery level.")]
        public static void MasterySetCommand(ChatCommandContext ctx, string weaponType, int level)
        {
            if (level < 0 || level > ExpertiseSystem.MaxWeaponExpertiseLevel)
            {
                ctx.Reply($"Level must be between 0 and {ExpertiseSystem.MaxWeaponExpertiseLevel}.");
                return;
            }

            var masteryHandler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
            if (masteryHandler == null)
            {
                ctx.Reply("Invalid weapon type.");
                return;
            }

            ulong steamId = ctx.Event.User.PlatformId;
            //var xpData = masteryHandler.GetExperienceData(steamId);

            // Update mastery level and XP
            var xpData = new KeyValuePair<int, float>(level, ExpertiseSystem.ConvertLevelToXp(level));
            masteryHandler.UpdateExperienceData(steamId, xpData);
            masteryHandler.SaveChanges();

            ctx.Reply($"Expertise for {masteryHandler.GetWeaponType()} set to {level}.");
        }

        [Command(name: "listWeaponStats", shortHand: "lws", adminOnly: false, usage: ".lws", description: "Lists weapon stat choices.")]
        public static void ListBloodStatsCommand(ChatCommandContext ctx)
        {
            string weaponStats = string.Join(", ", Enum.GetNames(typeof(WeaponStatManager.WeaponStatType)));
            ctx.Reply($"Available weapon stats: {weaponStats}");
        }
    }
}