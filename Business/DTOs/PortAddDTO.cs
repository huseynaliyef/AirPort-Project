using Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs
{
    public class PortAddDTO
    {
        [Required]
        public string Name { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
        [Required]
        public DateTime CertificationDate { get; set; }
        [Required]
        public DateTime EffectiveDate { get; set; }
    }
}
