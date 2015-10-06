namespace PDFAttachments.Models
{
    public class AttachmentEntry
    {
        public string SectionHeader { get; set; }
        public bool IsSection => !string.IsNullOrEmpty(SectionHeader);
        public string Thumbnail { get; set; }
        public bool HasThumbnail => !string.IsNullOrEmpty(Thumbnail);
        public string Attachment { get; set; }
        public bool HasAttachment => !string.IsNullOrEmpty(Attachment);
        public string DescriptiveText { get; set; }
        public bool HasDescriptiveText => !string.IsNullOrEmpty(DescriptiveText);
    }
}
