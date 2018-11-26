using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using Windows.UI;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.UI.Popups;
using PDFArchiveApp.Helpers;
using PDFArchiveApp.Services;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace PDFArchiveApp.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public List<string> TextResultList = new List<string>();

        public List<Image> ImageResultList = new List<Image>();

        public StorageFile SelectedFile { get; set; }

        public Publisher AppPublisher { get; set; }

        public MainPage()
        {
            InitializeComponent();
            InitialLoadingCanvas();

        }

        private void InitialLoadingCanvas()
        {
            Visual hostVisual = ElementCompositionPreview.GetElementVisual(LoadingCanvas1);
            Compositor compositor = hostVisual.Compositor;

            GaussianBlurEffect blurEffect = new GaussianBlurEffect()
            {
                Name = "Blur",
                BlurAmount = 5.0f,
                BorderMode = EffectBorderMode.Hard,
                Optimization = EffectOptimization.Speed,
                Source = new CompositionEffectSourceParameter("Backdrop")
            };

            var effectFactory = compositor.CreateEffectFactory(blurEffect, new[] { "Blur.BlurAmount" });
            var effectBrush = effectFactory.CreateBrush();

            var destinationBrush = compositor.CreateBackdropBrush();
            effectBrush.SetSourceParameter("Backdrop", destinationBrush);
            
            var blurSprite = compositor.CreateSpriteVisual();
            blurSprite.Size = new System.Numerics.Vector2((float)LoadingCanvas1.ActualWidth, (float)LoadingCanvas1.ActualHeight);
            blurSprite.Brush = effectBrush;
            
            ElementCompositionPreview.SetElementChildVisual(LoadingCanvas1, blurSprite);

            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);

            blurSprite.StartAnimation("Size", bindSizeAnimation);
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
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");

            var pdfFile = await picker.PickSingleFileAsync();
            if (pdfFile != null)
            {
                this.SelectedFile = pdfFile;

                this.tbxPath.Text = pdfFile.Path;
                
                try
                {
                    dataContextPage.IsLoading = true;

                    // Load PDF from file.
                    var pdfDoc = await PdfDocument.LoadFromFileAsync(pdfFile);

                    // Get page count from pdf document
                    var pageCount = pdfDoc.PageCount;

                    // Clear flipview items                
                    fvPDF.Items.Clear();

                    // Clear resultList
                    this.TextResultList.Clear();
                    this.ImageResultList.Clear();

                    for (uint i = 0; i < pageCount; i++)
                    {
                        using (var page = pdfDoc.GetPage(i))
                        {
                            var stream = new InMemoryRandomAccessStream();

                            //Default is actual size. Render pdf page to stream
                            await page.RenderToStreamAsync(stream);

                            // Create bitmapImage for Image source
                            var bitmap = new BitmapImage();
                            //Set stream as bitmapImage's source
                            await bitmap.SetSourceAsync(stream);

                            // Create image as FlipView item's source
                            Image img = new Image();
                            img.Source = bitmap;

                            //Add image item to flipview.
                            fvPDF.Items.Add(img);
                            this.ImageResultList.Add(img);
                            
                            // New OcrEngine with default language
                            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                            var decoder = await BitmapDecoder.CreateAsync(stream);
                            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                            // Get recognition result
                            var result = await ocrEngine.RecognizeAsync(softwareBitmap);
                            var text = result.Text;
                            text = Regex.Replace(text, @"\s+", String.Empty);

                            // Add to result list
                            this.TextResultList.Add(text);

                            stream.Dispose();
                            softwareBitmap.Dispose();
                        }
                    }

                    // Show first page recognition result
                    fvPDF_SelectionChanged(null, null);
                }
                catch (Exception ex)
                {
                    fvPDF.Items.Clear();
                }
                finally
                {
                    dataContextPage.IsLoading = false;
                }
            }
            this.btnSelect.IsEnabled = true;
        }

        private void Page_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            dataContextPage.ContainerWidth = this.ActualWidth;
            dataContextPage.ContainerHeight = this.ActualHeight;
        }

        private void fvPDF_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = fvPDF.SelectedIndex;
            if (this.TextResultList.Count > 0 && this.TextResultList.Count > index)
            {
                this.tbText.Text = this.TextResultList[index];
            }
        }

        private async void btnSave_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (this.SelectedFile == null)
            {
                var dialogNotReady = new MessageDialog("尚未選擇 PDF 檔案");

                dialogNotReady.Commands.Add(new UICommand { Label = "好", Id = 0 });
                dialogNotReady.DefaultCommandIndex = 0;

                await dialogNotReady.ShowAsync();
                return;
            }

            var tokenResponse = GoogleOAuthBroker.SavedGoogleAccessToken;
            if (true || tokenResponse == null)
            {
                var dialogGoogleDrive = new MessageDialog("尚未設定 Google 雲端硬碟的使用授權，是否要現在設定 ?");
                dialogGoogleDrive.Title = Package.Current.DisplayName;
                dialogGoogleDrive.Commands.Add(new UICommand { Label = "好，現在設定", Id = 0 });
                dialogGoogleDrive.Commands.Add(new UICommand { Label = "否，取消儲存", Id = 1 });
                dialogGoogleDrive.CancelCommandIndex = 1;
                dialogGoogleDrive.DefaultCommandIndex = 0;

                var resultGoogleDrive = await dialogGoogleDrive.ShowAsync();

                if (Convert.ToInt32(resultGoogleDrive.Id) == 0)
                {
                    //this.AppPublisher?.TriggerItemInvoke(Symbol.Setting);
                    PublisherService.Current.TriggerItemInvoke(Symbol.Setting);
                }
            }

            // data struct
            // pdf
            // thumbnail
            // text
            // all combine into zip

                var pdfFile = this.SelectedFile;

            // Load PDF from file.
            var pdfDoc = await PdfDocument.LoadFromFileAsync(pdfFile);

            var pageCount = pdfDoc.PageCount;


            var dialog = new MessageDialog("確定要儲存 ?");

            dialog.Commands.Add(new UICommand { Label = "好", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = "取消", Id = 1 });
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            var result = await dialog.ShowAsync();
            if (Convert.ToInt32(result.Id) == 1) return;


        }

        // 修改 OCR 的文字，即時做儲存
        private void TbText_TextChanged(object sender, TextChangedEventArgs e)
        {
            int index = fvPDF.SelectedIndex;
            if (this.TextResultList.Count > 0 && this.TextResultList.Count > index)
            {
                this.TextResultList[index] = this.tbText.Text.Trim();

                //Singleton<ToastNotificationsService>.Instance.ShowToastNotificationSample();
            }
        }
    }
}
