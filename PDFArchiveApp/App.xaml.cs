using System;

using PDFArchiveApp.Services;
//using PDFArchiveApp.Standard.Model;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using PDFArchiveApp.Standard.Model.v2;
using Windows.UI.Popups;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;

namespace PDFArchiveApp
{
    public partial class App : Application
    {
        private Lazy<ActivationService> _activationService;
                
        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        public App()
        {
            InitializeComponent();

            // Deferred execution until used. Check https://msdn.microsoft.com/library/dd642331(v=vs.110).aspx for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
            
            using (var db = new PdfEntryContext())
            {
                db.Database.Migrate();
            }

            
        }


        protected override async void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);

            var tokenResponse = GoogleOAuthBroker.SavedGoogleAccessToken;
            if (true || tokenResponse == null)
            {
                var dialog = new MessageDialog("尚未設定 Google 雲端硬碟的使用授權，是否要現在設定 ?");
                dialog.Title = Package.Current.DisplayName;
                dialog.Commands.Add(new UICommand { Label = "好，現在設定", Id = 0 });
                dialog.Commands.Add(new UICommand { Label = "稍後自行設定", Id = 1 });
                dialog.CancelCommandIndex = 1;
                dialog.DefaultCommandIndex = 0;

                var result = await dialog.ShowAsync();
                if (Convert.ToInt32(result.Id) == 0)
                {
                    //NavigationService.Navigate(typeof(Views.ShellPage), new { Command = "GoogleDriveSetting" });
                    PublisherService.Current.TriggerItemInvoke(Symbol.Setting);
                }
            }
        }
        
        public void BuildIndex()
        {
            //從App_Data底下讀入Index檔案 , 若沒有會自動建立
            DirectoryInfo dirInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\App_Data");
            FSDirectory dir = FSDirectory.Open(dirInfo);

            // Standard
            IndexWriter iw = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, IndexWriter.MaxFieldLength.UNLIMITED);
            // Pangu
            //IndexWriter iw = new IndexWriter(dir, new Lucene.Net.Analysis.Pan)

            //這裡將會寫進一份文件,而文件包含許多field(屬性),你可以決定這些屬性是否需要被索引
            Document doc = new Document();
            Field field = new Field("ID", "holmes2136", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            Field field2 = new Field("DESC", "但是Holmes是專業PG", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);

            doc.Add(field);
            doc.Add(field2);
            iw.AddDocument(doc);

            iw.Optimize();
            iw.Commit();

            //IndexWriter有實作IDisposable , 
            //表示握有外部資源,所以記的得Close
            //iw.Close();

            iw.Dispose();

        }

        public void Search(string keyWord)
        {

            string indexPath = AppDomain.CurrentDomain.BaseDirectory.ToString() + "\\App_Data\\";
            DirectoryInfo dirInfo = new DirectoryInfo(indexPath);
            FSDirectory dir = FSDirectory.Open(dirInfo);
            IndexSearcher search = new IndexSearcher(dir, true);
            // 針對 DESC 欄位進行搜尋
            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "DESC", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
            // 搜尋的關鍵字
            Query query = parser.Parse(keyWord);
            // 開始搜尋
            var hits = search.Search(query, null, search.MaxDoc).ScoreDocs;

            foreach (var res in hits)
            {
                Console.WriteLine(string.Format("ID:{0} / DESC{1}", search.Doc(res.Doc).Get("ID").ToString()
                    , search.Doc(res.Doc).Get("DESC").ToString().Replace(keyWord, "" + keyWord + "") + ""));
            }


        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                await ActivationService.ActivateAsync(args);
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, typeof(Views.MainPage), new Lazy<UIElement>(CreateShell));
        }

        private UIElement CreateShell()
        {
            return new Views.ShellPage();
        }
    }
}
