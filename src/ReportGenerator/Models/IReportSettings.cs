using System;

namespace ReportGenerator.Models
{
    internal interface IReportSettings
    {
        DateTime Date { get; set; }
        string Filename { get; set; }
        string ReportTitle { get; set; }
    }
}