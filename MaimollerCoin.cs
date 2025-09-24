using AquaMai.Config.Types;
using HarmonyLib;
using UnityEngine;

namespace StarrahMai;

public static class MaimollerCoin
{
    private static Dictionary<KeyCode, bool> _IOIO =
        (Dictionary<KeyCode, bool>)AccessTools.Field("ADXHIDIO.ADXController.IOIO:state").GetValue(null);

    [HarmonyPrefix]
    [HarmonyPatch("AquaMai.Core.Helpers.KeyListener", "GetKeyPush")]
    public static bool GetKeyPush(KeyCodeOrName key, ref bool __result)
    {
        if (key != KeyCodeOrName.F3) return true;
        __result = _IOIO[KeyCode.CapsLock];
        return false;
    }
}