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
        public string Version { get; set; }
    }

    public class PortSearchUITestDTO
    {
        public int Id { get; set; }
        public Guid? Identifier { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? CertificationDate { get; set; }
        public string Version {  get; set; }
    }
}
