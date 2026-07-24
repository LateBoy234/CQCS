using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace ThreeNH.ColorQualityControl.Reports
{
    class PrintDocumentPaginator : DocumentPaginator
    {
        public ReportSetting ReportSetting
        {
            get;set;
        }

        public override bool IsPageCountValid
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int PageCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Size PageSize
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override IDocumentPaginatorSource Source
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            throw new NotImplementedException();
        }
    }
}
