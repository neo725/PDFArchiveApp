using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFArchiveApp.Universal.Model
{
    public class PdfEntryContext : DbContext
    {
        public DbSet<PdfEntry> PdfEntrys { get; set; }

        public DbSet<PdfTag> Tags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite("Filename=PdfArchive.db");
        }
    }
}
