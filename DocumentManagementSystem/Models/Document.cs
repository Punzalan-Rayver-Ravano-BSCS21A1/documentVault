using System;

namespace DocumentManagementSystem.Models
{
    public class Document
    {
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public string DocumentName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
