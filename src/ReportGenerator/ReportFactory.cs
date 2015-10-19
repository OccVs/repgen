using System;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ReportGenerator.Reports;

namespace ReportGenerator
{
    internal class ReportFactory<TSettings, TReport> where TSettings : ReportSettingsBase
        where TReport : ReportBase<TSettings>
    {
        private TSettings Settings { get; }

        public ReportFactory(TSettings settings)
        {
            Settings = settings;
        }

        public TReport GetInstance(Document document, PdfWriter writer)
        {
            return (TReport) Activator.CreateInstance(typeof (TReport), Settings, document, writer);
        }
    }
}