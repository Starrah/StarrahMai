using System.Reflection;
using AquaMai.Config.Types;
using HarmonyLib;

namespace StarrahMai.AquaMai;

public static class KeyListener
{
    private static MethodInfo _GetKeyDownOrLongPress_Method = AccessTools
        .GetDeclaredMethods(AccessTools.TypeByName("AquaMai.Core.Helpers.KeyListener"))
        .FirstOrDefault(m => m.Name == "GetKeyDownOrLongPress");

    public static bool GetKeyDownOrLongPress(KeyCodeOrName key, bool isLongPress)
    {
        // 这里把key强转为byte是必须的：因为传进来的key的枚举类是定义在我们这个程序集里的，但AquaMai的函数需要的是定义在他的程序集里的，
        // 两者并不是同一个反射类，因此如果直接传的话会报错；但是数字类型是特例，可以在反射调用中与任何类型兼容，系统会自动帮你做强转。
        return (bool)_GetKeyDownOrLongPress_Method.Invoke(null, [(byte)key, isLongPress]);
    }
}