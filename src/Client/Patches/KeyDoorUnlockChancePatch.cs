using System.Reflection;
using EFT.Interactive;
using SPT.Reflection.Patching;
using SPTLeaderboard.Utils;
using UnityEngine;

namespace SPTLeaderboard.Patches
{
    public class KeyDoorUnlockChancePatch : ModulePatch
    {
        private const int UnlockChance = 60;

        protected override MethodBase GetTargetMethod() =>
            typeof(Door).GetMethod("Interact", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        [PatchPrefix]
        static bool Prefix(Door __instance, InteractionResult interactionResult)
        {
            if (ShouldUnlock(__instance, interactionResult))
                return true;

            __instance.Lock();
            return false;
        }

        internal static bool ShouldUnlock(Door door, InteractionResult interactionResult)
        {
            if (!(interactionResult is UnlockResult unlockResult) || !unlockResult.Succeed)
                return true;

            bool succeeded = Random.Range(0, 100) < UnlockChance;
            Logger.LogInfo($"[KeyDoorUnlockChance] {(succeeded ? "Succeeded" : "Failed")}: {door.name} ({UnlockChance}%).");
            return succeeded;
        }
    }

    public class KeycardDoorUnlockChancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(KeycardDoor).GetMethod("Interact", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        [PatchPrefix]
        static bool Prefix(KeycardDoor __instance, InteractionResult interactionResult)
        {
            if (KeyDoorUnlockChancePatch.ShouldUnlock(__instance, interactionResult))
                return true;

            __instance.StartCoroutine(__instance.UnlockFail());
            return false;
        }
    }
}
