using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using MelonLoader;

namespace StarrahMai;

/**
 * 纯个人自用，用于使Sinmai在Linux+Wine环境下能够正常运行与游玩的一些Mod
 * 除了我应该不会有什么人这么闲到在Wine下面折腾HDD吧......
 * 不过你即使一般Windows运行也没必要刻意关闭这个模块，这个模块内置了检查当前是否为Wine环境、如果不是的话自动不生效。
 */
public static class LinuxPatch
{
    // 使用 DllImport 声明来自 ntdll.dll 的 wine_get_version 函数
    [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern string wine_get_version();

    public static bool IsRunningOnWine()
    {
        try
        {
            string version = wine_get_version();
#if DEBUG
            MelonLogger.Msg($"[LinuxPatch] Wine Version: {version}");
#endif
            return !string.IsNullOrEmpty(version);
        }
#pragma warning disable CS0168 // 声明了变量，但从未使用过
        catch (Exception e)
#pragma warning restore CS0168 // 声明了变量，但从未使用过
        {
#if DEBUG
            MelonLogger.Msg($"[LinuxPatch] wine_get_version call failed: {e}");
#endif
            return false;
        }
    }

    [HarmonyPrepare]
    public static bool Prepare()
    {
        if (!IsRunningOnWine()) return false;
        MelonLogger.Msg($"启用功能：{MethodBase.GetCurrentMethod().DeclaringType.Name}");

        // 需要在Prepare阶段一次性运行的功能
        DisableAquamaiSoundExclusive();

        return true;
    }

    public static void DisableAquamaiSoundExclusive()
    {
        // 目标field是私有的，因此需要通过反射修改值
        Traverse.Create(typeof(AquaMai.Mods.GameSystem.Sound)).Field("enableExclusive").SetValue(false);
    }
}