using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfigurationExample.App.Services
{
	public partial class ApplicationConfigurationProviderService
	{
		/// <summary>
		///     Migrations collection ordered by version.
		/// </summary>
		private readonly IReadOnlyCollection<Migration> _migrations = new Migration[]
			{
				new Migration(new Version(1, 1, 0),
					() =>
					{
						//...
					})
			}
			.OrderBy(migration => migration.Version)
			.ToArray();

		/// <summary>
		///     Represents migration: action invoked when updating to the specified version.
		/// </summary>
		private class Migration
		{
			public readonly Version Version;
			public readonly Action  Action;

			public Migration(Version version, Action action)
			{
				Version = version;
				Action  = action;
			}
		}
	}
}