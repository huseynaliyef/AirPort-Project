using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Viewmodels
{
    public class DecommissionViewModel
    {
        public Guid Identifier {  get; set; }
        public DateTime searchedDate { get; set; }
    }
}
