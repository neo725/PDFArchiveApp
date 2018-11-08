using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFArchiveApp.Standard.Model.v2
{
    public class PdfEntryContext : DbContext
    {
        public DbSet<PdfEntry> PdfEntrys { get; set; }

        public DbSet<PdfTag> Tags { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    //base.OnConfiguring(optionsBuilder);
        //    optionsBuilder.UseSqlite("Filename=PdfArchive.db");
        //}
    }
}
