using System;
using System.ComponentModel;
using System.Globalization;
using Catel.Configuration;
using Catel.Data;

namespace ConfigurationExample.App.Configuration.Models
{
	public partial class ConfigurationModel
	{
		public class ApplicationConfiguration : ConfigurationGroupBase
		{
			/// <summary>
			///     Preferred UI culture. Affects localization.
			/// </summary>
			public CultureInfo PreferredCulture { get; set; }

			/// <summary>
			///     Username.
			/// </summary>
			[DefaultValue("User")]
			public String Username { get; set; }

			protected override void LoadPropertyFromStorage(IConfigurationService configurationService, String configurationKey, PropertyData propertyData)
			{
				switch (propertyData.Name)
				{
					case nameof(PreferredCulture):
						String preferredCultureDefaultValue = CultureInfo.CurrentUICulture.ToString();
						if (preferredCultureDefaultValue != "en-US" || preferredCultureDefaultValue != "ru-RU")
							preferredCultureDefaultValue = "en-US";

						String value = configurationService.GetRoamingValue(configurationKey, preferredCultureDefaultValue);
						SetValue(propertyData, new CultureInfo(value));
						break;
					default:
						base.LoadPropertyFromStorage(configurationService, configurationKey, propertyData);
						break;
				}
			}

			protected override void SavePropertyToStorage(IConfigurationService configurationService, String configurationKey, PropertyData propertyData)
			{
				switch (propertyData.Name)
				{
					case nameof(PreferredCulture):
						Object value = GetValue(propertyData);
						configurationService.SetRoamingValue(configurationKey, value.ToString());
						break;
					default:
						base.SavePropertyToStorage(configurationService, configurationKey, propertyData);
						break;
				}
			}
		}
	}
}