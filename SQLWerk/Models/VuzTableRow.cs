using System;
using System.Collections.Generic;
using System.Text;

namespace SQLWerk.Models
{
    public class VuzTableRow
    {
        public long Id { get; set; }
        public string? Codvuz { get; set; }
        public string? Z1 { get; set; }
        public string? Z1Full { get; set; }
        public string? Z2 { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }
        public string? Status { get; set; }
        public string? Obl { get; set; }
        public string? OblName { get; set; }
        public string? GrVed { get; set; }
        public string? Prof { get; set; }
    }
}
