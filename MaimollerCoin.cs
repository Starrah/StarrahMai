using System.Reflection;
using AquaMai.Config.Types;
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

    private static Type _keyPressDict_Type =
        AccessTools.Field("AquaMai.Core.Helpers.KeyListener:_keyPressFrames").FieldType;

    private static MethodInfo _keyPressDict_getItem = AccessTools.Method(_keyPressDict_Type, "get_Item");
    private static MethodInfo _keyPressDict_setItem = AccessTools.Method(_keyPressDict_Type, "set_Item");

    [HarmonyPostfix]
    [HarmonyPatch("AquaMai.Core.Helpers.KeyListener", "CheckLongPush")]
    public static void CheckCoinKey(Dictionary<KeyCodeOrName, int> ____keyPressFrames,
        Dictionary<KeyCodeOrName, int> ____keyPressFramesPrev)
    {
        // 由于两个KeyCodeOrName的底层Type不同，直接把CoinKey传进Dict会导致崩溃。
        // 尝试了各种办法，最后发现最简单也最靠谱的是使用反射找到get/set_Item方法然后手动Invoke，这时就可以传数值代替枚举对象了。
        
        // 在原本的CheckLongPush函数中，____keyPressFramesPrev[F3]已经被设为了上一帧____keyPressFrames[F3]的值，而____keyPressFrames[F3]会被按照物理键盘F3键的状态重设
        // 因此我们要选取的参考value应该是____keyPressFramesPrev[F3]，而非____keyPressFrames[F3]。
        int value = (int)_keyPressDict_getItem.Invoke(____keyPressFramesPrev, [(int)CoinKey]);
        if (_IOIO[KeyCode.CapsLock])
        {
# if DEBUG
            MelonLogger.Msg($"[MaimollerCoin] CheckLongPush {CoinKey} (Coin Key) is push {value}");
# endif
            _keyPressDict_setItem.Invoke(____keyPressFrames, [(int)CoinKey, value + 1]);
        }
        else
        {
            _keyPressDict_setItem.Invoke(____keyPressFrames, [(int)CoinKey, 0]);
        }
    }
}