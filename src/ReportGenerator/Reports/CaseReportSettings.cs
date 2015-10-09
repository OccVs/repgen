using System.Linq;

namespace ReportGenerator.Reports
{
    internal class CaseReportSettings : ReportSettingsBase
    {
        public override bool IsValid
        {
            get
            {
                if (!base.IsValid) return false;

                if (Attachments == null || !Attachments.Any())
                {
                    ValidationFailureList.Add("No attachments found in report settings");
                    return false;
                }

                return true;
            }
        }
    }
}
