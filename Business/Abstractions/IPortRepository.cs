using Business.DTOs;
using Data.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstractions
{
    public interface IPortRepository
    {
        Task AddPort(PortAddDTO model);
        Task EditPort(PortEditDTO model);
        Task<List<PortSearchUIDTO>> GetPorts(PortSearchDTO model);
        Task DecommissionPort(PortDecommissionDTO model);
    }
}
