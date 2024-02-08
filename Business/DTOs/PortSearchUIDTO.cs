using Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs
{
    public class PortSearchUIDTO
    {
        public int Id { get; set; }
        public Guid? Identifier { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? CertificationDate { get; set; }
        public int? SequenceNumber { get; set; }
        public int? CorrectionNumber { get; set; }
        public DateTime? LTBegin { get; set; }
        public DateTime? LTEnd { get; set; }
        public DateTime VTBegin { get; set; }
        public DateTime? VTEnd { get; set; }
        public Delta? Interpretation { get; set; }
    }
}
