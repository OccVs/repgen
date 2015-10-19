using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ReportGenerator.Reports
{
    internal abstract class ReportBase<T> where T : ReportSettingsBase
    {
        protected T Settings { get; }
        protected Document Document { get; }
        protected PdfWriter Writer { get; }
        protected IPdfPageEvent HeaderFooterHelper { get; set; }

        protected ReportBase(T settings, Document document, PdfWriter writer)
        {
            Settings = settings;
            Document = document;
            Writer = writer;
        }

        protected abstract void CreateTitlePage();

        public virtual void Generate()
        {
            Document.AddTitle(Settings.ReportTitle);
            if (HeaderFooterHelper != null) Writer.PageEvent = HeaderFooterHelper;
            if (!Settings.UserDefinedTitlePage)
                CreateTitlePage();
            Document.NewPage();
        }

        protected sealed class AttachmentTable : PdfPTable
        {
            public AttachmentTable() : base(2)
            {
                WidthPercentage = 100;
                HorizontalAlignment = Element.ALIGN_CENTER;
                SetWidths(new[] {5.5f, 4.5f});
            }
        }

        protected static PdfPCell MakeCell(string text, Font font, int horizontalAlignment = Element.ALIGN_LEFT,
            int verticalAlignment = Element.ALIGN_UNDEFINED)
        {
            return new PdfPCell(new Phrase(new Chunk(text, font)))
            {
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment
            };
        }

        protected static void FormatTableCells(PdfPTable table, int? padding) => table.Rows.ForEach(r =>
        {
            foreach (var cell in r.GetCells().Where(c => c != null))
            {
                cell.Border = Rectangle.NO_BORDER;
                if (padding.HasValue) cell.PaddingBottom = padding.Value;
            }
        });
    }
}