using Panik;

namespace QOLverPit.Patches;

[HarmonyPatch]
public class IntroPatch
{
    [HarmonyPatch(typeof(IntroScript), nameof(IntroScript.Start))]
    [HarmonyPrefix]
    internal static bool IntroScript_Start_Postfix(IntroScript __instance)
    {
        if (Plugin.SkipSplashScreens.Value && Data.settings.initialLanguageSelectionPerfromed)
        {
            __instance.languageSelectionHolder.gameObject.SetActive(value: false);
            __instance.popUpHolder.SetActive(value: false);
            __instance.autosaveWarningHolder.SetActive(value: false);
            __instance.publisherIntroHolder.SetActive(value: false);
            __instance.developerIntroHolder.SetActive(value: false);
            __instance.musicianIntroHolder.SetActive(value: false);
            Level.GoTo(Level.SceneIndex.Game, false);
            return false;
        }
        return true;
    }
}