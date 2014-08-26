using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CHeaderGenerator.UI.View
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        #region NativeMethods Class Definition

        private static class NativeMethods
        {
            public const int GWL_STYLE = -16;
            public const int WS_SYSMENU = 0x80000;

            [DllImport("user32.dll", SetLastError = true)]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        }

        #endregion

        public ProgressDialog()
        {
            InitializeComponent();
            Loaded += ProgressDialog_Loaded;
        }

        private void ProgressDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int winLong = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, winLong & ~NativeMethods.WS_SYSMENU);
        }
    }
}
