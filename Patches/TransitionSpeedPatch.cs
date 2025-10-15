using static Panik.Data;

namespace QOLverPit.Patches;

[HarmonyPatch]
public class TransitionSpeedPatch
{
    [HarmonyPatch(typeof(SettingsData), nameof(SettingsData.TransitionSpeedMapped_Get))]
    [HarmonyPrefix]
    internal static void TransitionSpeedMapped_Get_Prefix(ref float from)
    {
        if (!Mathf.Approximately(Plugin.TransitionSpeedMultiplier.Value, 1f))
        {
            from = Plugin.TransitionSpeedMultiplier.Value;
        }
    }
}