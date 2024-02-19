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

            var newEdit = new PortOne
            {
                Identifier = model.Identifier,
                Name = model.Name,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                SequenceNumber = 0,
                CorrectionNumber = 0,
                LTBegin = permPort.LTBegin,
                LTEnd = permPort.LTEnd,
                VTBegin = model.EffectiveDate,
                VTEnd = model.EndEffectiveDate,
                Interpretation = model.Interpretation
            };

            SetVersion(model, newEdit, editedPort);

            await _dbContext.Ports.AddAsync(newEdit);
            await _dbContext.SaveChangesAsync();

        }

        public async Task<List<PortDTO>> GetPorts(PortSearchDTO model)
        {
            var result = new List<PortDTO>();

            var groupedByIdentifier = await _dbContext.Ports.GroupBy(item => item.Identifier).ToListAsync();

            if (groupedByIdentifier.Count == 0)
                return result;

            foreach (var group in groupedByIdentifier)
            {
                var ports = group.Where(x => x.VTBegin <= model.EffectiveDate).ToList();

                if (model.State == States.BASELINE)
                    ports = ports.Where(x => x.Interpretation == Delta.PermDelta).ToList();
                else
                    ports = ports.Where(x => x.Interpretation == Delta.PermDelta || x.VTEnd >= model.EffectiveDate).ToList();

                var port = new PortOne();

                if (ports.Any())
                {
                    port = ports.Where(x=>x.LTEnd == null).LastOrDefault();
                    var decommissionedPort = ports.Where(x => x.LTEnd != null && x.LTEnd < model.EffectiveDate).FirstOrDefault();

                    if (model.State == States.SNAPSHOT)
                    {
                        var tempPort = ports.Where(x => x.Interpretation == Delta.TempDelta).LastOrDefault();
                        port = tempPort != null ? tempPort : port;
                    }

                    SetEndDate(model, port, group);
                    SetValueToNullData(port, ports);

                    if (decommissionedPort != null)
                        port = decommissionedPort;

                    if (port.LTEnd > model.EffectiveDate || port.LTEnd == null)
                    {
                        var version = GetVersion(port);
                        result.Add(
                            new PortDTO
                            {
                                Id = port.Id,
                                Identifier = port.Identifier,
                                Name = port.Name,
                                Latitude = port.Latitude,
                                Longitude = port.Longitude,
                                CertificationDate = port.CertificationDate,
                                TimeSlice = version,
                            });
                    }
                }
            }

            return result;
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
            port.LTBegin = model.EffectiveDate;
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

        private string GetVersion(PortOne port)
        {
            var version = "";
            if (port.Interpretation == Delta.PermDelta)
            {
                version = $"{port.SequenceNumber}.{port.CorrectionNumber} BaseLine from {port.VTBegin}";

                if (port.VTEnd != null)
                    version += $" to {port.VTEnd}";

                else if (port.VTEnd == null && port.LTEnd != null)
                    version += $" to {port.LTEnd}";

            }
            else if (port.Interpretation == Delta.TempDelta)
            {
                port.VTEnd = port.LTEnd < port.VTEnd ? port.LTEnd : port.VTEnd;
                version = $"{port.SequenceNumber}.{port.CorrectionNumber} SnapShot from {port.VTBegin} to {port.VTEnd}";
            }

            return version;
        }

        private void SetValueToNullData(PortOne port, IEnumerable<PortOne> ports)
        {
            if (port.Name == null)
            {
                foreach (var d in ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                {
                    if (d.Name != null && (d.VTEnd < port.VTBegin|| d.VTEnd == null))
                    {
                        port.Name = d.Name;
                        break;

                    }
                }
            }

            if (port.Latitude == null)
            {
                foreach (var d in ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                {
                    if (d.Latitude != null && (d.VTEnd < port.VTBegin || d.VTEnd == null))
                    {
                        port.Latitude = d.Latitude;
                        break;
                    }
                }
            }

            if (port.Longitude == null)
            {
                foreach (var d in ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                {
                    if (d.Longitude != null && (d.VTEnd < port.VTBegin || d.VTEnd == null))
                    {
                        port.Longitude = d.Longitude;
                        break;
                    }
                }
            }

            if (port.CertificationDate == null)
            {
                foreach (var d in ports.OrderByDescending(x => x.VTBegin).ThenByDescending(x => x.CorrectionNumber))
                {
                    if (d.CertificationDate != null && (d.VTEnd < port.VTBegin || d.VTEnd == null))
                    {
                        port.CertificationDate = d.CertificationDate;
                        break;
                    }
                }
            }
        }

        private void SetEndDate(PortSearchDTO model, PortOne port, IGrouping<Guid, PortOne> group)
        {
            var endValidTime = group.Where(x => x.VTBegin > model.EffectiveDate && x.Interpretation == Delta.PermDelta && model.State == States.BASELINE ||
                        x.VTBegin > model.EffectiveDate && model.State == States.SNAPSHOT)
                        .OrderBy(x => x.VTBegin).ThenBy(x => x.CorrectionNumber).FirstOrDefault()?.VTBegin;

            var endLifeTime = group.Where(x => x.LTEnd != null).FirstOrDefault()?.LTEnd;

            if (model.State == States.BASELINE)
            {
                port.VTEnd = endValidTime != null ? endValidTime : null;
                port.LTEnd = endLifeTime;
            }
            else
            {
                port.VTEnd = port.Interpretation == Delta.TempDelta ? port.VTEnd : (endValidTime != null ? endValidTime : null); 
                port.LTEnd = endLifeTime != null ? endLifeTime : null;
            }
        }
    }
}
