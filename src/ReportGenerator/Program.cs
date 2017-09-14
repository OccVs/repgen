using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CommandLine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using ReportGenerator.Reports;
using System.Collections.Generic;

namespace ReportGenerator
{
    public static class Program
    {
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        class Options
        {
            [Option('i', "InputFile", Required = true, HelpText = "InputPath to the report configuration file (JSON)")]
            public string InputFilePath { get; set; }

            [Option('o', "OutputFilePathj", Required = true, HelpText = " OutputPath to the report configuration file (JSON)")]
            public string OutputFilePath { get; set; }
        }

        private static void Main(string[] args)
        {

            var result = Parser.Default.ParseArguments<Options>(args);
           
            var options = result.Value;

            if (result.Errors.Any())
            {
                foreach (var error in result.Errors)
                    Console.Error.WriteLine(error);
                Environment.Exit(1);
            }
            if (!File.Exists(options.InputFilePath))
            {
                Console.Error.WriteLine("Report settings file not found");
                Environment.Exit(1);
            }

            using (var reader = new PdfReader(options.InputFilePath))
            using (var output = File.Open(options.OutputFilePath, FileMode.Create))
            using (var stamper = new PdfStamper(reader, output))
            {
             dynamic reportAssests = JsonConvert.DeserializeObject(args[0]);

                foreach (var item in reportAssests)
                {
                    string filePath = item.ImageFilePath;
                    int page = item.PageNumber;
                    Rectangle location = item.rectangular;

                    string attachmentName = Path.GetFileName(filePath);
                    var fileSpec = PdfFileSpecification.FileEmbedded(stamper.Writer, filePath,
                        attachmentName, null);
                    fileSpec.AddDescription(attachmentName, false);
                    stamper.AddFileAttachment(null, fileSpec);
                    var annot = PdfAnnotation.CreateLink(stamper.Writer, location, PdfAnnotation.HIGHLIGHT_NONE,
                        PdfAction.JavaScript(
                            $"this.exportDataObject({{ cName: '{attachmentName}', nLaunch: 2 }});", stamper.Writer));
                    stamper.AddAnnotation(annot, page);
                }
            }
        }

        private static void WriteCaseReport(string settingsJson)
        {
            CaseReportSettings settings;
            DeserializeSettings(out settings, settingsJson);
            WriteReport<CaseReportSettings, CaseReport>(settings);
        }

        private static void WriteWorkflowSummaryReport(string settingsJson)
        {
            WorkflowSummaryReportSettings settings;
            DeserializeSettings(out settings, settingsJson);
            WriteReport<WorkflowSummaryReportSettings, WorkflowSummaryReport>(settings);
        }

        private static void CheckSettings(IReportSettings settings)
        {
            if (settings.IsValid()) return;
            Console.Error.WriteLine(settings.ValidationFailures);
            Environment.Exit(1);
        }

        private static void DeserializeSettings<T>(out T settings, string settingsJson) where T : IReportSettings
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
