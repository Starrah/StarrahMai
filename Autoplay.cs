using AquaMai.Config.Types;
using AquaMai.Core.Helpers;
using AquaMai.Mods.UX;
using HarmonyLib;
using Manager;
using MelonLoader;
using Monitor;
using Process;

namespace StarrahMai;

public static class Autoplay
{
    public static readonly (KeyCodeOrName, bool) autoplayKey = (KeyCodeOrName.Service, false); // 一键开关Autoplay
    public static readonly (KeyCodeOrName, bool) dontRuinMyAccountTriggerKey = (KeyCodeOrName.Service, true); // 强制防毁号：开启Autoplay后马上关闭（用于手动强制触发DontRuinMyAccount）
    
    public static int deferAutoplay = 0; // 如果TrackStartProcess期间发动了autoplay，则此项为非0，表示应当在GameProcess开始后立即设置autoplay。 1：autoplay，2：强制防毁号

    public static bool ToggleAutoplay()
    {
        var activate = !GameManager.IsAutoPlay();
        GameManager.AutoPlay = activate ? GameManager.AutoPlayMode.Critical : GameManager.AutoPlayMode.None;
        return activate;
    }

    public static void TriggerDontRuinMyAccount()
    {
        DontRuinMyAccount.ignoreScore = true;
        MelonLogger.Msg("[Autoplay] “强制防毁号”功能已触发，本曲成绩将不会被保存。");
    }

    [HarmonyPatch(typeof(GameProcess), "OnUpdate")]
    [HarmonyPostfix]
    public static void GameProcessOnUpdate()
    {
        if (KeyListener.GetKeyDownOrLongPress(autoplayKey.Item1, autoplayKey.Item2) || deferAutoplay == 1)
        {
            var activate = ToggleAutoplay();
            MelonLogger.Msg("[Autoplay] Autoplay已{0}！", activate ? "开启" : "关闭");
        }
        else if (KeyListener.GetKeyDownOrLongPress(dontRuinMyAccountTriggerKey.Item1, dontRuinMyAccountTriggerKey.Item2) || deferAutoplay == 2)
        {
            TriggerDontRuinMyAccount();
        }
        if (deferAutoplay > 0) deferAutoplay = 0; // 重置deferAutoplay状态
    }

    [HarmonyPatch(typeof(TrackStartProcess), "OnUpdate")]
    [HarmonyPostfix]
    public static void TrackStartProcessOnUpdate(TrackStartMonitor[] ____monitors)
    {
        string message = "";
        if (KeyListener.GetKeyDownOrLongPress(autoplayKey.Item1, autoplayKey.Item2))
        {
            deferAutoplay = deferAutoplay == 1 ? 0 : 1;
            if (deferAutoplay > 0) message = "Autoplay将会在游戏开始后被开启。";
            else message = "又按了一次，取消Autoplay的开启。";
        }
        else if (KeyListener.GetKeyDownOrLongPress(dontRuinMyAccountTriggerKey.Item1, dontRuinMyAccountTriggerKey.Item2))
        {
            deferAutoplay = deferAutoplay == 2 ? 0 : 2;
            if (deferAutoplay > 0) message = "“强制防毁号”功能将会在游戏开始后被触发。";
            else message = "又按了一次，取消“强制防毁号”功能的触发。";
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
        deferAutoplay = 0;
    }
}