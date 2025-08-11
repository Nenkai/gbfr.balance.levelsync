using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using gbfr.balance.levelsync.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace gbfr.balance.levelsync.Configuration
{
    public class Config : Configurable<Config>
    {
        [DisplayName("Enable Power Adjustment")]
        [Description("Whether to enable level syncing/power adjustment outside quick quest.\n" +
            "NOTE: It will only update on next quests.")]
        [DefaultValue(true)]
        public bool EnableLevelSync { get; set; } = true;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
