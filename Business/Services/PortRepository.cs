﻿using Business.Abstractions;
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
            var allPortevents = _dbContext.Ports.Where(x => x.Identifier == model.Identifier);

            var allFilteredPortevents = FilterEventsByOldCorrection(allPortevents.Where(x=>x.Interpretation == model.Interpretation));

            var editedPort = allFilteredPortevents.LastOrDefault();

            var newEdit = new PortOne
            {
                Identifier = model.Identifier,
                Name = model.Name,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                SequenceNumber = 0,
                CorrectionNumber = 0,
                LTBegin = allPortevents.First().LTBegin,
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
                var allEvents = FilterEvents(group, model);

                var toBeAppliedEvents = allEvents.Where(x => x.VTBegin <= model.EffectiveDate).ToList();

                if (toBeAppliedEvents.Count == 0)
                    continue;

                var portState = CreateAndApplyPort(toBeAppliedEvents);

                SetEndDate(model, portState, allEvents);

                if (portState.LTEnd != null && portState.LTEnd < model.EffectiveDate)
                    continue;

                var version = GetVersion(portState);

                result.Add(new PortDTO
                {
                    Id = portState.Id,
                    Identifier = portState.Identifier,
                    Name = portState.Name,
                    Latitude = portState.Latitude,
                    Longitude = portState.Longitude,
                    CertificationDate = portState.CertificationDate,
                    TimeSlice = version,
                });
            }

            return result;
        }

        public async Task DecommissionPort(PortDecommissionDTO model)
        {
            var portList = _dbContext.Ports.AsQueryable().Where(x=>x.Identifier == model.Identifier);
            
            var port = FilterEventsByOldCorrection(portList).Last();
            
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

        #region private methods
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
            if (port.Interpretation == Delta.PermDelta)
            {
                var version = $"{port.SequenceNumber}.{port.CorrectionNumber} BaseLine from {port.VTBegin}";

                if (port.VTEnd != null)
                    version += $" to {port.VTEnd}";

                else if (port.VTEnd == null && port.LTEnd != null)
                    version += $" to {port.LTEnd}";

                return version;
            }
            else
            {
                return $"{port.SequenceNumber}.{port.CorrectionNumber} SnapShot from {port.VTBegin} to {port.VTEnd}";
            }
        }

        private PortOne CreateAndApplyPort(IEnumerable<PortOne> appliedEvents)
        {
            var port = new PortOne();
            var orderedAppliedEvents = appliedEvents.OrderBy(x => x.Interpretation).ThenBy(x => x.VTBegin);

            foreach (var appliedEvent in orderedAppliedEvents)
            {
                var properties = typeof(PortOne).GetProperties();
                foreach(var property in properties)
                {
                    var appliedEventProperty = appliedEvent.GetType().GetProperty(property.Name);
                    if(appliedEventProperty != null)
                    {
                        var value = appliedEventProperty.GetValue(appliedEvent);
                        if (value != null)
                            property.SetValue(port, value);
                    }
                }

                port.Identifier = appliedEvent.Identifier;
                port.Interpretation = appliedEvent.Interpretation;
                port.SequenceNumber = appliedEvent.SequenceNumber;
                port.CorrectionNumber = appliedEvent.CorrectionNumber;

                port.VTBegin = appliedEvent.VTBegin;
                port.VTEnd = appliedEvent.VTEnd;
                port.LTBegin = appliedEvent.LTBegin;
                port.LTEnd = appliedEvent.LTEnd;
            }

            return port;
        }

        private void SetEndDate(PortSearchDTO model, PortOne port, IEnumerable<PortOne> ports)
        {
            var nextBeginValidTime = ports.OrderBy(x => x.VTBegin)
                                          .FirstOrDefault(x => x.VTBegin > model.EffectiveDate)?
                                          .VTBegin;

            var endLifeTime = ports.FirstOrDefault(x => x.LTEnd != null)?.LTEnd;

            if (endLifeTime != null)
            {
                if (nextBeginValidTime == null || nextBeginValidTime > endLifeTime)
                {
                    nextBeginValidTime = endLifeTime;
                }
            }

            port.LTEnd = endLifeTime;

            if (port.Interpretation == Delta.PermDelta || port.VTEnd > nextBeginValidTime)
            {
                port.VTEnd = nextBeginValidTime;
            }
        }

        private List<PortOne> FilterEvents(IEnumerable<PortOne> group, PortSearchDTO model)
        {
            var maxCorrectionEvents = FilterEventsByOldCorrection(group);

            var filteredEvents = FilterEventsByState(maxCorrectionEvents, model);

            return filteredEvents;
        }

        private List<PortOne> FilterEventsByOldCorrection(IEnumerable<PortOne> group)
        {
            var featureGroups = group.GroupBy(x => new { x.Interpretation, x.SequenceNumber }).ToList();
            var eventsList = new List<PortOne>();

            foreach (var featureGroup in featureGroups)
            {
                var featureEvent = featureGroup.OrderByDescending(x => x.CorrectionNumber).First();

                eventsList.Add(featureEvent);
            }

            return eventsList;
        }

        private List<PortOne> FilterEventsByState(IEnumerable<PortOne> group, PortSearchDTO model)
        {
            if (model.State == States.BASELINE)
                return group.Where(x => x.Interpretation == Delta.PermDelta).ToList();
            else
                return group.Where(x => x.VTEnd == null || x.VTEnd >= model.EffectiveDate).ToList();
        }

        #endregion
    }
}
