using System;
using System.Collections.Generic;

namespace PDFArchiveApp.Standard.Model
{
    public class PdfEntry
    {
        public int Id { get; set; }

        public Guid EntryId { get; set; }

        public string SourcePath { get; set; }

        public string Filename { get; set; }

        public DateTime Created { get; set; }

        public List<PdfTag> Tags { get; set; }
    }
}
