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

        public ConfigurationView()
        {
            InitializeComponent();

            // Subscribe to the DataContextChanged event
            DataContextChanged += ConfigurationView_DataContextChanged;

            // Subscribe to password changed events
            OpenAiApiKeyBox.PasswordChanged += (s, e) => UpdateApiKey(nameof(ConfigurationViewModel.OpenAiApiKey), OpenAiApiKeyBox.Password);
            AnthropicApiKeyBox.PasswordChanged += (s, e) => UpdateApiKey(nameof(ConfigurationViewModel.AnthropicApiKey), AnthropicApiKeyBox.Password);
            GoogleApiKeyBox.PasswordChanged += (s, e) => UpdateApiKey(nameof(ConfigurationViewModel.GoogleApiKey), GoogleApiKeyBox.Password);
            MetaApiKeyBox.PasswordChanged += (s, e) => UpdateApiKey(nameof(ConfigurationViewModel.MetaApiKey), MetaApiKeyBox.Password);
            OpenRouterApiKeyBox.PasswordChanged += (s, e) => UpdateApiKey(nameof(ConfigurationViewModel.OpenRouterApiKey), OpenRouterApiKeyBox.Password);
            DeepSeekApiKeyBox.PasswordChanged += (s, e) => UpdateApiKey(nameof(ConfigurationViewModel.DeepSeekApiKey), DeepSeekApiKeyBox.Password);
        }

        private void ConfigurationView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ConfigurationViewModel oldViewModel)
            {
                // Unsubscribe from property changed events
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = e.NewValue as ConfigurationViewModel;
            if (_viewModel != null)
            {
                // Subscribe to property changed events
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                // Initialize password boxes
                UpdatePasswordBoxes();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update password boxes when API keys change
            if (e.PropertyName == nameof(ConfigurationViewModel.OpenAiApiKey) ||
                e.PropertyName == nameof(ConfigurationViewModel.AnthropicApiKey) ||
                e.PropertyName == nameof(ConfigurationViewModel.GoogleApiKey) ||
                e.PropertyName == nameof(ConfigurationViewModel.MetaApiKey) ||
                e.PropertyName == nameof(ConfigurationViewModel.OpenRouterApiKey) ||
                e.PropertyName == nameof(ConfigurationViewModel.DeepSeekApiKey) ||
                e.PropertyName == nameof(ConfigurationViewModel.BraveApiKey))
            {
                UpdatePasswordBoxes();
            }
        }

        private void UpdatePasswordBoxes()
        {
            if (_viewModel == null) return;

            // Update password boxes with values from view model
            if (OpenAiApiKeyBox != null)
                OpenAiApiKeyBox.Password = _viewModel.OpenAiApiKey;

            if (AnthropicApiKeyBox != null)
                AnthropicApiKeyBox.Password = _viewModel.AnthropicApiKey;

            if (GoogleApiKeyBox != null)
                GoogleApiKeyBox.Password = _viewModel.GoogleApiKey;

            if (MetaApiKeyBox != null)
                MetaApiKeyBox.Password = _viewModel.MetaApiKey;

            if (OpenRouterApiKeyBox != null)
                OpenRouterApiKeyBox.Password = _viewModel.OpenRouterApiKey;

            if (DeepSeekApiKeyBox != null)
                DeepSeekApiKeyBox.Password = _viewModel.DeepSeekApiKey;
        }

        private void UpdateApiKey(string propertyName, string value)
        {
            if (_viewModel == null) return;

            // Update the view model property based on the property name
            switch (propertyName)
            {
                case nameof(ConfigurationViewModel.OpenAiApiKey):
                    _viewModel.OpenAiApiKey = value;
                    break;
                case nameof(ConfigurationViewModel.AnthropicApiKey):
                    _viewModel.AnthropicApiKey = value;
                    break;
                case nameof(ConfigurationViewModel.GoogleApiKey):
                    _viewModel.GoogleApiKey = value;
                    break;
                case nameof(ConfigurationViewModel.MetaApiKey):
                    _viewModel.MetaApiKey = value;
                    break;
                case nameof(ConfigurationViewModel.OpenRouterApiKey):
                    _viewModel.OpenRouterApiKey = value;
                    break;
                case nameof(ConfigurationViewModel.DeepSeekApiKey):
                    _viewModel.DeepSeekApiKey = value;
                    break;
                case nameof(ConfigurationViewModel.BraveApiKey):
                    _viewModel.BraveApiKey = value;
                    break;
            }
        }
    }
}
