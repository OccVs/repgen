using System.Collections.Generic;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using ReportGenerator.Extensions;
using ReportGenerator.Models;

namespace ReportGenerator.Reports
{
    internal class WorkflowSummaryReport : ReportBase<WorkflowSummaryReportSettings>
    {
        private Font HeaderFont { get; }
        private Font Font { get; }
        private static readonly BaseColor BlueColor = new BaseColor(65, 177, 225);

        private sealed class Spacer : Paragraph
        {
            public Spacer(float spacing) : base(" ")
            {
                PaddingTop = spacing;
            }
        }

        private abstract class SummaryTable : PdfPTable
        {
            protected SummaryTable(int columns) : base(columns)
            {
            }

            protected void InitializeTable(float? width)
            {
                if (width.HasValue)
                {
                    TotalWidth = width.Value;
                    LockedWidth = true;
                }
                else
                    WidthPercentage = 100;
                HorizontalAlignment = Element.ALIGN_CENTER;
            }

            protected void FinalizeTable()
            {

                // Apparently tables won't add if we have less than 1 row (in this case, less than two cells)
                while (Rows.Count < 1)
                    AddCell(string.Empty);

                for (var r = 0; r < Rows.Count; r++)
                {
                    var cells = Rows[r].GetCells().ToArray();
                    for (var col = 0; col < cells.Length; col++)
                    {
                        if (r != 0)
                        {
                            cells[col].PaddingTop = 15f;
                        }
                        cells[col].PaddingBottom = 15f;
                        cells[col].PaddingLeft = 15f;

                        cells[col].BorderColor = BaseColor.LIGHT_GRAY;
                        cells[col].BorderWidthTop = 0;
                        cells[col].BorderWidthLeft = 0;

                        if (col == 0) continue;
                        cells[col].BorderWidthRight = 0;
                    }
                }

                var lastRowCells = Rows[Rows.Count - 1].GetCells();
                lastRowCells[0].BorderWidthBottom = 0;
                lastRowCells[1].BorderWidthBottom = 0;
            }
        }

        private sealed class FileTable : SummaryTable
        {
            public FileTable(IEnumerable<FileEntryBase> files, Font headerFont, Font font, float? width) : base(2)
            {
                InitializeTable(width);

                var paragraphs = files.Select(entry =>
                {
                    var phrase = new Phrase
                    {
                        new Chunk($"Input #1 Name: {entry.Name}\n", headerFont),
                        new Chunk($"MD5 Hash: {entry.Hash}\n", font),
                        new Chunk($"Video Streams: {entry.VideoStreams}\n", font),
                        new Chunk($"Audio Streams: {entry.AudioStreams}\n", font),
                        new Chunk($"Format: {entry.Codec}\n", font),
                        new Chunk($"File Size: {entry.Size.AsReadableFileSize()}\n", font),
                        new Chunk($"Pixel Format: {entry.PixelFormat}\n", font),
                        new Chunk($"Frame Rate: {entry.FrameRate}\n", font),
                        new Chunk($"SAR: {entry.SAR}\n", font)
                    };
                    return new Paragraph(phrase);
                });

                foreach (var p in paragraphs)
                {
                    AddCell(p);
                }

                FinalizeTable();
            }
        }

        private sealed class OperationTable : SummaryTable
        {
            public OperationTable(WorkflowSummaryReportSettings settings, Font headerFont, Font font, float? width) : base(2)
            {
                InitializeTable(width);

                var paragraphs = settings.Operations.Select(entry =>
                {
                    var phrase = new Phrase();
                    phrase.Add(new Chunk($"{entry.Name}\n", headerFont));
                    entry.Details.ForEach(detail =>
                    {
                        phrase.Add(new Chunk($"{detail.Description}: {detail.Settings}\n", font));
                    });
                    return new Paragraph(phrase);
                });

                foreach (var p in paragraphs)
                {
                    AddCell(p);
                }

                FinalizeTable();
            }
        }

        private class HeaderFooter : PdfPageEventHelper
        {
            private string HeaderImagePath { get; }
            private bool HasHeaderImage => !string.IsNullOrEmpty(HeaderImagePath);

            public HeaderFooter(string headerImagePath)
            {
                HeaderImagePath = headerImagePath;
            }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var canvas = writer.DirectContent;
                // Header
                var topRect = new Rectangle(0, document.PageSize.Height - document.TopMargin, document.PageSize.Width,
                    document.PageSize.Top)
                {
                    BackgroundColor = BlueColor
                };
                canvas.Rectangle(topRect);
                if (HasHeaderImage)
                {
                    var image = Image.GetInstance(HeaderImagePath, true);
                    image.SetAbsolutePosition(document.LeftMargin, document.PageSize.Height - (document.TopMargin - image.Height / 2));
                    canvas.AddImage(image);
                }

                // Footer
                canvas.Rectangle(new Rectangle(0, 0, document.PageSize.Width,
                    document.BottomMargin)
                {
                    BackgroundColor = BlueColor
                });
            }
        }

        public WorkflowSummaryReport(WorkflowSummaryReportSettings settings, Document document, PdfWriter writer)
            : base(settings, document, writer)
        {
            HeaderFont = FontFactory.GetFont(Settings.HeaderFontFace, Settings.HeaderFontSize, Font.BOLD);
            Font = FontFactory.GetFont(Settings.FontFace, Settings.FontSize);
            HeaderFooterHelper = new HeaderFooter(Settings.HeaderImagePath);
        }

        protected override void CreateTitlePage()
        {
        }

        public override void Generate()
        {
            base.Generate();
            Document.Add(new Spacer(40f));
            var tableWidth = Document.PageSize.Width - (Document.LeftMargin + Document.RightMargin);
            Document.Add(new FileTable(Settings.InputFiles, HeaderFont, Font, tableWidth));
            var blueLine = new Chunk(new LineSeparator(6f, 100f, BlueColor, Element.ALIGN_CENTER, 0f));
            Document.Add(blueLine);
            Document.Add(new OperationTable(Settings, HeaderFont, Font, tableWidth));
            Document.Add(blueLine);
            Document.Add(new FileTable(Settings.OutputFiles, HeaderFont, Font, tableWidth));
        }
    }
}