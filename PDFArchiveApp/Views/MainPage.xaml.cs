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

namespace PDFArchiveApp.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private List<string> _resultList = new List<string>();

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
                    _resultList.Clear();

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
                            
                            // New OcrEngine with default language
                            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                            var decoder = await BitmapDecoder.CreateAsync(stream);
                            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                            // Get recognition result
                            var result = await ocrEngine.RecognizeAsync(softwareBitmap);
                            var text = result.Text;
                            text = Regex.Replace(text, @"\s+", String.Empty);
                            // Add to result list
                            _resultList.Add(text);

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
            if (_resultList.Count > 0 && _resultList.Count > index)
            {
                this.tbText.Text = _resultList[index];
            }
        }
    }
}
