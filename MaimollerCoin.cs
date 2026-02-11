using System.Reflection;
using System.Runtime.InteropServices;
using AquaMai.Config.Types;
using AquaMai.Core.Helpers;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace StarrahMai;

/**
 * 本模块现在同时支持两种功能，可以单独开启，也可分别开启：
 * 1. 将Maimoller的Coin键映射为AquaMai中的某一个键（如F1~F12）。由CoinKey变量控制，默认为F3。
 *    - 这个映射出来的键只能在AquaMai（或与AquaMai兼容、使用AquaMai的按键状态接口的其他Mod，如本Mod的OBSSave功能）中，为相关功能绑定了所映射的按键时生效。
 *    - 例如你可以把练习模式、一键重开等功能的触发快捷键设置为F3，就可以用Maimoller的Coin键控制这些功能了。
 *    - 注：开启本功能后，原本在键盘上的那个按键会失效。
 * 2. 将Maimoller的Coin键，通过操作系统的键盘事件，映射为一个提交给操作系统的按键。由RealKey变量控制，默认不开启；如果需要设为Enter键，请改为13，如需其他的按键请自行查阅标准键盘码值表。
 *    - 这个键会被真实的发送给操作系统，因此可以被所有的程序捕捉到，而不仅限于AquaMai兼容Mod。
 *    - 控制aime模拟刷卡的segatools也可以捕获到本按键，因此可以被用来实现按Maimoller的Coin键完成模拟刷卡。
 */
public static class MaimollerCoin
{
    public static readonly KeyCodeOrName CoinKey = KeyCodeOrName.F3; // 上述的第1种功能下，把Coin键映射到AquaMai内的哪个键。若不希望开启，请设为KeyCodeOrName.None。
    public static readonly ushort RealKey = 0; // 上述的第2种功能下，把Coin键映射到哪个按键。使用标准键盘码值表。若不希望开启，请设为0。PS: Enter键对应的键码为13(0x0D)。
    
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
    
    // 与功能2（发送真实物理按键给键盘）有关的结构体/外部接口等。
    private static bool _prev;
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public int type; public INPUTUNION U; }
    [StructLayout(LayoutKind.Explicit, Size = 32, Pack = 8)] // 重要，以符合Win32对INPUT事件结构体的内存排列要求。
    private struct INPUTUNION { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(KeyListener), "CheckLongPush")]
    public static void CheckCoinKey(Dictionary<KeyCodeOrName, int> ____keyPressFrames,
        Dictionary<KeyCodeOrName, int> ____keyPressFramesPrev)
    {
        var pressed = _IOIO[KeyCode.CapsLock];

        if (CoinKey > 0) // 功能1的实现
        {
            // 在原本的CheckLongPush函数中，____keyPressFramesPrev[F3]已经被设为了上一帧____keyPressFrames[F3]的值，而____keyPressFrames[F3]会被按照物理键盘F3键的状态重设
            // 因此我们要选取的参考value应该是____keyPressFramesPrev[F3]，而非____keyPressFrames[F3]。
            var value = ____keyPressFramesPrev[CoinKey];
            if (pressed)
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

        if (RealKey > 0) // 功能2的实现
        {
            if ((pressed && !_prev) || (!pressed && _prev))
            {
                var eventObj = new INPUT();
                eventObj.type = 1; // INPUT_KEYBOARD
                eventObj.U.ki.wVk = RealKey; // 键码
                if (pressed && !_prev) eventObj.U.ki.dwFlags = 0; // 按下事件的flag
                else if (!pressed && _prev) eventObj.U.ki.dwFlags = 2; // 抬起事件的flag
# if DEBUG
                MelonLogger.Msg($"[MaimollerCoin] RealKey: _prev {_prev} _current {pressed}. Sending Keyboard Event wVk={eventObj.U.ki.wVk} dwFlags={eventObj.U.ki.dwFlags}. Diagnostic: INPUT struct size = { Marshal.SizeOf(typeof(INPUT))}, should be 40.");
# endif
                uint result = SendInput(1, [eventObj], Marshal.SizeOf(typeof(INPUT)));
                if (result == 0)
                {
                    MelonLogger.Warning($"[MaimollerCoin] RealKey: Calling Win32 API: SendInput, FAILED, result={result}, lastError={Marshal.GetLastWin32Error()}");
                }
            }
            _prev = pressed;
        }
    }
}