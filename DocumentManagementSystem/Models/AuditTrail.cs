using System;

namespace DocumentManagementSystem.Models
{
    public class AuditTrail
    {
        public int AuditId { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public string TableName { get; set; }
        public int RecordId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
    }
}
