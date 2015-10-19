using System.Collections.Generic;
using System.Linq;
using ReportGenerator.Models;

namespace ReportGenerator.Reports
{
    internal class WorkflowSummaryReportSettings : ReportSettingsBase
    {
        public string HeaderFontFace { get; set; }
        public ushort HeaderFontSize { get; set; }
        public string FontFace { get; set; }
        public ushort FontSize { get; set; }
        public List<InputFileEntry> InputFiles { get; set; }
        public List<OperationEntry> Operations { get; set; } 
        public List<OutputFileEntry> OutputFiles { get; set; }

        public override bool IsValid()
        {
            if (!base.IsValid()) return false;

            if (InputFiles == null || !InputFiles.Any())
            {
                ValidationFailureList.Add("No input files found in report settings");
                return false;
            }

            if (Operations == null || !Operations.Any())
            {
                ValidationFailureList.Add("No operations found in report settings");
                return false;
            }

            if (OutputFiles == null || !OutputFiles.Any())
            {
                ValidationFailureList.Add("No output files found in report settings");
                return false;
            }

            return true;
        }
    }
}
