using MelonLoader;

[assembly: MelonInfo(typeof(DontRuinMyAccount.Core), "DontRuinMyAccount", "1.0.0", "panda2134", null)]
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