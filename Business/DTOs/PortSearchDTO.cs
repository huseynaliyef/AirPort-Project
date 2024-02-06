using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs
{
    public class PortSearchDTO
    {
        public DateTime effectiveDate {  get; set; }
        public States State { get; set; }
    }

    public enum States
    {
        BASELINE,
        SNAPSHOT
    }
}
