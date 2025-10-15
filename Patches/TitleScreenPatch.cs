using Panik;
using System.Linq;

namespace QOLverPit.Patches;

[HarmonyPatch]
internal class TitleScreenPatch
{
    [HarmonyPatch(typeof(ScreenMenuScript), nameof(ScreenMenuScript.Open))]
    [HarmonyPrefix]
    internal static void ScreenMenuScript_Open_Prefix(ref string[] options, ref ScreenMenuScript.OptionEvent[] optionEvents)
    {
        var match = Strings.Sanitize(Strings.SantizationKind.menus, Translation.Get("SCREEN_MENU_OPTION_NEW_RUN"));
        if (options != null && options.Length > 0 && options.Any(x => x == match))
        {
            options = options.Append(Translation.Get("MENU_OPTION_QUIT")).ToArray();
            optionEvents = optionEvents.Append(() =>
            {
                
                Sound.Play("SoundMenuSelect");
                GameplayMaster.SetGamePhase(GameplayMaster.GamePhase.closingGame, forceSame: false);
            }).ToArray();
        }
    }
}