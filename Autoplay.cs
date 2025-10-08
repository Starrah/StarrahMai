using AquaMai.Config.Types;
using AquaMai.Core.Helpers;
using HarmonyLib;
using Manager;
using MelonLoader;
using MelonLoader.Assertions;
using Monitor;
using Process;

namespace StarrahMai;

public static class Autoplay
{
    public static readonly (KeyCodeOrName, bool) autoplayKey = (KeyCodeOrName.Service, false); // 一键开关Autoplay
    public static readonly (KeyCodeOrName, bool) dontRuinMyAccountTriggerKey = (KeyCodeOrName.Service, true); // 强制防毁号：开启Autoplay后马上关闭（用于手动强制触发DontRuinMyAccount）
    
    public static bool deferAutoplay = false; // 如果TrackStartProcess期间发动了autoplay，则此项为true，表示应当在GameProcess开始后立即设置autoplay。
    private static bool dontRuinMyAccountTriggered = false;

    public static bool ToggleAutoplay()
    {
        var activate = !GameManager.IsAutoPlay();
        GameManager.AutoPlay = activate ? GameManager.AutoPlayMode.Critical : GameManager.AutoPlayMode.None;
        return activate;
    }

    public static bool TriggerDontRuinMyAccount()
    {
        if (GameManager.IsAutoPlay())
        {
            MelonLogger.Msg("[Autoplay] “强制防毁号”键被按下，但是游戏已经处于Autoplay模式了，所以无事发生。");
            return false;
        }
        ToggleAutoplay();
        dontRuinMyAccountTriggered = true;
        MelonLogger.Msg("[Autoplay] “强制防毁号”功能已触发。");
        return true;
    }

    [HarmonyPatch(typeof(GameProcess), "OnUpdate")]
    [HarmonyPostfix]
    public static void GameProcessOnUpdate()
    {
        if (KeyListener.GetKeyDownOrLongPress(autoplayKey.Item1, autoplayKey.Item2) || (deferAutoplay && !dontRuinMyAccountTriggered))
        {
            var activate = ToggleAutoplay();
            MelonLogger.Msg("[Autoplay] Autoplay已{0}！", activate ? "开启" : "关闭");
        }
        else if (KeyListener.GetKeyDownOrLongPress(dontRuinMyAccountTriggerKey.Item1, dontRuinMyAccountTriggerKey.Item2) ||
                 (deferAutoplay && dontRuinMyAccountTriggered))
        {
            TriggerDontRuinMyAccount();
        }
        else if (dontRuinMyAccountTriggered)
        {
            LemonAssert.IsTrue(GameManager.IsAutoPlay(), "[StarrahMai] dontRuinMyAccountTriggered == true 但是游戏未处于autoplay状态！可能是安装了不兼容的Mod导致的！");
            dontRuinMyAccountTriggered = false;
            ToggleAutoplay();
        }
        if (deferAutoplay) deferAutoplay = false; // 重置deferAutoplay状态
    }

    [HarmonyPatch(typeof(TrackStartProcess), "OnUpdate")]
    [HarmonyPostfix]
    public static void TrackStartProcessOnUpdate(TrackStartMonitor[] ____monitors)
    {
        string message = "";
        if (KeyListener.GetKeyDownOrLongPress(autoplayKey.Item1, autoplayKey.Item2))
        {
            deferAutoplay = !deferAutoplay;
            if (deferAutoplay)
            {
                message = "Autoplay将会在游戏开始后被开启。";
            }
            else
            {
                message = "又按了一次，取消Autoplay的开启。";
            }
        }
        else if (KeyListener.GetKeyDownOrLongPress(dontRuinMyAccountTriggerKey.Item1, dontRuinMyAccountTriggerKey.Item2))
        {
            if (!dontRuinMyAccountTriggered)
            {
                deferAutoplay = true;
                dontRuinMyAccountTriggered = true;
                message = "“强制防毁号”功能将会在游戏开始后被触发。";
            }
            else
            {
                message = "又按了一次，取消“强制防毁号”功能的触发。";
            }
        }
        if (message != "")
        {
            Toast.ShowToast(____monitors[0].gameObject, message);
            MelonLogger.Msg("[Autoplay] {0}", message);
        }
    }

    [HarmonyPatch(typeof(TrackStartProcess), "OnStart")]
    [HarmonyPostfix]
    public static void TrackStartProcessOnStart()
    {
        deferAutoplay = false;
    }
}