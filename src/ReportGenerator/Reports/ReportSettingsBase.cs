using System;
using System.Collections.Generic;
using System.Text;
using ReportGenerator.Models;

namespace ReportGenerator.Reports
{
    internal abstract class ReportSettingsBase : IReportSettings
    {
        protected ReportSettingsBase()
        {
            ValidationFailureList = new List<string>();
        }

        public float? PageLeftMargin { get; set; }
        public float? PageRightMargin { get; set; }
        public float? PageTopMargin { get; set; }
        public float? PageBottomMargin { get; set; }
        public string TitlePageFilename { get; set; }
        public bool UserDefinedTitlePage => !string.IsNullOrEmpty(TitlePageFilename);
        public string ReportTitle { get; set; }
        public string Filename { get; set; }
        public string CaseNumber { get; set; }
        public string Agency { get; set; }
        public string AgencyInfo { get; set; }
        public string UnitName { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Pin { get; set; }

        public string NameTitlePin
        {
            get
            {
                var ret = new StringBuilder();
                ret.Append(Name);
                if (!string.IsNullOrEmpty(Title))
                    ret.AppendFormat("{0}{1}", ret.Length > 0 ? ", " : "", Title);
                if (!string.IsNullOrEmpty(Pin))
                    ret.AppendFormat("{0}{1}", ret.Length > 0 ? ", " : "", Pin);
                return ret.ToString();
            }
        }

        public List<string> AdditionalNames { get; set; }
        public string AdditionalInfo { get; set; }
        public DateTime Date { get; set; }
        public string HeaderImagePath { get; set; }
        public string FooterImagePath { get; set; }
        public List<AttachmentEntry> Attachments { get; set; }

        public virtual bool IsValid() { return !string.IsNullOrEmpty(Filename); } 
        protected List<string> ValidationFailureList { get; }
        public string ValidationFailures => string.Join(Environment.NewLine, ValidationFailureList);
    }
}