using MelonLoader;

[assembly: MelonInfo(typeof(StarrahMai.Core), "StarrahMai", "0.1.3", "Starrah", "https://github.com/Starrah/StarrahMai")]
[assembly: MelonGame("sega-interactive", "Sinmai")]
[assembly: MelonAdditionalDependencies("AquaMai")] // 确保在AquaMai之后加载（AI说的，不确定有效）
[assembly: MelonOptionalDependencies(
    "ADXHIDIOMod",
    "AquaMai.Config", "AquaMai.Core", "AquaMai.Mods" // 仅用于避免MelonLoader加载时报warning 
)]

namespace StarrahMai;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        /* 如果想要关闭某个功能，注释掉相应的加载语句即可 */
        loadModule(typeof(MaimollerCoin));
        loadModule(typeof(Autoplay));
        loadModule(typeof(LinuxPatch), false);
        MelonLogger.Msg("已加载");
    }

    private void loadModule(Type type, bool loadedMsg = true)
    {
        HarmonyInstance.PatchAll(type);
        if (loadedMsg) MelonLogger.Msg($"启用功能：{type.Name}");
    }
}