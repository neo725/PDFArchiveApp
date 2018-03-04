using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace PDFArchiveApp.Standard.Model
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
