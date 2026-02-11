using System.Reflection;
using AquaMai.Config.Types;
using AquaMai.Core.Helpers;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace StarrahMai;

public static class MaimollerCoin
{
    public static readonly KeyCodeOrName CoinKey = KeyCodeOrName.F3; // 把Coin键映射到哪个键

    private static Dictionary<KeyCode, bool> _IOIO; // 在Prepare钩子中设置

    [HarmonyPrepare]
    public static bool Prepare(MethodBase original)
    {
        if (original != null) return true; // 只对类prepare进行处理，如果是具体patch method的prepare，不做处理
        try
        {
            _IOIO = (Dictionary<KeyCode, bool>)AccessTools.Field("ADXHIDIO.ADXController.IOIO:state").GetValue(null);
        } catch
        {
            MelonLogger.Error($"[MaimollerCoin] ADXHIDIOMod.dll未安装，无法启用MaimollerCoin功能。（目前仅支持mml原厂IO Mod，暂时还不支持AquaMai的mml IO）");
            return false;
        }
        MelonLogger.Msg($"启用功能：{MethodBase.GetCurrentMethod().DeclaringType.Name}");
        return true;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(KeyListener), "CheckLongPush")]
    public static void CheckCoinKey(Dictionary<KeyCodeOrName, int> ____keyPressFrames,
        Dictionary<KeyCodeOrName, int> ____keyPressFramesPrev)
    {
        // 在原本的CheckLongPush函数中，____keyPressFramesPrev[F3]已经被设为了上一帧____keyPressFrames[F3]的值，而____keyPressFrames[F3]会被按照物理键盘F3键的状态重设
        // 因此我们要选取的参考value应该是____keyPressFramesPrev[F3]，而非____keyPressFrames[F3]。
        var value = ____keyPressFramesPrev[CoinKey];
        if (_IOIO[KeyCode.CapsLock])
        {
# if DEBUG
            MelonLogger.Msg($"[MaimollerCoin] CheckLongPush {CoinKey} (Coin Key) is push {value}");
# endif
            ____keyPressFrames[CoinKey] = value + 1;
        }
        else
        {
            ____keyPressFrames[CoinKey] = 0;
        }
    }
}