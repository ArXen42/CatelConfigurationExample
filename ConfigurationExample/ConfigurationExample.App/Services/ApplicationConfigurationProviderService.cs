using System;
using Catel.Configuration;
using ConfigurationExample.App.Configuration.Models;
using ConfigurationExample.App.Services.Interfaces;
using ConfigurationExample.App.Utility;
using JetBrains.Annotations;

namespace ConfigurationExample.App.Services
{
	[UsedImplicitly]
	public partial class ApplicationConfigurationProviderService : IApplicationConfigurationProviderService
	{
		private readonly IConfigurationService _configurationService;

		public ApplicationConfigurationProviderService(IConfigurationService configurationService)
		{
			_configurationService = configurationService;
			Configuration         = new ConfigurationModel();

			LoadSettingsFromStorage();
			ApplyMigrations();
		}

		public event TypedEventHandler<IApplicationConfigurationProviderService> ConfigurationSaved;

		public ConfigurationModel Configuration { get; }

		public void LoadSettingsFromStorage()
		{
			Configuration.LoadFromStorage(_configurationService);
		}

		public void SaveChanges()
		{
			Configuration.SaveToStorage(_configurationService);
			ConfigurationSaved?.Invoke(this);
		}

		private void ApplyMigrations()
		{
			var    currentVersion       = typeof(ApplicationConfigurationProviderService).Assembly.GetName().Version;
			String currentVersionString = currentVersion.ToString();
			String storedVersionString  = _configurationService.GetRoamingValue("SolutionVersion", currentVersionString);

			if (storedVersionString == currentVersionString)
				return; //Either migrations were already applied or we are on fresh install

			var storedVersion = new Version(storedVersionString);
			foreach (var migration in _migrations)
			{
				Int32 comparison = migration.Version.CompareTo(storedVersion);
				if (comparison <= 0)
					continue;

				migration.Action.Invoke();
			}
		}
	}
}