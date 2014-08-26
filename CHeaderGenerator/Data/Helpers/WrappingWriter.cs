using System;

namespace CHeaderGenerator.Data.Helpers
{
    class WrappingWriter : IDisposable
    {
        private readonly Action endAction;

        public WrappingWriter(Action beginAction, Action endAction)
        {
            this.endAction = endAction;
            beginAction();
        }

        public void Dispose()
        {
            this.endAction();
        }
    }
}
