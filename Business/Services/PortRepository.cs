using Business.Abstractions;
using Business.DTOs;
using Data.DAL;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services
{
    public class PortRepository : IPortRepository
    {
        private readonly AirPortDbContext _dbContext;
        public PortRepository(AirPortDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddPort(PortAddDTO model)
        {
            var newPort = new PortOne();
            newPort.Identifier = Guid.NewGuid();
            newPort.Name = model.Name;
            newPort.Latitude = model.Latitude;
            newPort.Longitude = model.Longitude;
            newPort.CertificationDate = model.CertificationDate;
            newPort.LTBegin = model.EffectiveDate;
            newPort.VTBegin = model.EffectiveDate;
            newPort.Interpretation = Delta.PermDelta;
            newPort.SequenceNumber = 1;
            newPort.CorrectionNumber = 0;
            await _dbContext.Ports.AddAsync(newPort);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<PortOne>> GetPorts()
        {
            return await _dbContext.Ports.ToListAsync();
        }

        public async Task<PortOne> GetPortById(int Id)
        {
            return await _dbContext.Ports.FindAsync(Id);
        }

        public async Task EditPort(PortEditDTO model)
        {

            var PortList = await _dbContext.Ports.Where(x => x.Identifier == model.Identifier).ToListAsync();
            var PortListTemp = await _dbContext.Ports.Where(x => x.Identifier == model.Identifier && x.Interpretation == Delta.TempDelta).ToListAsync();
            
            int maxSN = 0;
            int maxSNTemp = 0;

            for (int i = 0; i < PortList.Count; i++)
            {
                if (PortList[i].SequenceNumber > maxSN)
                    maxSN = PortList[i].SequenceNumber;

            }

            for(int i = 0; i < PortListTemp.Count; i++)
            {
                if (PortListTemp[i].SequenceNumber > maxSNTemp)
                    maxSNTemp = PortListTemp[i].SequenceNumber;

            }
            var EditedPort = PortList.Where(x => x.SequenceNumber == maxSN).OrderByDescending(x => x.CorrectionNumber).FirstOrDefault();
            var EditPortTemp = PortList.Where(x => x.SequenceNumber == maxSNTemp && x.Interpretation == Delta.TempDelta).OrderByDescending(x => x.CorrectionNumber).FirstOrDefault();

            var newEdit = new PortOne
            {
                Identifier = model.Identifier,
                Name = model.Name,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                CertificationDate = EditedPort.CertificationDate,
                SequenceNumber = 0,
                CorrectionNumber = 0,
                LTBegin = EditedPort.LTBegin,
                LTEnd = EditedPort.LTEnd,
                VTBegin = model.EffectiveDate,
                VTEnd = model.EndEffectiveDate,
                Interpretation = model.Interpretation
            };
            
            if(model.Interpretation == Delta.TempDelta)
            {
                if(EditPortTemp == null)
                {
                    newEdit.VTBegin = model.EffectiveDate;
                    newEdit.VTEnd = model.EndEffectiveDate;
                    newEdit.SequenceNumber = 1;
                    newEdit.CorrectionNumber = 0;
                }
                else if(EditPortTemp.VTBegin == model.EffectiveDate)
                {
                    newEdit.CorrectionNumber = EditPortTemp.CorrectionNumber + 1;
                    newEdit.SequenceNumber = EditPortTemp.SequenceNumber;
                }
                else
                {
                    newEdit.VTBegin = model.EffectiveDate;
                    newEdit.SequenceNumber = EditPortTemp.SequenceNumber + 1;
                    newEdit.CorrectionNumber = 0;
                }

            }
            else if(model.Interpretation == Delta.PermDelta)
            {
                if (EditedPort.VTBegin == model.EffectiveDate)
                {
                    newEdit.CorrectionNumber = EditedPort.CorrectionNumber + 1;
                    newEdit.SequenceNumber = EditedPort.SequenceNumber;
                }
                else
                {
                    newEdit.VTBegin = model.EffectiveDate;
                    newEdit.SequenceNumber = EditedPort.SequenceNumber + 1;
                    newEdit.CorrectionNumber = 0;
                }
            }

            await _dbContext.Ports.AddAsync(newEdit);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<PortSearchUIDTO>> GetPortByBaseLine(PortSearchDTO model)
        {

            List<PortSearchUIDTO> SearchedPorts = new List<PortSearchUIDTO>();
            PortOne? SearchedPort = null;

            var groupedByIdentifier = await _dbContext.Ports.GroupBy(item => item.Identifier).ToListAsync();
            var count = groupedByIdentifier.Count;
            List<List<PortOne>> PortIdentityList = new List<List<PortOne>>();

            for (int i = 0; i < groupedByIdentifier.Count; i++)
            {
                var ListPort = await _dbContext.Ports.Where(x => x.Identifier == groupedByIdentifier[i].Key && x.Interpretation == Delta.PermDelta).ToListAsync();
                PortIdentityList.Add(ListPort);
            }

            for (int j = 0; j < PortIdentityList.Count; j++)
            {
                var Ports = PortIdentityList[j].Where(x => x.VTBegin <= model.effectiveDate).OrderBy(x => x.VTBegin).ToList();
                DateTime TempDate = model.effectiveDate;
                for (int i = 0; i < Ports.Count; i++)
                {
                    if (Ports[i].VTBegin > model.effectiveDate)
                        continue;
                    if (Ports[i].VTBegin < model.effectiveDate)
                        TempDate = Ports[i].VTBegin;
                    if (Ports[i].VTBegin == model.effectiveDate)
                    {
                        TempDate = Ports[i].VTBegin;
                        break;
                    }
                }

                var VerifyTempDAtePorts = await _dbContext.Ports.Where(x => x.VTBegin == TempDate).ToListAsync();


                var LastSearchdPorts = VerifyTempDAtePorts.OrderByDescending(x => x.CorrectionNumber).ToList();


                if (LastSearchdPorts.Any())
                {
                    SearchedPort = LastSearchdPorts.FirstOrDefault();
                    var VTBDateGreatFromEffectiveDatePort = await _dbContext.Ports.Where(x => x.VTBegin > SearchedPort.VTBegin).OrderBy(x => x.VTBegin).ThenBy(x => x.CorrectionNumber).FirstOrDefaultAsync();

                    if (SearchedPort.Name == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Name != null)
                            {
                                SearchedPort.Name = d.Name;
                                break;

                            }
                        }
                    }

                    if (SearchedPort.Latitude == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Latitude != null)
                            {
                                SearchedPort.Latitude = d.Latitude;
                                break;
                            }
                        }
                    }

                    if (SearchedPort.Longitude == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Longitude != null)
                            {
                                SearchedPort.Longitude = d.Longitude;
                                break;
                            }
                        }
                    }

                    var maxSNPort = PortIdentityList[j].Where(x=>x.Interpretation == Delta.PermDelta).OrderByDescending(x => x.SequenceNumber).FirstOrDefault();
                    

                    SearchedPort.VTEnd = VTBDateGreatFromEffectiveDatePort != null ? VTBDateGreatFromEffectiveDatePort.VTBegin : null;

                    SearchedPort.LTEnd = maxSNPort.LTEnd;
                    if (SearchedPort.LTEnd != null)
                        SearchedPort.VTEnd = SearchedPort.LTEnd;

                    if (SearchedPort.LTEnd > model.effectiveDate || SearchedPort.LTEnd == null)
                    {
                        SearchedPorts.Add(
                            new PortSearchUIDTO
                            {
                                Id = SearchedPort.Id,
                                Identifier = SearchedPort.Identifier,
                                Name = SearchedPort.Name,
                                Latitude = SearchedPort.Latitude,
                                Longitude = SearchedPort.Longitude,
                                CertificationDate = SearchedPort.CertificationDate,
                                SequenceNumber = SearchedPort.SequenceNumber,
                                CorrectionNumber = SearchedPort.CorrectionNumber,
                                LTBegin = SearchedPort.LTBegin,
                                LTEnd = SearchedPort.LTEnd,
                                VTBegin = SearchedPort.VTBegin,
                                VTEnd = SearchedPort.VTEnd,
                                Interpretation = SearchedPort.Interpretation,
                            });
                    }
                }

            }

            return SearchedPorts;

        }

        public async Task<List<PortSearchUIDTO>> GetPortBySnapShot(PortSearchDTO model)
        {
            List<PortSearchUIDTO> SearchedPorts = new List<PortSearchUIDTO>();
            PortOne? SearchedPort = null;

            var groupedByIdentifier = await _dbContext.Ports.GroupBy(item => item.Identifier).ToListAsync();
            var data = groupedByIdentifier[0].Key;
            var count = groupedByIdentifier.Count;
            List<List<PortOne>> PortIdentityList = new List<List<PortOne>>();

            for (int i = 0; i < groupedByIdentifier.Count; i++)
            {
                var ListPort = await _dbContext.Ports.Where(x => x.Identifier == groupedByIdentifier[i].Key).ToListAsync();
                PortIdentityList.Add(ListPort);
            }

            for (int j = 0; j < PortIdentityList.Count; j++)
            {
                var Ports = PortIdentityList[j].Where(x => x.VTBegin <= model.effectiveDate).OrderBy(x => x.VTBegin).ToList();
                DateTime PermDate = model.effectiveDate;
                for (int i = 0; i < Ports.Count; i++)
                {
                    if (Ports[i].VTBegin > model.effectiveDate)
                        continue;
                    if (Ports[i].VTBegin < model.effectiveDate)
                        PermDate = Ports[i].VTBegin;
                    if (Ports[i].VTBegin == model.effectiveDate)
                    {
                        PermDate = Ports[i].VTBegin;
                        break;
                    }
                }

                var VerifyPorts = await _dbContext.Ports.Where(x => x.VTBegin == PermDate).ToListAsync();

                var verifyTempDeltaPort = await _dbContext.Ports.Where(x=>x.VTBegin <= PermDate && x.VTEnd >= PermDate && x.Interpretation == Delta.TempDelta).FirstOrDefaultAsync();

                var LastSearchdPorts = VerifyPorts.OrderByDescending(x => x.CorrectionNumber).ToList();


                if (LastSearchdPorts.Any())
                {
                    SearchedPort = verifyTempDeltaPort != null ? verifyTempDeltaPort : LastSearchdPorts.FirstOrDefault();
                    var VTBDateGreatFromEffectiveDatePort = await _dbContext.Ports.Where(x => x.VTBegin > SearchedPort.VTBegin).OrderBy(x => x.VTBegin).ThenBy(x => x.CorrectionNumber).FirstOrDefaultAsync();

                    if (SearchedPort.Name == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Name != null)
                            {
                                SearchedPort.Name = d.Name;
                                break;

                            }
                        }
                    }

                    if (SearchedPort.Latitude == null)
                    {
                        var OrderedByDeceddingPorts = Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber).ToList();
                        foreach (var d in OrderedByDeceddingPorts)
                        {
                            if (d.Latitude != null)
                            {
                                SearchedPort.Latitude = d.Latitude;
                                break;
                            }
                        }
                    }

                    if (SearchedPort.Longitude == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Longitude != null)
                            {
                                SearchedPort.Longitude = d.Longitude;
                                break;
                            }
                        }
                    }

                    if(verifyTempDeltaPort != null)
                        SearchedPort.VTEnd = verifyTempDeltaPort.VTEnd;


                    var maxSNPort = PortIdentityList[j].Where(x => x.Interpretation == Delta.PermDelta).OrderByDescending(x => x.SequenceNumber).FirstOrDefault();
                    
                    if(maxSNPort.LTEnd != null)
                        SearchedPort.LTEnd = maxSNPort.LTEnd;


                    if (SearchedPort.LTEnd > model.effectiveDate || SearchedPort.LTEnd == null)
                    {

                        SearchedPorts.Add(
                            new PortSearchUIDTO 
                            { 
                                Id = SearchedPort.Id,
                                Identifier = SearchedPort.Identifier,
                                Name = SearchedPort.Name,
                                Latitude = SearchedPort.Latitude,
                                Longitude = SearchedPort.Longitude,
                                CertificationDate = SearchedPort.CertificationDate,
                                SequenceNumber = SearchedPort.SequenceNumber,
                                CorrectionNumber = SearchedPort.CorrectionNumber,
                                LTBegin = SearchedPort.LTBegin,
                                LTEnd = SearchedPort.LTEnd,
                                VTBegin = SearchedPort.VTBegin,
                                VTEnd = SearchedPort.VTEnd,
                                Interpretation = SearchedPort.Interpretation,
                            });
                    }

                }

            }
            return SearchedPorts;
        }

        public async Task DecommissionPort(PortDecommissionDTO model)
        {
            var PortList = await _dbContext.Ports.Where(x=>x.Identifier == model.Identifier).ToListAsync();
            int maxSN = 0;

            for (int i = 0; i < PortList.Count; i++)
            {
                if (PortList[i].SequenceNumber > maxSN)
                    maxSN = PortList[i].SequenceNumber;

            }

            var Port = PortList.Where(x=>x.SequenceNumber == maxSN).OrderByDescending(x=>x.CorrectionNumber).FirstOrDefault();
            if(Port.VTBegin == model.EffectiveDate)
            {
                Port.CorrectionNumber++;
            }
            else
            {
                Port.SequenceNumber++;
                Port.CorrectionNumber = 0;
            }
            Port.LTEnd = model.EffectiveDate;
            Port.Name = null;
            Port.Latitude = null;
            Port.Longitude = null;
            Port.CertificationDate = null;
            Port.Id = 0;

            await _dbContext.Ports.AddAsync(Port);
            await _dbContext.SaveChangesAsync();
        }
    }
}
