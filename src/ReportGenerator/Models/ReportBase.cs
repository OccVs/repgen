using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ReportGenerator.Models
{
    internal abstract class ReportBase<T> where T : ReportSettingsBase
    {
        protected T Settings { get; }
        protected Document Document { get; }
        protected PdfWriter Writer { get; }

        protected ReportBase(T settings, Document document, PdfWriter writer)
        {
            Settings = settings;
            Document = document;
            Writer = writer;
        }

        protected abstract void CreateTitlePage();

        public virtual void Generate()
        {
            Writer.PageEvent = new Footer(Settings.CaseNumber, Settings.NameTitlePin, Settings.SignatureFilename);
            if (!Settings.UserDefinedTitlePage)
                CreateTitlePage();
            Document.NewPage();
        }

        // We maybe should move footers into their own classes and inject them if they need to be customized/swapped
        protected class Footer : PdfPageEventHelper
        {
            private string FooterText { get; }

            private string FirstPageFooterText { get; }

            private string FooterImagePath { get; }

            private bool HasFooterImage => !string.IsNullOrEmpty(FooterImagePath);

            public Footer(string firstPageFooter, string footer, string footerImagePath = null)
            {
                FirstPageFooterText = firstPageFooter;
                FooterText = footer;
                FooterImagePath = footerImagePath;
            }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var font = FontFactory.GetFont("Calibri", 8);
                var page = document.PageSize;
                var footer = new PdfPTable(3) {TotalWidth = page.Width - (page.BorderWidthLeft + page.BorderWidthRight)};
                var cellHeight = document.BottomMargin;
                footer.SetWidths(new[] {1f, 1f, 1f});
                var footerTextCell = MakeCell(writer.PageNumber == 1 ? FirstPageFooterText : FooterText, font,
                    Element.ALIGN_CENTER);
                footerTextCell.FixedHeight = cellHeight;
                footer.AddCell(footerTextCell);
                var pageCell = MakeCell($"Page {writer.PageNumber}", font, Element.ALIGN_CENTER);
                pageCell.FixedHeight = cellHeight;
                footer.AddCell(pageCell);
                var footerImageCell = HasFooterImage
                    ? new PdfPCell(Image.GetInstance(FooterImagePath, true))
                    : new PdfPCell();
                footerImageCell.FixedHeight = cellHeight;
                footerImageCell.HorizontalAlignment = Element.ALIGN_CENTER;
                footer.AddCell(footerImageCell);
                FormatTableCells(footer, null);
                footer.WriteSelectedRows(0, -1, page.BorderWidthLeft, footer.TotalHeight, writer.DirectContent);
            }
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