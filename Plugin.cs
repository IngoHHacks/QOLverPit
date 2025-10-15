using BepInEx.Configuration;
using CloverAPI.Content.Data;
using CloverAPI.Utils;
using Panik;
using QOLverPit.Patches;
using System;
using System.IO;

namespace QOLverPit
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [HarmonyPatch]
    [BepInDependency("ModdingAPIs.cloverpit.CloverAPI")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "IngoH.cloverpit.QOLverPit";
        public const string PluginName = "QOLverPit";
        public const string PluginVer = "1.0.0";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;
        internal const string MainContentFolder = "QOLverPit_Content";
        public static string DataPath { get; private set; }
        public static string ImagePath { get; private set; }
        
        internal static ConfigEntry<Controls.KeyboardElement> ChangeDepositModeKeyboardKey;
        internal static ConfigEntry<Controls.MouseElement> ChangeDepositModeMouseButton;
        internal static ConfigEntry<Controls.JoystickElement> ChangeDepositModeControllerButton;
        internal static ConfigEntry<Controls.KeyboardElement> ChangeDepositModeReverseKeyboardKey;
        internal static ConfigEntry<Controls.MouseElement> ChangeDepositModeReverseMouseButton;
        internal static ConfigEntry<Controls.JoystickElement> ChangeDepositModeReverseControllerButton;
        internal static ConfigEntry<bool> AllowMaxPlus;
        
        internal static ConfigEntry<float> TransitionSpeedMultiplier;
        internal static ConfigEntry<float> GameSpeedMultiplier;
        
        internal static ConfigEntry<bool> SkipSplashScreens;

        internal static bool Ready => GameUtils.GameReady;
        
        private void Awake()
        {
            Log = Logger;
            PluginPath = Path.GetDirectoryName(Info.Location);
            DataPath = Path.Combine(PluginPath, MainContentFolder, "Data");
            ImagePath = Path.Combine(PluginPath, MainContentFolder, "Images");

            MakeConfig();

            Time.timeScale = GameSpeedMultiplier.Value;
        }
        
        private void MakeConfig()
        {
            ChangeDepositModeKeyboardKey =
                Config.Bind("Controls", "Keyboard: Change Deposit Mode", Controls.KeyboardElement.Q, new ConfigDescription("Key to change the deposit amount mode. Set to None to disable."));
            ChangeDepositModeMouseButton =
                Config.Bind("Controls", "Mouse: Change Deposit Mode", Controls.MouseElement.axisScrollWheelVertical, new ConfigDescription("Mouse button to change the deposit amount mode. Set to Undefined to disable. Setting it to scroll wheel makes it work in both directions."));
            ChangeDepositModeControllerButton =
                Config.Bind("Controls", "Controller: Change Deposit Mode", Controls.JoystickElement.LeftShoulder, new ConfigDescription("Button to change the deposit amount mode. Set to Undefined to disable. Setting it to a stick makes it work in both directions."));
            ChangeDepositModeReverseKeyboardKey =
                Config.Bind("Controls", "Keyboard: Change Deposit Mode (Reverse)", Controls.KeyboardElement.None, new ConfigDescription("Key to change the deposit amount mode in reverse order. Set to None to disable."));
            ChangeDepositModeReverseMouseButton =
                Config.Bind("Controls", "Mouse: Change Deposit Mode (Reverse)", Controls.MouseElement.Undefined, new ConfigDescription("Mouse button to change the deposit amount mode in reverse order. Set to Undefined to disable. Setting it to scroll wheel makes it work in both directions."));
            ChangeDepositModeReverseControllerButton =
                Config.Bind("Controls", "Controller: Change Deposit Mode (Reverse)", Controls.JoystickElement.RightShoulder, new ConfigDescription("Button to change the deposit amount mode in reverse order. Set to Undefined to disable. Setting it to a stick makes it work in both directions."));
            AllowMaxPlus =
                Config.Bind("Gameplay", "Allow Max Plus Deposit Mode", false, new ConfigDescription("If true, the Max Plus deposit mode will be available when changing deposit modes. This mode deposits the maximum amount of coins up to one coin less than the remaining debt. This isn't normally possible due to deposits being in steps of 5%, so this may be considered cheating."));
            TransitionSpeedMultiplier =
                Config.Bind("Gameplay", "Transition Speed Multiplier", 1f, new ConfigDescription("How much faster transitions should be. Overrides the in-game setting if not 1. Must be greater than 0.", new AcceptableValueRange<float>(0.01f, 1000f)));
            GameSpeedMultiplier =
                Config.Bind("Gameplay", "Game Speed Multiplier", 1f, new ConfigDescription("How much faster the game should be. Beware! This can cause physics issues if set too high. Must be greater than 0.", new AcceptableValueRange<float>(0.01f, 100f)));
            SkipSplashScreens =
                Config.Bind("Gameplay", "Skip Splash Screens", false, new ConfigDescription("If true, the splash screens will be skipped on game start. Won't apply on first launch before language selection."));

            GameSpeedMultiplier.SettingChanged += (_, _) =>
            {
                Time.timeScale = GameSpeedMultiplier.Value;
            };
            
            ModSettingsManager.RegisterPageFromConfig(this, "QOLverPit");
        }


        private void OnEnable()
        {
            Harmony.PatchAll();
            LogInfo($"Loaded {PluginName}!");
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
            LogInfo($"Unloaded {PluginName}!");
        }

        private void Update()
        {
            if (!Ready)
            {
                return;
            }
            
            mouseAxisDelay -= Time.unscaledDeltaTime;
            controllerAxisDelay -= Time.unscaledDeltaTime;
            if (KeyboardFwd() || MouseFwd() || ControllerFwd())
            {
                DepositPatch.CycleDepositMode();
            }
            else if (KeyboardRev() || MouseRev() || ControllerRev())
            {
                DepositPatch.CycleDepositMode(true);
            }
        }

        private bool KeyboardFwd()
        {
            return Controls.KeyboardButton_PressedGet(0, ChangeDepositModeKeyboardKey.Value);
        }
        
        private bool KeyboardRev()
        {
            return Controls.KeyboardButton_PressedGet(0, ChangeDepositModeReverseKeyboardKey.Value);
        }
        
        private float mouseAxisDelay = 0f;
        private bool MouseFwd()
        {
            if (Controls.MouseElement_IsAxis(ChangeDepositModeMouseButton.Value) && Controls.MouseAxis_ValueGet(0, ChangeDepositModeMouseButton.Value) > 0 ||
                Controls.MouseElement_IsAxis(ChangeDepositModeReverseMouseButton.Value) && Controls.MouseAxis_ValueGet(0, ChangeDepositModeReverseMouseButton.Value) < 0)
            {
                if (mouseAxisDelay <= 0f)
                {
                    mouseAxisDelay = 0.05f; // Delay to prevent multiple triggers
                    return true;
                }
            }
            return Controls.MouseElement_IsButton(ChangeDepositModeMouseButton.Value) && Controls.MouseButton_PressedGet(0, ChangeDepositModeMouseButton.Value);
        }
        
        private bool MouseRev()
        {
            if (Controls.MouseElement_IsAxis(ChangeDepositModeMouseButton.Value) && Controls.MouseAxis_ValueGet(0, ChangeDepositModeMouseButton.Value) < 0 ||
                Controls.MouseElement_IsAxis(ChangeDepositModeReverseMouseButton.Value) && Controls.MouseAxis_ValueGet(0, ChangeDepositModeReverseMouseButton.Value) > 0)
            {
                if (mouseAxisDelay <= 0f)
                {
                    mouseAxisDelay = 0.05f; // Delay to prevent multiple triggers
                    return true;
                }
            }
            return Controls.MouseElement_IsButton(ChangeDepositModeReverseMouseButton.Value) && Controls.MouseButton_PressedGet(0, ChangeDepositModeReverseMouseButton.Value);
        }
        
        private float controllerAxisDelay = 0f;
        
        private bool ControllerFwd()
        {
            if (Controls.JoystickElement_IsAxis(ChangeDepositModeControllerButton.Value) && Math.Abs(Controls.JoystickAxis_ValueGet(0, ChangeDepositModeControllerButton.Value)) > 0.5f ||
                Controls.JoystickElement_IsAxis(ChangeDepositModeReverseControllerButton.Value) && Math.Abs(Controls.JoystickAxis_ValueGet(0, ChangeDepositModeReverseControllerButton.Value)) > 0.5f)
            {
                if (controllerAxisDelay <= 0f)
                {
                    controllerAxisDelay = 0.2f; // Delay to prevent multiple triggers
                    return true;
                }
            }
            return Controls.JoystickButton_PressedGet(0, ChangeDepositModeControllerButton.Value);
        }
        
        private bool ControllerRev()
        {
            if (Controls.JoystickElement_IsAxis(ChangeDepositModeControllerButton.Value) && Math.Abs(Controls.JoystickAxis_ValueGet(0, ChangeDepositModeControllerButton.Value)) > 0.5f ||
                Controls.JoystickElement_IsAxis(ChangeDepositModeReverseControllerButton.Value) && Math.Abs(Controls.JoystickAxis_ValueGet(0, ChangeDepositModeReverseControllerButton.Value)) > 0.5f)
            {
                if (controllerAxisDelay <= 0f)
                {
                    controllerAxisDelay = 0.2f; // Delay to prevent multiple triggers
                    return true;
                }
            }
            return Controls.JoystickButton_PressedGet(0, ChangeDepositModeReverseControllerButton.Value);
        }
    }
}