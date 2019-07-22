using System.Threading.Tasks;
using Catel.MVVM;
using Catel.Services;
using ConfigurationExample.App.Configuration.ViewModels;

namespace ConfigurationExample.App.Core.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IUIVisualizerService _uiVisualizerService;
		private readonly IViewModelFactory    _viewModelFactory;

		public MainWindowViewModel(IUIVisualizerService uiVisualizerService, IViewModelFactory viewModelFactory)
		{
			_uiVisualizerService = uiVisualizerService;
			_viewModelFactory    = viewModelFactory;
			OpenSettings         = new TaskCommand(Execute);
		}

		public TaskCommand OpenSettings { get; set; }

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();
		}

		protected override async Task CloseAsync()
		{
			await base.CloseAsync();
		}

		private async Task Execute()
		{
			var vm = _viewModelFactory.CreateViewModel<SettingsViewModel>(null);
			await _uiVisualizerService.ShowDialogAsync(vm);
		}
	}
}