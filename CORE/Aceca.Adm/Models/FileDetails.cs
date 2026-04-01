namespace Aceca.Adm.Models
{
    public class FileDetails
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
        public byte[] FileData { get; set; }
        public string FileType { get; set; }
    }
}
