using MelonLoader;

[assembly: MelonInfo(typeof(StarrahMai.Core), "StarrahMai", "1.0.0", "Starrah", "https://github.com/Starrah/StarrahMai")]
[assembly: MelonGame("sega-interactive", "Sinmai")]

namespace DontRuinMyAccount
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }
    }
}