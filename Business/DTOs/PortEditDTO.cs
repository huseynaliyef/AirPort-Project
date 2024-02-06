using Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs
{
    public class PortEditDTO
    {
        [Required]
        public Guid Identifier { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        [Required]
        public DateTime EffectiveDate {  get; set; }
        public DateTime? EndEffectiveDate { get; set; }
        [Required]
        public Delta Interpretation { get; set; }
    }
}
