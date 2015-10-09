namespace ReportGenerator.Models
{
    internal abstract class FileEntryBase
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public uint Size { get; set; }
        public uint VideoStreams { get; set; }
        public uint AudioStreams { get; set; }
        public string Codec { get; set; }
        public string PixelFormat { get; set; }
        public decimal FrameRate { get; set; }
        public string SAR { get; set; }
    }
}
