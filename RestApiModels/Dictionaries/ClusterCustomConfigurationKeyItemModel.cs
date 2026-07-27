using System.Collections.Generic;

namespace HEAppE.RestApiModels.Dictionaries
{
    /// <summary>
    ///  Cluster custom configuration key item model with scheduler type compatibility flags
    /// </summary>
    public class ClusterCustomConfigurationKeyItemModel : DictionaryItemModel
    {
        /// <summary>
        /// Default configuration value defined in system appsettings when not overridden for a specific cluster
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// List of supported scheduler types matching DictionaryItemModel (Id & Name from SchedulerTypeExt)
        /// </summary>
        public List<DictionaryItemModel> SupportedSchedulerTypes { get; set; } = new();
    }
}
