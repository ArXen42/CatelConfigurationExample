using System;
using System.ComponentModel;

namespace ConfigurationExample.App.Configuration.Models
{
	public partial class ConfigurationModel
	{
		public class PerformanceConfiguration : ConfigurationGroupBase
		{
			/// <summary>
			///     Arbitrary test parameter related to performance.
			/// </summary>
			[DefaultValue(10)]
			public Int32 MaxUpdatesPerSecond { get; set; }
		}
	}
}