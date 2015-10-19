namespace ReportGenerator.Models
{
    public class AttachmentEntry
    {
        public string SectionHeader { get; set; }
        public bool IsSection => !string.IsNullOrEmpty(SectionHeader);
        public string ThumbnailPath { get; set; }
        public bool HasThumbnail => !string.IsNullOrEmpty(ThumbnailPath);
        public string AttachmentPath { get; set; }
        public bool HasAttachment => !string.IsNullOrEmpty(AttachmentPath);
        public string DescriptiveText { get; set; }
        public bool HasDescriptiveText => !string.IsNullOrEmpty(DescriptiveText);
    }
}
