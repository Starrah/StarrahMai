using AquaMai.Config.Types;
using MelonLoader;

[assembly: MelonInfo(typeof(StarrahMai.Core), "StarrahMai", "0.1.1", "Starrah", "https://github.com/Starrah/StarrahMai")]
[assembly: MelonGame("sega-interactive", "Sinmai")]
[assembly: MelonAdditionalDependencies("AquaMai")] // 确保在AquaMai之后加载（AI说的，不确定有效）
[assembly: MelonOptionalDependencies("ADXHIDIOMod")]

namespace StarrahMai;

public class Core : MelonMod
{
    /* 配置区：改设置的话，都在这里调 */
    public static (KeyCodeOrName, bool) autoplayKey = (KeyCodeOrName.Service, false); // 一键开关Autoplay
    public static (KeyCodeOrName, bool) dontRuinMyAccountTriggerKey = (KeyCodeOrName.Service, true); // 强制防毁号：开启Autoplay后马上关闭（用于手动强制触发DontRuinMyAccount）
    /* 配置区结束 */

    /* 如果想要关闭某个功能，注释掉相应的加载语句即可 */
    public override void OnInitializeMelon()
    {
        loadModule(typeof(MaimollerCoin));
        loadModule(typeof(Autoplay));
        MelonLogger.Msg("已加载");
    }

    private void loadModule(Type type)
    {
        HarmonyInstance.PatchAll(type);
        MelonLogger.Msg($"启用功能：{type.Name}");
    }
}