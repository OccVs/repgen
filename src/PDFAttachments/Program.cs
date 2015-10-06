using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Newtonsoft.Json;
using PDFAttachments.Models;

namespace PDFAttachments
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("No settings JSON file specified");
                Environment.Exit(1);
            }
            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("File not found");
                Environment.Exit(1);
            }

            PdfSettings settings = null;

            try
            {
                settings = JsonConvert.DeserializeObject<PdfSettings>(File.ReadAllText(args[0]));
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Error parsing settings JSON file");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(settings.Filename))
            {
                Console.Error.WriteLine("No output filename found in settings JSON file");
                Environment.Exit(1);
            }

            if (settings.Attachments == null || !settings.Attachments.Any())
            {
                Console.Error.WriteLine("No attachments found in settings JSON file");
                Environment.Exit(1);
            }

            try
            {
                WritePdf(settings);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error generating PDF file");
                Console.Error.WriteLine(ex);
                Environment.Exit(1);
            }
        }

        private static void RemoveTableBorders(PdfPTable table) => table.Rows.ForEach(r =>
        {
            foreach (var cell in r.GetCells().Where(c => c != null))
                cell.Border = Rectangle.NO_BORDER;
        });

        private static PdfPCell MakeCell(string text, Font font, int horizontalAlignment = Element.ALIGN_LEFT,
            int verticalAlignment = Element.ALIGN_UNDEFINED)
        {
            return new PdfPCell(new Phrase(new Chunk(text, font)))
            {
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment
            };
        }

        static void CreateTitlePage(Document doc, PdfSettings settings)
        {
            var titleFont = FontFactory.GetFont("Calibri", 20);
            var caseFont = FontFactory.GetFont("Cambria", 16);
            var font = FontFactory.GetFont("Calibri", 14);

            doc.AddTitle(settings.ReportTitle);
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
            var caseNumberCell = MakeCell(settings.CaseNumber, caseFont, Element.ALIGN_CENTER, Element.ALIGN_MIDDLE);
            caseNumberCell.FixedHeight = 20f;
            table.AddCell(caseNumberCell);

            table.AddCell(new PdfPCell());
            table.AddCell(greenLineCell);

            table.AddCell(new PdfPCell());
            var reportTitleCell = MakeCell(settings.ReportTitle, titleFont, Element.ALIGN_CENTER, Element.ALIGN_MIDDLE);
            reportTitleCell.FixedHeight = 40f;
            table.AddCell(reportTitleCell);

            table.AddCell(new PdfPCell());
            table.AddCell(greenLineCell);

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(settings.Agency, font));

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(settings.AgencyInfo, font));

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(settings.UnitName, font));

            table.AddCell(new PdfPCell());
            table.AddCell(greenLineCell);

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(settings.NameTitlePin, font));

            table.AddCell(new PdfPCell());
            var additionalNames = new Phrase {Font = font};
            for (var i = 0; i < settings.AdditionalNames.Count; i++)
            {
                var separator = i < settings.AdditionalNames.Count - 1 ? ", " : "";
                additionalNames.Add($"{settings.AdditionalNames[i]}{separator}");
            }
            var additionalNamesCell = new PdfPCell {Border = Rectangle.NO_BORDER};
            additionalNamesCell.AddElement(additionalNames);
            table.AddCell(additionalNamesCell);

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(settings.AdditionalInfo, font));

            table.AddCell(new PdfPCell());
            table.AddCell(MakeCell(settings.Date.ToLongDateString(), font));

            RemoveTableBorders(table);
            doc.Add(table);
        }

        class Footer : PdfPageEventHelper
        {
            string FooterText { get; }

            string FirstPageFooterText { get; }

            string FooterImagePath { get; }

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
                footer.SetWidths(new []{1f, 1f, 1f});
                var footerTextCell = MakeCell(writer.PageNumber == 1 ? FirstPageFooterText : FooterText, font, Element.ALIGN_CENTER);
                footerTextCell.FixedHeight = cellHeight;
                footer.AddCell(footerTextCell);
                var pageCell = MakeCell($"Page {writer.PageNumber}", font, Element.ALIGN_CENTER);
                pageCell.FixedHeight = cellHeight;
                footer.AddCell(pageCell);
                var footerImageCell = HasFooterImage ? new PdfPCell(Image.GetInstance(FooterImagePath, true)) : new PdfPCell();
                footerImageCell.FixedHeight = cellHeight;
                footerImageCell.HorizontalAlignment = Element.ALIGN_CENTER;
                footer.AddCell(footerImageCell);
                RemoveTableBorders(footer);
                footer.WriteSelectedRows(0, -1, page.BorderWidthLeft, footer.TotalHeight, writer.DirectContent);
            }
        }

        static PdfPTable MakeAttachmentTable()
        {
            var table = new PdfPTable(2)
            {
                WidthPercentage = 100,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.SetWidths(new[] { 5.5f, 4.5f });
            return table;
        }

        private class ThumbnailLayoutHandler : IPdfPCellEvent
        {
            string AttachmentName { get; }

            WeakReference<PdfWriter> PdfWriterWeakRef { get; }

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

        static void WritePdf(PdfSettings settings)
        {
            var font = FontFactory.GetFont("Times New Roman", 10);
            var sectionFont = FontFactory.GetFont("Times New Roman", 16, Font.BOLD);
            using (var fs = settings.UserDefinedTitlePage ? File.OpenWrite(settings.TitlePageFilename) : File.Create(settings.Filename))
            using (var doc = new Document(new Rectangle(PageSize.LETTER)))
            using (var writer = PdfWriter.GetInstance(doc, fs))
            {
                doc.Open();
                doc.SetMargins(settings.PageLeftMargin.GetValueOrDefault(doc.LeftMargin),
                    settings.PageRightMargin.GetValueOrDefault(doc.RightMargin),
                    settings.PageTopMargin.GetValueOrDefault(doc.TopMargin),
                    settings.PageBottomMargin.GetValueOrDefault(doc.BottomMargin));
                writer.PageEvent = new Footer(settings.CaseNumber, settings.NameTitlePin, settings.SignatureFilename);
                if (!settings.UserDefinedTitlePage)
                    CreateTitlePage(doc, settings);
                doc.NewPage();

                var table = MakeAttachmentTable();

                foreach (var attachment in settings.Attachments)
                {
                    string attachmentName = null;
                    if (attachment.IsSection)
                    {
                        if (table.Rows.Any() && table.Complete)
                        {
                            RemoveTableBorders(table);
                            doc.Add(table);
                            doc.NewPage();
                            table = MakeAttachmentTable();
                        }
                        var sectionHeader = MakeCell(attachment.SectionHeader, sectionFont);
                        sectionHeader.Colspan = 2;
                        table.AddCell(sectionHeader);
                    }
                    if (attachment.HasAttachment)
                    {
                        attachmentName = Path.GetFileName(attachment.Attachment);
                        var fileSpec = PdfFileSpecification.FileEmbedded(writer, attachment.Attachment,
                            attachmentName, null);
                        fileSpec.AddDescription(attachmentName, false);
                        writer.AddFileAttachment(fileSpec);
                    }
                    if (attachment.HasThumbnail)
                    {
                        var image = Image.GetInstance(attachment.Thumbnail, true);
                        var cell = new PdfPCell(image)
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        if (attachment.HasAttachment)
                        {
                            cell.CellEvent = new ThumbnailLayoutHandler(attachmentName, writer);
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
                RemoveTableBorders(table);
                doc.Add(table);

                doc.Close();
            }
        }
    }
}
