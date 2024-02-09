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

        public async Task<List<PortSearchUIDTO>> GetPorts(PortSearchDTO model)
        {
            List<PortSearchUIDTO> searchedPorts = new List<PortSearchUIDTO>();
            PortOne? searchedPort = null;

            var groupedByIdentifier = await _dbContext.Ports.GroupBy(item => item.Identifier).ToListAsync();
            if (groupedByIdentifier.Count == 0)
                return searchedPorts;

            List<List<PortOne>> PortIdentityList = new List<List<PortOne>>();

            for (int i = 0; i < groupedByIdentifier.Count; i++)
            {
                var ListPort = new List<PortOne>();
                ListPort = await _dbContext.Ports.Where(x => x.Identifier == groupedByIdentifier[i].Key).ToListAsync();
                PortIdentityList.Add(ListPort);
            }

            for (int j = 0; j < PortIdentityList.Count; j++)
            {
                var Ports = new List<PortOne>();
                if (model.State == States.BASELINE)
                {
                    Ports = PortIdentityList[j].Where(x => x.VTBegin <= model.effectiveDate && x.Interpretation == Delta.PermDelta).OrderBy(x => x.VTBegin).ToList();
                }

                if(model.State == States.SNAPSHOT)
                {
                    Ports = PortIdentityList[j].Where(x => x.VTBegin <= model.effectiveDate).OrderBy(x => x.VTBegin).ToList();
                }
                DateTime VerifyDate = model.effectiveDate;
                for (int i = 0; i < Ports.Count; i++)
                {
                    if (Ports[i].VTBegin > model.effectiveDate)
                        continue;

                    if (model.State == States.SNAPSHOT)
                    {
                        if(Ports[i].Interpretation == Delta.TempDelta && Ports[i].VTBegin < model.effectiveDate && Ports[i].VTEnd > model.effectiveDate || Ports[i].VTEnd == model.effectiveDate)
                            VerifyDate = Ports[i].VTBegin;
                        if(Ports[i].Interpretation == Delta.PermDelta && Ports[i].VTBegin < model.effectiveDate)
                            VerifyDate = Ports[i].VTBegin;
                    }

                    
                    if (model.State == States.BASELINE && Ports[i].VTBegin < model.effectiveDate)
                        VerifyDate = Ports[i].VTBegin;

                    if (Ports[i].VTBegin == model.effectiveDate)
                    {
                        VerifyDate = Ports[i].VTBegin;
                        break;
                    }
                }

                var verifyDatePorts = await _dbContext.Ports.Where(x => x.VTBegin == VerifyDate).ToListAsync();
                var verifyTempDeltaPort = await _dbContext.Ports.Where(x => x.VTBegin <= VerifyDate && x.VTEnd >= VerifyDate && x.Interpretation == Delta.TempDelta).FirstOrDefaultAsync();


                var LastSearchdPorts = verifyDatePorts.OrderByDescending(x => x.CorrectionNumber).ToList();


                if (LastSearchdPorts.Any())
                {
                    if(model.State == States.BASELINE)
                        searchedPort = LastSearchdPorts.FirstOrDefault();

                    if(model.State == States.SNAPSHOT)
                        searchedPort = verifyTempDeltaPort != null ? verifyTempDeltaPort : LastSearchdPorts.FirstOrDefault();

                    var VTBDateGreatFromEffectiveDatePort = await _dbContext.Ports.Where(x => x.VTBegin > searchedPort.VTBegin).OrderBy(x => x.VTBegin).ThenBy(x => x.CorrectionNumber).FirstOrDefaultAsync();

                    if (searchedPort.Name == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Name != null)
                            {
                                searchedPort.Name = d.Name;
                                break;

                            }
                        }
                    }

                    if (searchedPort.Latitude == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Latitude != null)
                            {
                                searchedPort.Latitude = d.Latitude;
                                break;
                            }
                        }
                    }

                    if (searchedPort.Longitude == null)
                    {
                        foreach (var d in Ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                        {
                            if (d.Longitude != null)
                            {
                                searchedPort.Longitude = d.Longitude;
                                break;
                            }
                        }
                    }

                    if(model.State == States.BASELINE)
                    {
                        var maxSNPort = PortIdentityList[j].Where(x => x.Interpretation == Delta.PermDelta).OrderByDescending(x => x.SequenceNumber).FirstOrDefault();


                        searchedPort.VTEnd = VTBDateGreatFromEffectiveDatePort != null && VTBDateGreatFromEffectiveDatePort.Interpretation == Delta.PermDelta ? VTBDateGreatFromEffectiveDatePort.VTBegin : null;

                        searchedPort.LTEnd = maxSNPort.LTEnd;
                        if (searchedPort.LTEnd != null)
                            searchedPort.VTEnd = searchedPort.LTEnd;

                    }

                    if(model.State == States.SNAPSHOT)
                    {
                        if (verifyTempDeltaPort != null)
                            searchedPort.VTEnd = verifyTempDeltaPort.VTEnd;


                        var maxSNPort = PortIdentityList[j].Where(x => x.Interpretation == Delta.PermDelta).OrderByDescending(x => x.SequenceNumber).FirstOrDefault();

                        if (maxSNPort.LTEnd != null)
                            searchedPort.LTEnd = maxSNPort.LTEnd;
                    }


                    if (searchedPort.LTEnd > model.effectiveDate || searchedPort.LTEnd == null)
                    {
                        var version = "";
                        if (searchedPort.Interpretation == Delta.PermDelta)
                        {
                            version = $"{searchedPort.SequenceNumber}.{searchedPort.CorrectionNumber} BaseLine from {searchedPort.VTBegin}";

                            if (searchedPort.VTEnd != null)
                                version += $" to {searchedPort.VTEnd}"; 

                        }
                        if(searchedPort.Interpretation == Delta.TempDelta)
                        {
                            version = $"{searchedPort.SequenceNumber}.{searchedPort.CorrectionNumber} SnapShot from {searchedPort.VTBegin} to {searchedPort.VTEnd}";
                        }

                        searchedPorts.Add(
                            new PortSearchUIDTO
                            {
                                Id = searchedPort.Id,
                                Identifier = searchedPort.Identifier,
                                Name = searchedPort.Name,
                                Latitude = searchedPort.Latitude,
                                Longitude = searchedPort.Longitude,
                                CertificationDate = searchedPort.CertificationDate,
                                Version = version,
                            });
                    }
                }

            }

            return searchedPorts;
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
