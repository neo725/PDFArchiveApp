using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PDFArchiveApp
{
    public class Publisher
    {
        ////public event EventHandler InvokeItem;
        //public delegate void InvokeItemHandler(string itemName);

        //public event InvokeItemHandler InvokeItem;

        public event EventHandler<Symbol> InvokeItemEvent;

        public Publisher()
        {

        }

        public void TriggerItemInvoke(Symbol itemName)
        {
            OnInvokeItemEvent(itemName);
        }

        protected virtual void OnInvokeItemEvent(Symbol itemName)
        {
            InvokeItemEvent?.Invoke(this, itemName);
        }
    }

    public static class PublisherService
    {
        private static Publisher _publisher;

        public static Publisher Current {
            get
            {
                if (_publisher == null)
                {
                    _publisher = new Publisher();
                }

                return _publisher;
            }
        }
    }
}
