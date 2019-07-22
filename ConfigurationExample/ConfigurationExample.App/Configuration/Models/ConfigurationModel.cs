// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ConfigurationExample.App.Configuration.Models
{
	/// <summary>
	///     Represents this application configuration.
	/// </summary>
	public partial class ConfigurationModel : ConfigurationGroupBase
	{
		public ConfigurationModel()
		{
			Application = new ApplicationConfiguration();
			Performance = new PerformanceConfiguration();
		}

		public ApplicationConfiguration Application { get; private set; }
		public PerformanceConfiguration Performance { get; private set; }
	}
}