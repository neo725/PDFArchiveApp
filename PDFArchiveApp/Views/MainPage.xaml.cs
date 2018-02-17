using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace PDFArchiveApp.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async void btnSelect_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.btnSelect.IsEnabled = false;
            // Pick pdf file by user
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            StorageFile pdfFile = await picker.PickSingleFileAsync();
            if (pdfFile != null)
            {
                this.tbxPath.Text = pdfFile.Path;
            }
            this.btnSelect.IsEnabled = true;
        }
    }
}
