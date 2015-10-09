using System.Collections.Generic;

namespace ReportGenerator.Models
{
    internal class WorkflowSummaryReportSettings : ReportSettingsBase
    {
        List<InputFileEntry> InputFiles { get; set; }
        List<OperationEntry> Operations { get; set; } 
        List<OutputFileEntry> OutputFiles { get; set; } 
    }
}
