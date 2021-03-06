﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CommandLine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using ReportGenerator.Reports;

namespace ReportGenerator
{
    internal static class Program
    {
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        class Options
        {
            [Option('r', "report", Required = true, HelpText = "Name of the report to run")]
            public string Report { get; set; }
            [Option('c', "config", Required = true, HelpText = "Path to the report configuration file (JSON)")]
            public string ConfigPath { get; set; }
        }

        private static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Errors.Any())
            {
                foreach (var error in result.Errors)
                    Console.Error.WriteLine(error);
                Environment.Exit(1);
            }

            var options = result.Value;
            if (!File.Exists(options.ConfigPath))
            {
                Console.Error.WriteLine("Report settings file not found");
                Environment.Exit(1);
            }

            Func<string, bool> isReport = name => string.Compare(options.Report, name, StringComparison.OrdinalIgnoreCase) == 0;

            try
            {
                if (isReport(nameof(CaseReport)))
                {
                    WriteCaseReport(File.ReadAllText(options.ConfigPath));
                }
                else if (isReport(nameof(WorkflowSummaryReport)))
                {
                    WriteWorkflowSummaryReport(File.ReadAllText(options.ConfigPath));
                }
                else
                {
                    Console.Error.WriteLine("Unknown report name");
                    Environment.Exit(1);
                }

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

        private static void DeserializeSettings<T>(out T settings, string settingsJson) where T: IReportSettings
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