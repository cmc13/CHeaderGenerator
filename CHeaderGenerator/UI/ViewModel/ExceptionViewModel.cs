using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHeaderGenerator.UI.ViewModel
{
    class ExceptionViewModel : DialogViewModel
    {
        #region Private Data Members

        private string message;
        private string caption;
        private Exception exception;

        #endregion

        public ExceptionViewModel()
            : base()
        {
            // Not really a concept of 'dirty'
            IsDirty = true;
        }

        #region Property Definitions

        public string Message
        {
            get { return message; }
            set
            {
                if (message != value)
                {
                    message = value;
                    RaisePropertyChanged("Message");
                }
            }
        }

        public string Caption
        {
            get { return caption; }
            set
            {
                if (caption != value)
                {
                    caption = value;
                    RaisePropertyChanged("Caption");
                }
            }
        }

        public Exception Exception
        {
            get { return exception; }
            set
            {
                if (exception != value)
                {
                    exception = value;
                    RaisePropertyChanged("Exception");
                }
            }
        }

        #endregion
    }
}
