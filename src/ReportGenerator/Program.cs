using System;
using System.IO;
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
                Console.Error.WriteLine("No report settings file specified");
                Environment.Exit(1);
            }
            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("Report settings file not found");
                Environment.Exit(1);
            }

            try
            {
                //WriteReport(settings);
                // TODO: Implement handling of different configuration file and report types
                WriteCaseReport(File.ReadAllText(args[0]));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error generating report");
                Console.Error.WriteLine(ex);
                Environment.Exit(1);
            }
        }

        private static void WriteCaseReport(string settingsJson)
        {
            CaseReportSettings settings;
            DeserializeSetting(out settings, settingsJson);
            WriteReport<CaseReportSettings, CaseReport>(settings);
        }

        private static void CheckSettings(IReportSettings settings)
        {
            if (settings.IsValid) return;
            Console.Error.WriteLine(settings.ValidationFailures);
            Environment.Exit(1);
        }

        private static void DeserializeSetting<T>(out T settings, string settingsJson) where T: IReportSettings
        {
            settings = default(T);
            try
            {
                settings = JsonConvert.DeserializeObject<T>(settingsJson);
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Error parsing settings JSON file");
                Environment.Exit(1);
            }
        }

        private static void WriteReport<TSettings, TReport>(TSettings settings) where TSettings : ReportSettingsBase
            where TReport : ReportBase<TSettings>
        {
            CheckSettings(settings);
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