using System.Numerics;

namespace QOLverPit.Patches;

[HarmonyPatch]
internal class DepositPatch
{
    internal enum DepositMode
    {
        Normal,
        Max,
        MaxPlus,
        All,
        Count
    }
    
    internal static DepositMode _currentDepositMode = DepositMode.Normal;
    
    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.NextDepositAmmountGet))]
    [HarmonyPrefix]
    internal static bool NextDepositAmmountGet_Prefix(ref BigInteger __result)
    
    {   var debt = GameplayData.DebtGet();
        var interval = debt <= 1073741823L ? Mathf.FloorToInt(debt.CastToInt() / 20f) : debt / 20;
        var coins = GameplayData.CoinsGet();
        var debtRemaining = GameplayData.DebtMissingGet();
        var leaveOver = GameplayData.SpinCostMax_Get();
        switch (_currentDepositMode)
        {
            case DepositMode.Max:
                if (coins <= leaveOver) return true; // Default logic
                if (coins >= debtRemaining + leaveOver)
                {
                    __result = BigInteger.Min(BigInteger.Max(1, (debtRemaining - 1) / interval) * interval, debtRemaining); // Round down to nearest interval
                }
                else
                {
                    __result = coins - leaveOver;
                }
                return false;
            case DepositMode.MaxPlus:
                if (coins <= leaveOver) return true; // Default logic
                if (coins >= debtRemaining + leaveOver)
                {
                    __result = BigInteger.Max(1, debtRemaining - 1);
                }
                else
                {
                    __result = coins - leaveOver;
                }
                return false;
            case DepositMode.All:
                __result = BigInteger.Min(coins, debtRemaining);
                return false;
            default: // (DepositMode.Normal and unhandled cases)
                return true; // Default logic
        }
    }
    
    [HarmonyPatch(typeof(PromptGuideScript), nameof(PromptGuideScript.SetGuideType))]
    [HarmonyPostfix]
    internal static void SetGuideType_Postfix(PromptGuideScript.GuideType type)
    {
        if (type != PromptGuideScript.GuideType.atm_insertCoin || _currentDepositMode == DepositMode.Normal ||
            ATMScript.Button_DealIsRunning() || GameplayData.CoinsGet() < GameplayData.NextDepositAmmountGet()) return;
        PromptGuideScript.instance.text.text += " " + GetModeText();
    }

    internal static void CycleDepositMode(bool reverse = false)
    {
        var direction = reverse ? -1 : 1;
        _currentDepositMode = (DepositMode)nmod((int)_currentDepositMode + direction, (int)DepositMode.Count);
        if (!Plugin.AllowMaxPlus.Value && _currentDepositMode == DepositMode.MaxPlus)
        {
            _currentDepositMode = (DepositMode)nmod((int)_currentDepositMode + direction, (int)DepositMode.Count);
        }

        DiegeticMenuElement element = DiegeticMenuController.ActiveMenu?.HoveredElement;
        if (element != null)
        {
            _skipDelay = true;
            element.RefreshHovering(true);
        }
    }
    
    private static bool _skipDelay = false;
    
    [HarmonyPatch(typeof(DiegeticMenuController), nameof(DiegeticMenuController.Update))]
    [HarmonyPostfix]
    internal static void DiegeticMenuController_Update_Postfix(DiegeticMenuController __instance)
    {
        if (_skipDelay)
        {
            if (__instance == DiegeticMenuController.ActiveMenu && __instance.runningDelay > 0f)
            {
                _skipDelay = false;
                __instance.runningDelay = 0f;
            }
        }
    }
    
    private static int nmod(int x, int m)
    {
        return ((x % m) + m) % m;
    }
    
    internal static string GetModeText()
    {
        return _currentDepositMode switch
        {
            DepositMode.Normal => "(Normal)",
            DepositMode.Max => "(Max)",
            DepositMode.MaxPlus => "(Max+)",
            DepositMode.All => "(All)",
            _ => ""
        };
    }
}