using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Adept.UI.Controls
{
    /// <summary>
    /// Interaction logic for ConfirmationDialog.xaml
    /// </summary>
    public partial class ConfirmationDialog : UserControl
    {
        public ConfirmationDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region Properties

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ConfirmationDialog), new PropertyMetadata("Confirm Action"));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(ConfirmationDialog), new PropertyMetadata("Are you sure you want to proceed?"));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty ConfirmButtonTextProperty =
            DependencyProperty.Register("ConfirmButtonText", typeof(string), typeof(ConfirmationDialog), new PropertyMetadata("Confirm"));

        public string ConfirmButtonText
        {
            get { return (string)GetValue(ConfirmButtonTextProperty); }
            set { SetValue(ConfirmButtonTextProperty, value); }
        }

        public static readonly DependencyProperty CancelButtonTextProperty =
            DependencyProperty.Register("CancelButtonText", typeof(string), typeof(ConfirmationDialog), new PropertyMetadata("Cancel"));

        public string CancelButtonText
        {
            get { return (string)GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

        public static readonly DependencyProperty ConfirmCommandProperty =
            DependencyProperty.Register("ConfirmCommand", typeof(ICommand), typeof(ConfirmationDialog), new PropertyMetadata(null));

        public ICommand ConfirmCommand
        {
            get { return (ICommand)GetValue(ConfirmCommandProperty); }
            set { SetValue(ConfirmCommandProperty, value); }
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(ConfirmationDialog), new PropertyMetadata(null));

        public ICommand CancelCommand
        {
            get { return (ICommand)GetValue(CancelCommandProperty); }
            set { SetValue(CancelCommandProperty, value); }
        }

        #endregion
    }
}
