using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Catel.Configuration;
using Catel.Data;
using Catel.IoC;
using Catel.Services;
using Serilog;

namespace ConfigurationExample.App.Configuration.Models
{
	/// <summary>
	///     Represents configuration group model that can be stored and loaded from configuration storage provided by <see cref="IConfigurationService" />.
	/// </summary>
	/// <remarks>
	///     Properties that represent configuration parameters should be usual Catel properties and included in backup (default behaviour).
	///     Use Catel.Fody to make it automatic and simple.
	///     Properties of type derived from <see cref="ConfigurationGroupBase" /> will treated as configuration subgroups.
	/// </remarks>
	public abstract class ConfigurationGroupBase : ModelBase
	{
		private readonly IReadOnlyCollection<ConfigurationProperty> _configurationProperties;
		private readonly IReadOnlyCollection<PropertyData>          _nestedConfigurationGroups;

		protected ConfigurationGroupBase()
		{
			var properties = this.GetDependencyResolver()
				.Resolve<PropertyDataManager>()
				.GetCatelTypeInfo(GetType())
				.GetCatelProperties()
				.Select(property => property.Value)
				.Where(property => property.IncludeInBackup && !property.IsModelBaseProperty)
				.ToArray();

			_configurationProperties = properties
				.Where(property => !property.Type.IsSubclassOf(typeof(ConfigurationGroupBase)))
				.Select(property =>
				{
					// ReSharper disable once PossibleNullReferenceException
					String configurationKeyBase = GetType()
						.FullName
						.Replace("+",                                       ".")
						.Replace(typeof(ConfigurationModel).FullName + ".", string.Empty);

					configurationKeyBase = configurationKeyBase.Remove(configurationKeyBase.Length - "Configuration".Length);

					String configurationKey = $"{configurationKeyBase}.{property.Name}";
					return new ConfigurationProperty(property, configurationKey);
				})
				.ToArray();

			_nestedConfigurationGroups = properties
				.Where(property => property.Type.IsSubclassOf(typeof(ConfigurationGroupBase)))
				.ToArray();
		}

		/// <summary>
		///     Initialize this model properties with values from storage or default.
		/// </summary>
		public void LoadFromStorage(IConfigurationService configurationService)
		{
			foreach (var property in _configurationProperties)
			{
				try
				{
					LoadPropertyFromStorage(configurationService, property.ConfigurationKey, property.PropertyData);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Can't load from storage nested configuration group {Name}", property.PropertyData.Name);
				}
			}

			foreach (var property in _nestedConfigurationGroups)
			{
				var configurationGroup = GetValue(property) as ConfigurationGroupBase;
				if (configurationGroup == null)
				{
					Log.Error("Can't load from storage configuration property {Name}", property.Name);
					continue;
				}

				configurationGroup.LoadFromStorage(configurationService);
			}
		}

		/// <summary>
		///     Save this model property values into storage.
		/// </summary>
		public void SaveToStorage(IConfigurationService configurationService)
		{
			foreach (var property in _configurationProperties)
			{
				try
				{
					SavePropertyToStorage(configurationService, property.ConfigurationKey, property.PropertyData);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Can't save to storage configuration property {Name}", property.PropertyData.Name);
				}
			}

			foreach (var property in _nestedConfigurationGroups)
			{
				var configurationGroup = GetValue(property) as ConfigurationGroupBase;
				if (configurationGroup == null)
				{
					Log.Error("Can't save to storage nested configuration group {Name}", property.Name);
					continue;
				}

				configurationGroup.SaveToStorage(configurationService);
			}
		}

		/// <summary>
		///     Load specific property from storage.
		/// </summary>
		/// <remarks>
		///     Override if you need to manually control the way some properties are loaded from storage.
		/// </remarks>
		protected virtual void LoadPropertyFromStorage(IConfigurationService configurationService, String configurationKey, PropertyData propertyData)
		{
			var objectConverterService = this.GetDependencyResolver().Resolve<IObjectConverterService>();

			Object value = configurationService.GetRoamingValue(configurationKey, propertyData.GetDefaultValue());
			if (value is String stringValue)
				value = objectConverterService.ConvertFromStringToObject(stringValue, propertyData.Type, CultureInfo.InvariantCulture);

			SetValue(propertyData, value);
		}

		/// <summary>
		///     Save specific property to storage.
		/// </summary>
		/// <remarks>
		///     Override if you need to manually control the way some properties are saved to storage.
		/// </remarks>
		protected virtual void SavePropertyToStorage(IConfigurationService configurationService, String configurationKey, PropertyData propertyData)
		{
			Object value = GetValue(propertyData);
			configurationService.SetRoamingValue(configurationKey, value);
		}

		/// <summary>
		///     Represents configuration property.
		/// </summary>
		private class ConfigurationProperty
		{
			/// <summary>
			///     Catel property data.
			/// </summary>
			public readonly PropertyData PropertyData;

			/// <summary>
			///     Configuration key.
			/// </summary>
			public readonly String ConfigurationKey;

			public ConfigurationProperty(PropertyData propertyData, String configurationKey)
			{
				PropertyData     = propertyData;
				ConfigurationKey = configurationKey;
			}
		}
	}
}