using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using ReportGenerator.Reports;

namespace ReportGenerator
{
    internal static class Program
    {
        private static void Main(string[] args)
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

            // TODO: Implement handling of different configuration file and report types
            CaseReportSettings settings = null;

            try
            {
                settings = JsonConvert.DeserializeObject<CaseReportSettings>(File.ReadAllText(args[0]));
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
                //WriteReport(settings);
                WriteReport<CaseReportSettings, CaseReport>(settings);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error generating PDF file");
                Console.Error.WriteLine(ex);
                Environment.Exit(1);
            }
        }

        private static void WriteReport<TSettings, TReport>(TSettings settings) where TSettings : ReportSettingsBase
            where TReport : ReportBase<TSettings>
        {
            using (
                var fs = settings.UserDefinedTitlePage
                    ? File.OpenWrite(settings.TitlePageFilename)
                    : File.Create(settings.Filename))
            using (var doc = new Document(new Rectangle(PageSize.LETTER)))
            using (var writer = PdfWriter.GetInstance(doc, fs))
            {
                doc.Open();
                doc.SetMargins(settings.PageLeftMargin.GetValueOrDefault(doc.LeftMargin),
                    settings.PageRightMargin.GetValueOrDefault(doc.RightMargin),
                    settings.PageTopMargin.GetValueOrDefault(doc.TopMargin),
                    settings.PageBottomMargin.GetValueOrDefault(doc.BottomMargin));

                var factory = new ReportFactory<TSettings, TReport>(settings);
                var report = factory.GetInstance(doc, writer);
                report.Generate();

                doc.Close();
            }
        }
    }
}