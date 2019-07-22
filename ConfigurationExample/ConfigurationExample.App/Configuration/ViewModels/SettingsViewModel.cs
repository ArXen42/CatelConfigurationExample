using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Catel.MVVM;
using ConfigurationExample.App.Configuration.Models;
using ConfigurationExample.App.Services.Interfaces;
using JetBrains.Annotations;

// ReSharper disable MemberCanBePrivate.Global

namespace ConfigurationExample.App.Configuration.ViewModels
{
	public class SettingsViewModel : ViewModelBase
	{
		private readonly IApplicationConfigurationProviderService _applicationConfigurationProviderService;

		public SettingsViewModel(IApplicationConfigurationProviderService applicationConfigurationProviderService)
		{
			_applicationConfigurationProviderService = applicationConfigurationProviderService;
		}

		public override String Title => "Settings";

		public ConfigurationModel Configuration    { get; set; }
		public LanguageEntry      SelectedLanguage { get; set; }

		public IReadOnlyCollection<LanguageEntry> AvailableLanguages { get; } = new List<LanguageEntry>
		{
			new LanguageEntry(new CultureInfo("en-US"), "English"),
			new LanguageEntry(new CultureInfo("ru-RU"), "Русский")
		};

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			_applicationConfigurationProviderService.LoadSettingsFromStorage();
			Configuration = _applicationConfigurationProviderService.Configuration;

			SelectedLanguage = AvailableLanguages.First(lang => Equals(lang.CultureInfo, Configuration.Application.PreferredCulture));
		}

		protected override Task<Boolean> SaveAsync()
		{
			_applicationConfigurationProviderService.SaveChanges();

			return base.SaveAsync();
		}

		protected override Task<Boolean> CancelAsync()
		{
			_applicationConfigurationProviderService.LoadSettingsFromStorage();

			return base.CancelAsync();
		}

		[UsedImplicitly]
		private void OnSelectedLanguageChanged()
		{
			Configuration.Application.PreferredCulture = SelectedLanguage.CultureInfo;
		}

		public sealed class LanguageEntry
		{
			public LanguageEntry(CultureInfo cultureInfo, String displayName)
			{
				CultureInfo = cultureInfo;
				DisplayName = displayName;
			}

			public CultureInfo CultureInfo { get; }
			public String      DisplayName { get; }
		}
	}
}