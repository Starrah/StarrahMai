using AquaMai.Config.Types;
using AquaMai.Core.Helpers;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace StarrahMai;

public static class MaimollerCoin
{
    public static readonly KeyCodeOrName CoinKey = KeyCodeOrName.F3; // 把Coin键映射到哪个键

    // 如果没装ADXHIDIO Mod。则下面这行初始化（static构造函数中）会抛异常，从而整个类不会被加载，钩子也不会打上。不会报错崩溃。没啥问题，就先这样吧。
    private static Dictionary<KeyCode, bool> _IOIO =
        (Dictionary<KeyCode, bool>)AccessTools.Field("ADXHIDIO.ADXController.IOIO:state").GetValue(null);

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