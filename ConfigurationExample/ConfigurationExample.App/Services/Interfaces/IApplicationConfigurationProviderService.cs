using ConfigurationExample.App.Configuration.Models;
using ConfigurationExample.App.Utility;

namespace ConfigurationExample.App.Services.Interfaces
{
	/// <summary>
	///     Provides access to application settings and change notifications.
	/// </summary>
	/// <remarks>
	///     Read-only access, only SettingsViewModel writes settings to persistent configuration.
	/// </remarks>
	public interface IApplicationConfigurationProviderService
	{
		/// <summary>
		///     Occurs when configuration is saved to persistent storage.
		/// </summary>
		event TypedEventHandler<IApplicationConfigurationProviderService> ConfigurationSaved;

		/// <summary>
		///     Configuration model.
		/// </summary>
		ConfigurationModel Configuration { get; }

		/// <summary>
		///     Initialize properties of <see cref="Configuration" /> with values from storage or default.
		/// </summary>
		void LoadSettingsFromStorage();

		/// <summary>
		///     Save property values of <see cref="Configuration" /> into storage.
		/// </summary>
		void SaveChanges();
	}
}