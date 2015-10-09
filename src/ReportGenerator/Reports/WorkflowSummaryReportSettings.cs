using System.Collections.Generic;
using ReportGenerator.Models;

namespace ReportGenerator.Reports
{
    internal class WorkflowSummaryReportSettings : ReportSettingsBase
    {
        List<InputFileEntry> InputFiles { get; set; }
        List<OperationEntry> Operations { get; set; } 
        List<OutputFileEntry> OutputFiles { get; set; } 
    }
}
