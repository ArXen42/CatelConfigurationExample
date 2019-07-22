using System.Windows;
using Catel.ApiCop;
using Catel.ApiCop.Listeners;
using Catel.IoC;
using Catel.Logging;
using ConfigurationExample.App.Services;
using ConfigurationExample.App.Services.Interfaces;

namespace ConfigurationExample.App
{
	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private static readonly ILog Log = LogManager.GetCurrentClassLogger();

		protected override void OnStartup(StartupEventArgs e)
		{
#if DEBUG
			LogManager.AddDebugListener();
#endif

			Log.Info("Starting application");

			Log.Info("Registering custom types");
			var serviceLocator = this.GetDependencyResolver().Resolve<IServiceLocator>();
			serviceLocator.RegisterType<IApplicationConfigurationProviderService, ApplicationConfigurationProviderService>();

			Log.Info("Calling base.OnStartup");

			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			// Get advisory report in console
			ApiCopManager.AddListener(new ConsoleApiCopListener());
			ApiCopManager.WriteResults();

			base.OnExit(e);
		}
	}
}