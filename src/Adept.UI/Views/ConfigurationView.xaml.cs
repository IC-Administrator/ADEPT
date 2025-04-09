using Adept.UI.ViewModels;
using System.Windows.Controls;

namespace Adept.UI.Views
{
    /// <summary>
    /// Interaction logic for ConfigurationView.xaml
    /// </summary>
    public partial class ConfigurationView : UserControl
    {
        private ConfigurationViewModel? _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationView"/> class.
        /// </summary>
        public ConfigurationView()
        {
            InitializeComponent();
            DataContextChanged += ConfigurationView_DataContextChanged;
        }

        private void ConfigurationView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ConfigurationViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = e.NewValue as ConfigurationViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // API keys are now handled through TextBox bindings
        }
    }
}
