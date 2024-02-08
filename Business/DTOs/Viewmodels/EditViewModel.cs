using Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Viewmodels
{
    public class EditViewModel
    {
        public Guid Identifier { get; set; }
        public DateTime VTBegin {  get; set; }
        public DateTime? VTEnd { get; set; }
        public Delta Interpretation {  get; set; }
    }
}
