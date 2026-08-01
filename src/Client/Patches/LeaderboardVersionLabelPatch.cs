using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTLeaderboard.Data;

namespace SPTLeaderboard.Patches
{
	public class LeaderboardVersionLabelPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(PreloaderUI), nameof(PreloaderUI.RefreshCornerLabel));
		}

		[PatchPrefix]
		private static void Prefix(PreloaderUI __instance)
		{
#if DEBUG
			const string buildType = " [DEBUG]";
#elif BETA
            const string buildType = " [BETA]";
#endif

#if DEBUG || BETA
			string sptlbVersion = $"SPTLB {GlobalData.Version}{buildType} - {GlobalData.SubVersion}";
#else
            string sptlbVersion = $"SPTLB {GlobalData.Version}";
#endif

			var field = AccessTools.Field(typeof(PreloaderUI), "string_5");
			var current = field.GetValue(__instance) as string;

			if (string.IsNullOrEmpty(current))
			{
				field.SetValue(__instance, sptlbVersion);
			}
			else if (!current.Contains("SPTLB"))
			{
				field.SetValue(__instance, current + " | " + sptlbVersion);
			}
		}
	}
}
