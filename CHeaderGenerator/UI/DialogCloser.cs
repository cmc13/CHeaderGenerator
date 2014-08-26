using System.Windows;

namespace CHeaderGenerator.UI
{
    public static class DialogCloser
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(DialogCloser),
                new PropertyMetadata(DialogResultChanged));

        public static void SetDialogResult(Window target, bool? value)
        {
            target.SetValue(DialogResultProperty, value);
        }

        private static void DialogResultChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window != null)
                window.DialogResult = e.NewValue as bool?;
        }
    }
}
