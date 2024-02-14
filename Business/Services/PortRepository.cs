using Business.Abstractions;
using Business.DTOs;
using Data.DAL;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

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

        public async Task EditPort(PortEditDTO model)
        {
            var portList = _dbContext.Ports.AsQueryable().Where(x => x.Identifier == model.Identifier && x.Interpretation == model.Interpretation);
            var permPortList = _dbContext.Ports.AsQueryable().Where(x => x.Identifier == model.Identifier && x.Interpretation == Delta.PermDelta);

            var permPort = permPortList.Where(x => x.SequenceNumber == GetMaxSequenceNumber(permPortList.ToList())).OrderByDescending(x => x.CorrectionNumber).FirstOrDefault();
            var editedPort = portList.Where(x => x.SequenceNumber == GetMaxSequenceNumber(portList.ToList())).OrderByDescending(x => x.CorrectionNumber).FirstOrDefault();

            var newEdit = new PortOne();
            newEdit.Identifier = model.Identifier;
            newEdit.Name = model.Name;
            newEdit.Latitude = model.Latitude;
            newEdit.Longitude = model.Longitude;
            newEdit.CertificationDate = editedPort?.CertificationDate is null ? permPort.CertificationDate : editedPort?.CertificationDate;
            newEdit.SequenceNumber = 0;
            newEdit.CorrectionNumber = 0;
            newEdit.LTBegin = permPort.LTBegin;
            newEdit.LTEnd = permPort.LTEnd;
            newEdit.VTBegin = model.EffectiveDate;
            newEdit.VTEnd = model.EndEffectiveDate;
            newEdit.Interpretation = model.Interpretation;

            SetVersion(model, newEdit, editedPort);

            await _dbContext.Ports.AddAsync(newEdit);
            await _dbContext.SaveChangesAsync();

        }

        public async Task<List<PortSearchUIDTO>> GetPorts(PortSearchDTO model)
        {
            List<PortSearchUIDTO> searchedPorts = new List<PortSearchUIDTO>();

            var groupedByIdentifier = await _dbContext.Ports.GroupBy(item => item.Identifier).ToListAsync();

            if (groupedByIdentifier.Count == 0)
                return searchedPorts;

            foreach (var group in groupedByIdentifier)
            {
                var listPort = _dbContext.Ports.AsQueryable().Where(x => x.Identifier == group.Key);
                var ports = GetPortsListForState(model, listPort);
                var verifyDate = GetVerifyDate(model, ports.ToList());
                PortOne? searchedPort = null;

                var verifyTempDeltaPort = await _dbContext.Ports
                    .Where(x => x.VTBegin <= verifyDate && x.VTEnd >= verifyDate && x.Interpretation == Delta.TempDelta)
                    .OrderByDescending(x=>x.CorrectionNumber).FirstOrDefaultAsync();

                var lastSearchdPorts = _dbContext.Ports.AsQueryable().Where(x => x.VTBegin == verifyDate).OrderByDescending(x => x.CorrectionNumber);


                if (lastSearchdPorts.Any())
                {
                    if(model.State == States.BASELINE)
                        searchedPort = lastSearchdPorts.FirstOrDefault();

                    else if(model.State == States.SNAPSHOT)
                        searchedPort = verifyTempDeltaPort != null ? verifyTempDeltaPort : lastSearchdPorts.FirstOrDefault();


                    var vtbDateGreatFromEffectiveDatePort = await _dbContext.Ports
                        .Where(x => x.VTBegin > searchedPort.VTBegin && x.Interpretation == Delta.PermDelta && model.State == States.BASELINE ||
                        x.VTBegin > searchedPort.VTBegin && model.State == States.SNAPSHOT)
                        .OrderBy(x => x.VTBegin).ThenBy(x => x.CorrectionNumber).FirstOrDefaultAsync();

                    SetValueToNullData(verifyDate, searchedPort, ports);

                    if (model.State == States.BASELINE)
                    {
                        var maxSNPort = listPort.Where(x => x.Interpretation == Delta.PermDelta)
                            .OrderByDescending(x => x.SequenceNumber).FirstOrDefault();

                        searchedPort.VTEnd = vtbDateGreatFromEffectiveDatePort != null ? vtbDateGreatFromEffectiveDatePort.VTBegin : null;
                        searchedPort.LTEnd = maxSNPort?.LTEnd;

                    }
                    else if(model.State == States.SNAPSHOT)
                    {
                        searchedPort.VTEnd = verifyTempDeltaPort != null ? verifyTempDeltaPort.VTEnd : (vtbDateGreatFromEffectiveDatePort != null ? vtbDateGreatFromEffectiveDatePort.VTBegin : null);

                        var maxSNPort = listPort.Where(x => x.Interpretation == Delta.PermDelta)
                            .OrderByDescending(x => x.SequenceNumber).FirstOrDefault();

                        searchedPort.LTEnd = maxSNPort?.LTEnd != null ? maxSNPort.LTEnd : null;

                    }


                    if (searchedPort.LTEnd > model.effectiveDate || searchedPort.LTEnd == null)
                    {
                        var version = GetVersion(searchedPort);

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
            var portList = _dbContext.Ports.AsQueryable().Where(x=>x.Identifier == model.Identifier);
            int maxSN = GetMaxSequenceNumber(portList.ToList());

            var port = portList.Where(x=>x.SequenceNumber == maxSN).OrderByDescending(x=>x.CorrectionNumber).FirstOrDefault();
            if(port.VTBegin == model.EffectiveDate)
            {
                port.CorrectionNumber++;
            }
            else
            {
                port.SequenceNumber++;
                port.CorrectionNumber = 0;
            }
            port.LTEnd = model.EffectiveDate;
            port.Name = null;
            port.Latitude = null;
            port.Longitude = null;
            port.CertificationDate = null;
            port.Id = 0;

            await _dbContext.Ports.AddAsync(port);
            await _dbContext.SaveChangesAsync();
        }

        private int GetMaxSequenceNumber(List<PortOne> portList)
        {
            int maxSN = 0;
            for (int i = 0; i < portList.Count; i++)
            {
                if (portList[i].SequenceNumber > maxSN)
                    maxSN = portList[i].SequenceNumber;

            }
            return maxSN;
        }

        private void SetVersion(PortEditDTO model, PortOne newEdit, PortOne editedPort)
        {
            if (editedPort == null)
            {
                newEdit.VTBegin = model.EffectiveDate;
                newEdit.VTEnd = model.EndEffectiveDate;
                newEdit.SequenceNumber = 1;
                newEdit.CorrectionNumber = 0;
            }
            else if (editedPort.VTBegin == model.EffectiveDate)
            {
                newEdit.CorrectionNumber = editedPort.CorrectionNumber + 1;
                newEdit.SequenceNumber = editedPort.SequenceNumber;
            }
            else
            {
                newEdit.VTBegin = model.EffectiveDate;
                newEdit.VTEnd = model.EndEffectiveDate;
                newEdit.SequenceNumber = editedPort.SequenceNumber + 1;
                newEdit.CorrectionNumber = 0;
            }
        }

        private string GetVersion(PortOne searchedPort)
        {
            var version = "";
            if (searchedPort.Interpretation == Delta.PermDelta)
            {
                version = $"{searchedPort.SequenceNumber}.{searchedPort.CorrectionNumber} BaseLine from {searchedPort.VTBegin}";

                if (searchedPort.VTEnd != null)
                    version += $" to {searchedPort.VTEnd}";

                else if (searchedPort.VTEnd == null && searchedPort.LTEnd != null)
                    version += $" to {searchedPort.LTEnd}";

            }
            else if (searchedPort.Interpretation == Delta.TempDelta)
            {
                searchedPort.VTEnd = searchedPort.LTEnd < searchedPort.VTEnd ? searchedPort.LTEnd : searchedPort.VTEnd;
                version = $"{searchedPort.SequenceNumber}.{searchedPort.CorrectionNumber} SnapShot from {searchedPort.VTBegin} to {searchedPort.VTEnd}";
            }

            return version;
        }

        private DateTime GetVerifyDate(PortSearchDTO model, List<PortOne> ports)
        {
            DateTime verifyDate = model.effectiveDate;

            for (int i = 0; i < ports.Count; i++)
            {
                if (ports[i].VTBegin > model.effectiveDate)
                    continue;

                if (model.State == States.SNAPSHOT)
                {
                    if (ports[i].Interpretation == Delta.TempDelta && ports[i].VTBegin < model.effectiveDate && ports[i].VTEnd > model.effectiveDate || ports[i].VTEnd == model.effectiveDate)
                        verifyDate = ports[i].VTBegin;
                    if (ports[i].Interpretation == Delta.PermDelta && ports[i].VTBegin < model.effectiveDate)
                        verifyDate = ports[i].VTBegin;
                }


                if (model.State == States.BASELINE && ports[i].VTBegin < model.effectiveDate)
                    verifyDate = ports[i].VTBegin;

                if (ports[i].VTBegin == model.effectiveDate)
                {
                    verifyDate = ports[i].VTBegin;
                    break;
                }
            }

            return verifyDate;
        }

        private void SetValueToNullData(DateTime verifyDate, PortOne searchedPort, IQueryable<PortOne> ports)
        {
            if (searchedPort.Name == null)
            {
                foreach (var d in ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                {
                    if (d.Name != null && (d.VTEnd < verifyDate || d.VTEnd == null))
                    {
                        searchedPort.Name = d.Name;
                        break;

                    }
                }
            }

            if (searchedPort.Latitude == null)
            {
                foreach (var d in ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                {
                    if (d.Latitude != null && (d.VTEnd < verifyDate || d.VTEnd == null))
                    {
                        searchedPort.Latitude = d.Latitude;
                        break;
                    }
                }
            }

            if (searchedPort.Longitude == null)
            {
                foreach (var d in ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                {
                    if (d.Longitude != null && (d.VTEnd < verifyDate || d.VTEnd == null))
                    {
                        searchedPort.Longitude = d.Longitude;
                        break;
                    }
                }
            }
        }

        private IQueryable<PortOne> GetPortsListForState(PortSearchDTO model, IQueryable<PortOne> portIdentityListItem)
        {

            if (model.State == States.BASELINE)
                return portIdentityListItem.AsQueryable().Where(x => x.VTBegin <= model.effectiveDate && x.Interpretation == Delta.PermDelta).OrderBy(x => x.VTBegin);

            else
               return portIdentityListItem.AsQueryable().Where(x => x.VTBegin <= model.effectiveDate).OrderBy(x => x.VTBegin);
        }

    }
}
