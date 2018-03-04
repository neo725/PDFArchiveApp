using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFArchiveApp.Models.View
{
    public class PageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private double _containerWidth = 0;
        private double _containerHeight = 0;
        private bool _isLoading = false;

        public PageViewModel()
        {
            this.IsLoading = false;
        }

        public double ContainerWidth
        {
            get { return _containerWidth; }
            set { _containerWidth = value; OnPropertyChanged("ContainerWidth"); }
        }

        public double ContainerHeight
        {
            get { return _containerHeight; }
            set { _containerHeight = value; OnPropertyChanged("ContainerHeight"); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged("IsLoading"); }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
