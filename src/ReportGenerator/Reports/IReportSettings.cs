using System;

namespace ReportGenerator.Reports
{
    internal interface IReportSettings
    {
        DateTime Date { get; set; }
        string Filename { get; set; }
        string ReportTitle { get; set; }
        bool IsValid();
        string ValidationFailures { get; }
    }
}