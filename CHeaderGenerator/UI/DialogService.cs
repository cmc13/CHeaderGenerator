using CHeaderGenerator.UI.View;
using CHeaderGenerator.UI.ViewModel;
using System;
using System.ComponentModel.Composition;
using System.Windows;

namespace CHeaderGenerator.UI
{
    [Export(typeof(IDialogService))]
    public class DialogService : CHeaderGenerator.UI.IDialogService
    {
        public void ShowExceptionDialog(string message, string caption, Exception ex)
        {
            var agEx = ex as AggregateException;

            ShowDialog<ExceptionDialog>(new ExceptionViewModel
            {
                Message = message,
                Caption = caption,
                Exception = (agEx != null && agEx.InnerExceptions.Count == 1) ? agEx.InnerExceptions[0] : ex
            });
        }

        public void ShowMessageDialog(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public MessageBoxResult ShowYesNoCancelDialog(string message, string caption)
        {
            return MessageBox.Show(message, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        }

        public void ShowErrorDialog(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool? ShowDialog<TDialog>(object viewModel) where TDialog : Window
        {
            var dlg = Activator.CreateInstance(typeof(TDialog)) as TDialog;
            dlg.DataContext = viewModel;
            return dlg.ShowDialog();
        }

        public void ShowWindow<TWindow>(object viewModel) where TWindow : Window
        {
            var win = Activator.CreateInstance(typeof(TWindow)) as TWindow;
            win.DataContext = viewModel;
            win.Show();
        }
    }
}
