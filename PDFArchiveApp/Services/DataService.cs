using PDFArchiveApp.Standard.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFArchiveApp.Services
{
    public static class DataService
    {
        private static PdfEntryContext db = new PdfEntryContext();

        private static IEnumerable<PdfEntry> AllEntrys()
        {
            var data = new ObservableCollection<PdfEntry>();

            foreach (var entry in db.PdfEntrys.ToList()) {
                data.Add(entry);
            }
            
            return data;
        }

        public static ObservableCollection<PdfEntry> GetEntrysData()
        {
            return new ObservableCollection<PdfEntry>(AllEntrys());
        }
    }
}
