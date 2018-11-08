﻿using System.Collections.Generic;

using Windows.ApplicationModel.DataTransfer;

namespace PDFArchiveApp_v2.Models
{
    public class DragDropCompletedData
    {
        public DataPackageOperation DropResult { get; set; }

        public IReadOnlyList<object> Items { get; set; }
    }
}
