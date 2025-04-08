using Microsoft.Extensions.Logging;
using System.Windows;

namespace Adept.UI
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(ILogger<MainWindow> logger)
        {
            _logger = logger;
            InitializeComponent();
            
            _logger.LogInformation("MainWindow initialized");
        }
    }
}
