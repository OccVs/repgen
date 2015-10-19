using System.Collections.Generic;

namespace ReportGenerator.Models
{
    internal class OperationEntry
    {
        public string Name { get; set; }
        public List<OperationDetailEntry> Details { get; set; } 
    }
}
