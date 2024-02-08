using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Viewmodels
{
    public class IndexViewModel
    {
        public List<PortSearchUIDTO>? SearchedPorts = new List<PortSearchUIDTO>();
        public DateTime SearchDate { get; set; }
    }
}
