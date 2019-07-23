Реализация управления настройками ПО это, вероятно, одна из тех вещей, которую практически в каждом приложении реализуют по своему. Большинство фреймворков и прочих надстроек обычно предоставляют свои средства для сохранения/загрузки значений из какого-либо key-value хранилища параметров.

Тем не менее, в большинстве случаев реализация, конкретного окна настроек и связанных с ним множества вещей оставлена на усмотрение пользователя. В данной заметке хочу поделиться подходом, к которому удалось придти. В моем случае нужно реализовать работу с настройками в MVVM-friendly стиле и с использованием специфики используемого в данном случае фреймворка Catel.

**Disclaimer**: в данной заметке не будет каких-либо технических тонкостей сложнее базовой рефлексии. Это просто описание подхода к решению небольшой проблемы, получившегося у меня за выходные. Захотелось подумать, как можно избавиться от стандартного boilerplate кода и копипасты, связанной с сохранением/загрузкой настроек приложения. Само решение оказалось довольно тривиальным благодаря удобным имеющимся средствам .NET/Catel, но возможно кому-нибудь сэкономит пару часов времени или наведет на полезные мысли. 

<cut/>

<spoiler title="Краткое описание фреймворка Catel">
Как и другие WPF фреймворки (Prism, MVVM Light, Caliburn.Micro и т.д.), Catel предоставляет удобные средства для построения приложений в MVVM стиле.
Главные компоненты:
 - IoC (интегрированный с MVVM компонентами)
 - ModelBase: базовый класс, предоставляющий автоматическую реализацию PropertyChanged (особенно в связке с Catel.Fody), сериализацию и BeginEdit/CancelEdit/EndEdit (классические "применить"/"отмена").
 - ViewModelBase, умеющий привязываться к модели, оборачивая ее свойства.
 - Работа с представлениями (views), которые умеют автоматически создавать и привязываться к ViewModel. Поддерживаются [вложенные контролы](https://docs.catelproject.com/vnext/introduction/mvvm/introduction-to-nested-user-controls-problem/).
</spoiler>

## Требования

Будем исходить того, что от средств конфигурации мы хотим следующее:
 - Доступ к конфигурации в простом структурированном виде. Например
`CultureInfo culture = settings.Application.PreferredCulture;`
`TimeSpan updateRate = settings.Perfomance.UpdateRate;`.
   - Все параметры представлены в виде обычных свойств. Способ их хранения инкапсулирован внутри. Для простых типов все должно происходить автоматически, для более сложных должна быть возможность сконфигурировать сериализацию значения в строку. 
 - Простота и надежность. Не хочется использовать хрупкие инструменты вроде сериализации всей модели настроек целиком или какого-нибудь Entity Framework. На нижнем уровне конфигурация остается простым хранилищем пар "параметр - значение".
 - Возможность отменить внесенные в конфигурацию изменения, например в случае, если пользователь нажал "отмена" в окне настроек.
 - Возможность подписки на обновления конфигурации. Например, мы хотим обновлять язык приложения сразу после того, как конфигурация была изменена.
 - Миграция между версиями приложения. Должна быть возможность задать действия при переходе между версиями приложения (переименовать параметры и т.д.).
 - Минимум boilerplate кода, минимум возможностей для опечаток. В идеале мы просто хотим задать автосвойство и не думать о том, как оно сохранится, под каким строковым ключом и т.д.. Мы не хотим вручную заниматься копированием каждого из свойств во view-model окна настроек, все должно работать автоматически.

## Стандартные средства

Catel предоставляет сервис IConfigurationService, позволяющий сохранять и загружать значения по строковым ключам из локального хранилища (файла на диске в стандартной реализации).

Если мы захотим использовать этот сервис в чистом виде, то придется эти ключи объявлять самостоятельно, например задав такие константы:
```cs
public static class Application
{
	public const String PreferredCulture = "Application.PreferredCulture";
	public static readonly String PreferredCultureDefaultValue = Thread.CurrentThread.CurrentUICulture.ToString();
}
```
Затем мы можем получать эти параметры примерно следующим образом:
```cs
var preferredCulture = new CultureInfo(configurationService.GetRoamingValue(
			Application.PreferredCulture,
			Application.PreferredCultureDefaultValue));
```
Много и нудно писать, легко сделать опечатки, когда настроек много. Кроме того, сервис поддерживает только простые типы, например `CultureInfo` без дополнительных преобразований сохранить не получится.

Для упрощения работы с этим сервисом получилась обертка, состоящая из нескольких компонент.

Полный код примера доступен в [GitHub репозитории](https://github.com/ArXen42/CatelConfigurationExample). Он содержит простейшее приложение с возможностью отредактировать пару параметров в настройках и убедиться, что все работает. С локализацией не стал заморачиваться, параметр "Language" в настройках используется исключительно для демонстрации работы конфигурации. Если интересует, в Catel есть удобные [механизмы локализации](https://docs.catelproject.com/vnext/catel-core/multilingual/), в том числе и на уровне WPF. Если не нравятся ресурсные файлы, можно сделать свою реализацию, работающую с GNU gettext, например.
Для удобства чтения, в примерах кода в тексте этой публикации удалены все xml-doc комментарии.

![](https://habrastorage.org/webt/_9/js/i_/_9jsi_xxzbfegcuhavmkyluowni.png)

## Сервис конфигурации

Сервис, который можно встроить через IoC и иметь доступ к работе с настройками из любой точки приложения.

Основная задача сервиса - предоставлять модель настроек, которая в свою очередь предоставляет простой и структурированный способ доступа к ним.

Кроме модели настроек, сервис также предоставляет возможность отменить или сохранить внесенные в настройки изменения.

Интерфейс:
```cs
public interface IApplicationConfigurationProviderService
{
	event TypedEventHandler<IApplicationConfigurationProviderService> ConfigurationSaved;
	ConfigurationModel Configuration { get; }
	void LoadSettingsFromStorage();
	void SaveChanges();
}
```

Реализация:
```cs
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
```

Реализация тривиальна, содержимое `ConfigurationModel` описано в следующих разделах. Единственное, что вероятно привлекает внимание - метод `ApplyMigrations`.

В новой версии программы может что-то поменяться, например способ хранения какого-то сложного параметра или его название. Если мы не хотим терять наши настройки после каждого обновления, изменяющего существующие параметры, нужен механизм миграций. Метод `ApplyMigrations` реализует очень простую поддержку выполнения каких-либо действий при переходе между версиями.
Если в новой версии приложения что-то поменялось, мы просто добавляем необходимые действия (например сохранение параметра под новым именем) в для новой версии в список миграций, содержащийся в соседнем файле:
```cs
	private readonly IReadOnlyCollection<Migration> _migrations = new Migration[]
		{
			new Migration(new Version(1,1,0),
				() =>
				{
					//...
				})
		}
		.OrderBy(migration => migration.Version)
		.ToArray();

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
```

## Модель настроек

Автоматизация рутинных операций состоит в следующем. Конфигурация описывается как обычная модель (data-object). Catel предоставляет удобный базовый класс `ModelBase`, являющийся ядром всех его MVVM средств, например автоматических binding'ов между всеми тремя компонентами MVVM. В частности, он позволяет легко обращаться к свойствам модели, которые мы хотим сохранять.

Объявив такую модель, мы можем получить ее свойства, сопоставить им строковые ключи, создав их из имен свойств, после чего автоматически загружать и сохранять значения из конфигурации. Иными словами, связать свойства и значения в конфигурации.

### Объявление параметров конфигурации 
Так выглядит корневая модель:
```cs
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
```

`ApplicationConfiguration` и `PerfomanceConfiguration` - подклассы, описывающие свои группы настроек:
```cs
public partial class ConfigurationModel
{
	public class PerformanceConfiguration : ConfigurationGroupBase
	{
		[DefaultValue(10)]
		public Int32 MaxUpdatesPerSecond { get; set; }
	}
}
```

Под капотом это свойство свяжется с параметром `"Performance.MaxUpdatesPerSecond"`, название которого сгенерировано из названия типа `PerformanceConfiguration`.

Нужно заметить, что возможность объявить эти свойства настолько лаконично появилась благодаря использованию [Catel.Fody](https://github.com/Catel/Catel.Fody), плагина к известному .NET кодогенератору [Fody](https://github.com/Fody/Fody). Если по каким-то причинам вы не хотите его использовать, свойства нужно объявлять как обычно, согласно [документации](http://docs.catelproject.com/vnext/catel-core/data-handling/modelbase/) (визуально похоже на DependencyProperty из WPF).

При желании, уровень вложенности можно увеличить.

### Реализация связывания свойств с IConfigurationService

Связывание происходит в базовом классе `ConfigurationGroupBase`, который в свою очередь унаследован от ModelBase. Рассмотрим его содержимое подробнее.

В первую очередь, составляем список свойств, которые мы хотим сохранять:
```cs
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
...
	private class ConfigurationProperty
	{
		public readonly PropertyData PropertyData;
		public readonly String       ConfigurationKey;

		public ConfigurationProperty(PropertyData propertyData, String configurationKey)
		{
			PropertyData     = propertyData;
			ConfigurationKey = configurationKey;
		}
	}
}
```

Здесь мы просто обращаемся к аналогу рефлексии для моделей Catel, получаем свойства (отфильтровав служебные или те, которые мы явно пометили атрибутом `[ExcludeFromBackup]`) и генерируем для них строковые ключи. Свойства, которые сами имеют тип `ConfigurationGroupBase` заносим в отдельный список.

Метод `LoadFromStorage()` записывает в полученные ранее свойства значения из конфигурации или стандартные, если ранее они не сохранялись. Для подгрупп вызываются их `LoadFromStorage()`:
```cs
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

protected virtual void LoadPropertyFromStorage(IConfigurationService configurationService, String configurationKey, PropertyData propertyData)
{
	var objectConverterService = this.GetDependencyResolver().Resolve<IObjectConverterService>();

	Object value = configurationService.GetRoamingValue(configurationKey, propertyData.GetDefaultValue());
	if (value is String stringValue)
		value = objectConverterService.ConvertFromStringToObject(stringValue, propertyData.Type, CultureInfo.InvariantCulture);

	SetValue(propertyData, value);
}
```

Метод `LoadPropertyFromStorage` определяет, как происходит перенос значения из конфигурации в свойство. Он виртуален и может быть переопределен для нетривиальных свойств.
Небольшая особенность внутренней работы сервиса `IConfigurationService`: можно заметить использование `IObjectConverterService`. Он нужен из-за того, что `IConfigurationService.GetValue` в данном случае вызывается с generic параметром типа `Object` и в таком случае он не будет сам преобразовывать загруженные строки в числа, например, поэтому нужно сделать это самим.

Аналогично с сохранением параметров:
```cs
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

protected virtual void SavePropertyToStorage(IConfigurationService configurationService, String configurationKey, PropertyData propertyData)
{
	Object value = GetValue(propertyData);
	configurationService.SetRoamingValue(configurationKey, value);
}
```

Нужно заметить, что внутри модели конфигурации нужно следовать простым соглашениям об именовании для получения единообразных строковых ключей параметров:

 - Типы групп настроек (кроме корневой) являются подклассами "родительской" группы и их имена оканчиваются на Configuration.
 - Для каждого такого типа есть соответствующее ему свойство. Например группа `ApplicationSettings` и свойство `Application`. Название свойства ни на что не влияет, но это наиболее логичный и ожидаемый вариант.

### Настройка сохранения отдельных свойств

Автомагия Catel.Fody и `IConfigurationService` (прямое сохранение значения в `IConfigurationService` и атрибут `[DefaultValue]`) будет работать только для простых типов и константных значений по умолчанию. Для сложных свойств придется расписать немного подлиннее:
```cs
public partial class ConfigurationModel
{
	public class ApplicationConfiguration : ConfigurationGroupBase
	{
		public CultureInfo PreferredCulture { get; set; }

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
```

Теперь мы можем, например, в окне настроек привязаться к любому из свойств модели:
```xml
<TextBox Text="{Binding Configuration.Application.Username}" />
```
Осталось не забыть переопределить операции при закрытии ViewModel окна настроек:
```cs
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
```