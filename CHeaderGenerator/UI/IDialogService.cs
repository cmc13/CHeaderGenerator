using System;
namespace CHeaderGenerator.UI
{
    interface IDialogService
    {
        bool? ShowDialog<TDialog>(object viewModel) where TDialog : System.Windows.Window;
        void ShowErrorDialog(string message, string caption);
        void ShowExceptionDialog(string message, string caption, Exception ex);
        void ShowMessageDialog(string message, string caption);
        void ShowWindow<TWindow>(object viewModel) where TWindow : System.Windows.Window;
        System.Windows.MessageBoxResult ShowYesNoCancelDialog(string message, string caption);
    }
}
