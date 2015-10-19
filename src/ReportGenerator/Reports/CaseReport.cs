using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

namespace ReportGenerator.Reports
{
    internal class CaseReport : ReportBase<CaseReportSettings>
    {
        class Footer : PdfPageEventHelper
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
                var footer = new PdfPTable(3) { TotalWidth = page.Width - (page.BorderWidthLeft + page.BorderWidthRight) };
                var cellHeight = document.BottomMargin;
                footer.SetWidths(new[] { 1f, 1f, 1f });
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

        public CaseReport(CaseReportSettings settings, Document document, PdfWriter writer)
            : base(settings, document, writer)
        {
            HeaderFooterHelper = new Footer(Settings.CaseNumber, Settings.NameTitlePin, Settings.FooterImagePath);
        }

        private class ThumbnailLayoutHandler : IPdfPCellEvent
        {
            private string AttachmentName { get; }

            private WeakReference<PdfWriter> PdfWriterWeakRef { get; }

            public ThumbnailLayoutHandler(string attachmentName, PdfWriter writer)
            {
                AttachmentName = attachmentName;
                PdfWriterWeakRef = new WeakReference<PdfWriter>(writer);
            }

            public void CellLayout(PdfPCell cell, Rectangle position, PdfContentByte[] canvases)
            {
                PdfWriter writer;
                PdfWriterWeakRef.TryGetTarget(out writer);
                if (writer == null) return;
                var annot = PdfAnnotation.CreateLink(writer, position, PdfAnnotation.HIGHLIGHT_NONE,
                    PdfAction.JavaScript(
                        $"this.exportDataObject({{ cName: '{AttachmentName}', nLaunch: 2 }});", writer));
                annot.Border = new PdfBorderArray(0, 0, 0);
                writer.AddAnnotation(annot);
            }
        }

        protected override void CreateTitlePage()
        {
            var titleFont = FontFactory.GetFont("Calibri", 20);
            var caseFont = FontFactory.GetFont("Cambria", 16);
            var font = FontFactory.GetFont("Calibri", 14);

            var outerTable = new PdfPTable(1) {WidthPercentage = 100f};
            var outerTableCell = new PdfPCell
            {
                MinimumHeight = Document.PageSize.Height - (Document.TopMargin + Document.BottomMargin),
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = Rectangle.NO_BORDER
            };

            var table = new PdfPTable(2)
            {
                WidthPercentage = 100,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.DefaultCell.Border = Rectangle.NO_BORDER; // TODO: Doesn't seem to work

            table.AddCell(new PdfPCell());
            var greenLine = new Chunk(new LineSeparator(6f, 100f, new BaseColor(51, 153, 102), Element.ALIGN_CENTER, 0f));
            var greenLineCell = new PdfPCell {VerticalAlignment = Element.ALIGN_MIDDLE};
            greenLineCell.AddElement(greenLine);
            table.AddCell(greenLineCell);

            table.AddCell(new PdfPCell());
            var caseNumberCell = MakeCell(Settings.CaseNumber, caseFont, Element.ALIGN_CENTER, Element.ALIGN_MIDDLE);
            caseNumberCell.FixedHeight = 20f;
            table.AddCell(caseNumberCell);

            table.AddCell(new PdfPCell());
            table.AddCell(greenLineCell);

            table.AddCell(new PdfPCell());
            var reportTitleCell = MakeCell(Settings.ReportTitle, titleFont, Element.ALIGN_CENTER, Element.ALIGN_MIDDLE);
            reportTitleCell.FixedHeight = 40f;
            table.AddCell(reportTitleCell);

            table.AddCell(new PdfPCell());
            table.AddCell(greenLineCell);

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(Settings.Agency, font));

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(Settings.AgencyInfo, font));

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(Settings.UnitName, font));

            table.AddCell(new PdfPCell());
            table.AddCell(greenLineCell);

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(Settings.NameTitlePin, font));

            table.AddCell(new PdfPCell());
            var additionalNames = new Phrase {Font = font};
            for (var i = 0; i < Settings.AdditionalNames.Count; i++)
            {
                var separator = i < Settings.AdditionalNames.Count - 1 ? ", " : "";
                additionalNames.Add($"{Settings.AdditionalNames[i]}{separator}");
            }
            var additionalNamesCell = new PdfPCell {Border = Rectangle.NO_BORDER};
            additionalNamesCell.AddElement(additionalNames);
            table.AddCell(additionalNamesCell);

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(Settings.AdditionalInfo, font));

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(Settings.Date.ToLongDateString(), font));

            FormatTableCells(table, null);

            outerTableCell.AddElement(table);
            outerTable.AddCell(outerTableCell);
            Document.Add(outerTable);
        }

        public override void Generate()
        {
            var font = FontFactory.GetFont("Times New Roman", 10);
            var sectionFont = FontFactory.GetFont("Times New Roman", 16, Font.BOLD);

            base.Generate();

            var table = new AttachmentTable();

            foreach (var attachment in Settings.Attachments)
            {
                string attachmentName = null;
                if (attachment.IsSection)
                {
                    if (table.Rows.Any() && table.Complete)
                    {
                        FormatTableCells(table, 20);
                        Document.Add(table);
                        Document.NewPage();
                        table = new AttachmentTable();
                    }
                    var sectionHeader = MakeCell(attachment.SectionHeader, sectionFont);
                    sectionHeader.Colspan = 2;
                    table.AddCell(sectionHeader);
                }
                if (attachment.HasAttachment)
                {
                    attachmentName = Path.GetFileName(attachment.AttachmentPath);
                    var fileSpec = PdfFileSpecification.FileEmbedded(Writer, attachment.AttachmentPath,
                        attachmentName, null);
                    fileSpec.AddDescription(attachmentName, false);
                    Writer.AddFileAttachment(fileSpec);
                }
                if (attachment.HasThumbnail)
                {
                    var image = Image.GetInstance(attachment.ThumbnailPath, true);
                    var cell = new PdfPCell(image)
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    if (attachment.HasAttachment)
                    {
                        cell.CellEvent = new ThumbnailLayoutHandler(attachmentName, Writer);
                    }
                    if (!attachment.HasDescriptiveText)
                        cell.Colspan = 2;
                    table.AddCell(cell);
                }

                if (!attachment.HasDescriptiveText) continue;

                var description = MakeCell(attachment.DescriptiveText, font);
                description.VerticalAlignment = Element.ALIGN_MIDDLE;
                if (!attachment.HasThumbnail)
                    description.Colspan = 2;

                table.AddCell(description);
            }

            FormatTableCells(table, 20);
            Document.Add(table);
        }
    }
}