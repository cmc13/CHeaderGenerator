using System;

namespace CHeaderGenerator.UI.ViewModel
{
    class ProgressViewModel : DialogViewModel
    {
        #region Private Data Members

        private double minimum = 0.0;
        private double maximum = 100.0;
        private double progressValue = 0.0;
        private string message = string.Empty;
        private string title = "Progress";

        #endregion

        #region Public Property Definitions

        public double Minimum
        {
            get { return this.minimum; }
            set
            {
                if (this.minimum != value)
                {
                    this.minimum = value;
                    base.RaisePropertyChanged("Minimum");
                }
            }
        }

        public double Maximum
        {
            get { return this.maximum; }
            set
            {
                if (this.maximum != value)
                {
                    this.maximum = value;
                    base.RaisePropertyChanged("Maximum");
                }
            }
        }

        public double ProgressValue
        {
            get { return this.progressValue; }
            set
            {
                if (this.progressValue != value)
                {
                    this.progressValue = value;
                    base.RaisePropertyChanged("ProgressValue");
                }
            }
        }

        public string Message
        {
            get { return this.message; }
            set
            {
                if (this.message != value)
                {
                    this.message = value;
                    base.RaisePropertyChanged("Message");
                }
            }
        }

        public string Title
        {
            get { return this.title; }
            set
            {
                if (this.title != value)
                {
                    this.title = value;
                    base.RaisePropertyChanged("Title");
                }
            }
        }

        #endregion
    }
}
