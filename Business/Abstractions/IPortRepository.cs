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
        Task<List<PortOne>> GetPorts();
        Task<PortOne> GetPortById(int Id);
        Task EditPort(PortEditDTO model);
        Task<List<PortSearchUIDTO>> GetPortByBaseLine(PortSearchDTO model);
        Task<List<PortSearchUIDTO>> GetPortBySnapShot(PortSearchDTO model);
        Task DecommissionPort(PortDecommissionDTO model);
    }
}
