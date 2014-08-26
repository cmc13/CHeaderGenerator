using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CHeaderGenerator.UI.ViewModel
{
    public class DialogViewModel : ViewModelBase, IDisposable
    {
        private bool? dialogResult = null;
        private bool isDirty = false;

        public DialogViewModel()
        {
            OKCommand = new RelayCommand(() => { OnOKCommand(); DialogResult = true; },
                () => IsDirty);
        }

        public ICommand OKCommand { get; private set; }

        public bool? DialogResult
        {
            get { return this.dialogResult; }
            set
            {
                if (this.dialogResult != value)
                {
                    this.dialogResult = value;
                    base.RaisePropertyChanged("DialogResult");
                }
            }
        }

        public bool IsDirty
        {
            get { return this.isDirty; }
            set
            {
                if (this.isDirty != value)
                {
                    this.isDirty = value;
                    base.RaisePropertyChanged("IsDirty");
                }
            }
        }

        protected virtual void OnOKCommand()
        {
            // Do nothing here, allow classes to override this.
        }

        ~DialogViewModel()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.DialogResult == null)
                    this.DialogResult = true;
            }
        }
    }
}
