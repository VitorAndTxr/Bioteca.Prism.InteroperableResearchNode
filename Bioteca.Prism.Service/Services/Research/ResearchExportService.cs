using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Service.Interfaces.Research;
using Bioteca.Prism.Service.Interfaces.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Service.Services.Research;

/// <summary>
/// Exports a complete research project as a ZIP archive with JSON entity files and signal data.
/// Uses separate queries per entity group to avoid Cartesian explosion.
/// All entities are projected to anonymous objects to prevent circular reference issues
/// from EF Core bidirectional navigation properties.
/// </summary>
public class ResearchExportService : IResearchExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly PrismDbContext _context;
    private readonly ISyncExportService _syncExportService;
    private readonly ILogger<ResearchExportService> _logger;

    public ResearchExportService(
        PrismDbContext context,
        ISyncExportService syncExportService,
        ILogger<ResearchExportService> logger)
    {
        _context = context;
        _syncExportService = syncExportService;
        _logger = logger;
    }

    public async Task<ResearchExportResult> ExportAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        var research = await _context.Research
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == researchId, cancellationToken);

        if (research == null)
            throw new KeyNotFoundException($"Research with ID {researchId} not found");

        var researchResearchers = await _context.Research
            .AsNoTracking()
            .Where(r => r.Id == researchId)
            .SelectMany(r => r.ResearchResearchers)
            .Include(rr => rr.Researcher)
            .ToListAsync(cancellationToken);

        var researchVolunteers = await _context.Research
            .AsNoTracking()
            .Where(r => r.Id == researchId)
            .SelectMany(r => r.ResearchVolunteers)
            .Include(rv => rv.Volunteer)
                .ThenInclude(v => v.ClinicalConditions)
            .Include(rv => rv.Volunteer)
                .ThenInclude(v => v.ClinicalEvents)
            .Include(rv => rv.Volunteer)
                .ThenInclude(v => v.Medications)
            .Include(rv => rv.Volunteer)
                .ThenInclude(v => v.AllergyIntolerances)
            .Include(rv => rv.Volunteer)
                .ThenInclude(v => v.VitalSigns)
            .ToListAsync(cancellationToken);

        var applications = await _context.Research
            .AsNoTracking()
            .Where(r => r.Id == researchId)
            .SelectMany(r => r.Applications)
            .ToListAsync(cancellationToken);

        var researchDevices = await _context.Research
            .AsNoTracking()
            .Where(r => r.Id == researchId)
            .SelectMany(r => r.ResearchDevices)
            .Include(rd => rd.Device)
                .ThenInclude(d => d.Sensors)
            .ToListAsync(cancellationToken);

        var volunteerIds = researchVolunteers.Select(rv => rv.VolunteerId).ToHashSet();

        var sessions = await _context.RecordSessions
            .AsNoTracking()
            .Where(s => s.ResearchId == researchId || volunteerIds.Contains(s.VolunteerId))
            .Include(s => s.SessionAnnotations)
            .Include(s => s.ClinicalEvents)
            .Include(s => s.VitalSigns)
            .Include(s => s.Records)
                .ThenInclude(r => r.RecordChannels)
                    .ThenInclude(rc => rc.TargetAreas)
            .ToListAsync(cancellationToken);

        // Sanitize title for ZIP root folder name
        var safeTitle = new string(research.Title
            .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)
            .ToArray());
        if (safeTitle.Length > 50) safeTitle = safeTitle[..50];
        var rootFolder = $"{safeTitle}_{researchId}";
        var zipFileName = $"{safeTitle}_{researchId}.zip";

        var missingFiles = new List<string>();
        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // research.json
            WriteJsonEntry(archive, $"{rootFolder}/research.json", new
            {
                research.Id,
                research.Title,
                research.Description,
                research.Status,
                research.StartDate,
                research.EndDate,
                research.CreatedAt,
                research.UpdatedAt
            });

            // researchers/researchers.json
            WriteJsonEntry(archive, $"{rootFolder}/researchers/researchers.json",
                researchResearchers.Select(rr => new
                {
                    rr.Researcher.ResearcherId,
                    rr.Researcher.Name,
                    rr.Researcher.Email,
                    rr.Researcher.Institution,
                    rr.Researcher.Role,
                    rr.Researcher.Orcid,
                    rr.Researcher.CreatedAt,
                    rr.Researcher.UpdatedAt,
                    rr.IsPrincipal,
                    rr.AssignedAt,
                    rr.RemovedAt
                }).ToList());

            // volunteers/volunteers.json
            WriteJsonEntry(archive, $"{rootFolder}/volunteers/volunteers.json",
                researchVolunteers.Select(rv => new
                {
                    rv.Volunteer.VolunteerId,
                    rv.Volunteer.VolunteerCode,
                    rv.Volunteer.Name,
                    rv.Volunteer.Email,
                    rv.Volunteer.BirthDate,
                    rv.Volunteer.Gender,
                    rv.Volunteer.BloodType,
                    rv.Volunteer.Height,
                    rv.Volunteer.Weight,
                    rv.Volunteer.MedicalHistory,
                    rv.Volunteer.ConsentStatus,
                    rv.Volunteer.EnrolledAt,
                    rv.Volunteer.UpdatedAt,
                    rv.EnrollmentStatus,
                    rv.ConsentDate,
                    rv.ConsentVersion,
                    rv.ExclusionReason
                }).ToList());

            // per-volunteer clinical sub-entities
            foreach (var rv in researchVolunteers)
            {
                var v = rv.Volunteer;
                var vPath = $"{rootFolder}/volunteers/{v.VolunteerId}";

                WriteJsonEntry(archive, $"{vPath}/clinical_conditions.json",
                    v.ClinicalConditions.Select(cc => new
                    {
                        cc.Id, cc.VolunteerId, cc.SnomedCode, cc.ClinicalStatus,
                        cc.OnsetDate, cc.AbatementDate, cc.SeverityCode,
                        cc.VerificationStatus, cc.ClinicalNotes, cc.RecordedBy,
                        cc.CreatedAt, cc.UpdatedAt
                    }).ToList());

                WriteJsonEntry(archive, $"{vPath}/clinical_events.json",
                    v.ClinicalEvents.Select(ce => new
                    {
                        ce.Id, ce.VolunteerId, ce.EventType, ce.EventDatetime,
                        ce.SnomedCode, ce.DurationMinutes, ce.SeverityCode,
                        ce.NumericValue, ce.ValueUnit, ce.Characteristics,
                        ce.TargetAreaId, ce.RecordSessionId, ce.RecordedBy,
                        ce.CreatedAt, ce.UpdatedAt
                    }).ToList());

                WriteJsonEntry(archive, $"{vPath}/medications.json",
                    v.Medications.Select(m => new
                    {
                        m.Id, m.VolunteerId, m.MedicationSnomedCode, m.ConditionId,
                        m.Dosage, m.Frequency, m.Route, m.StartDate, m.EndDate,
                        m.Status, m.Notes, m.RecordedBy, m.CreatedAt, m.UpdatedAt
                    }).ToList());

                WriteJsonEntry(archive, $"{vPath}/allergies.json",
                    v.AllergyIntolerances.Select(a => new
                    {
                        a.Id, a.VolunteerId, a.AllergyIntoleranceSnomedCode,
                        a.Criticality, a.ClinicalStatus, a.Manifestations,
                        a.OnsetDate, a.LastOccurrence, a.VerificationStatus,
                        a.RecordedBy, a.CreatedAt, a.UpdatedAt
                    }).ToList());

                WriteJsonEntry(archive, $"{vPath}/vital_signs.json",
                    v.VitalSigns.Select(vs => new
                    {
                        vs.Id, vs.VolunteerId, vs.RecordSessionId,
                        vs.MeasurementDatetime, vs.SystolicBp, vs.DiastolicBp,
                        vs.HeartRate, vs.RespiratoryRate, vs.Temperature,
                        vs.OxygenSaturation, vs.Weight, vs.Height, vs.Bmi,
                        vs.MeasurementContext, vs.RecordedBy,
                        vs.CreatedAt, vs.UpdatedAt
                    }).ToList());
            }

            // applications/applications.json
            WriteJsonEntry(archive, $"{rootFolder}/applications/applications.json",
                applications.Select(a => new
                {
                    a.ApplicationId, a.ResearchId, a.AppName, a.Url,
                    a.Description, a.AdditionalInfo, a.CreatedAt, a.UpdatedAt
                }).ToList());

            // devices/devices.json
            WriteJsonEntry(archive, $"{rootFolder}/devices/devices.json",
                researchDevices.Select(rd => new
                {
                    rd.Device.DeviceId,
                    rd.Device.DeviceName,
                    rd.Device.Manufacturer,
                    rd.Device.Model,
                    rd.Device.AdditionalInfo,
                    rd.Device.CreatedAt,
                    rd.Device.UpdatedAt,
                    rd.Role,
                    rd.AddedAt,
                    rd.RemovedAt,
                    rd.CalibrationStatus,
                    rd.LastCalibrationDate,
                    Sensors = rd.Device.Sensors.Select(s => new
                    {
                        s.SensorId, s.DeviceId, s.SensorName, s.MaxSamplingRate,
                        s.Unit, s.MinRange, s.MaxRange, s.Accuracy,
                        s.AdditionalInfo, s.CreatedAt, s.UpdatedAt
                    }).ToList()
                }).ToList());

            // per-device sensors.json
            foreach (var rd in researchDevices)
            {
                var d = rd.Device;
                WriteJsonEntry(archive, $"{rootFolder}/devices/{d.DeviceId}/sensors.json",
                    d.Sensors.Select(s => new
                    {
                        s.SensorId, s.DeviceId, s.SensorName, s.MaxSamplingRate,
                        s.Unit, s.MinRange, s.MaxRange, s.Accuracy,
                        s.AdditionalInfo, s.CreatedAt, s.UpdatedAt
                    }).ToList());
            }

            // sessions
            foreach (var session in sessions)
            {
                var sPath = $"{rootFolder}/sessions/{session.Id}";

                WriteJsonEntry(archive, $"{sPath}/session.json", new
                {
                    session.Id,
                    session.ResearchId,
                    session.VolunteerId,
                    session.ClinicalContext,
                    session.StartAt,
                    session.FinishedAt,
                    session.CreatedAt,
                    session.UpdatedAt,
                    Annotations = session.SessionAnnotations.Select(sa => new
                    {
                        sa.Id, sa.RecordSessionId, sa.Text,
                        sa.CreatedAt, sa.UpdatedAt
                    }).ToList()
                });

                WriteJsonEntry(archive, $"{sPath}/clinical_events.json",
                    session.ClinicalEvents.Select(ce => new
                    {
                        ce.Id, ce.VolunteerId, ce.EventType, ce.EventDatetime,
                        ce.SnomedCode, ce.DurationMinutes, ce.SeverityCode,
                        ce.NumericValue, ce.ValueUnit, ce.Characteristics,
                        ce.TargetAreaId, ce.RecordSessionId, ce.RecordedBy,
                        ce.CreatedAt, ce.UpdatedAt
                    }).ToList());

                WriteJsonEntry(archive, $"{sPath}/vital_signs.json",
                    session.VitalSigns.Select(vs => new
                    {
                        vs.Id, vs.VolunteerId, vs.RecordSessionId,
                        vs.MeasurementDatetime, vs.SystolicBp, vs.DiastolicBp,
                        vs.HeartRate, vs.RespiratoryRate, vs.Temperature,
                        vs.OxygenSaturation, vs.Weight, vs.Height, vs.Bmi,
                        vs.MeasurementContext, vs.RecordedBy,
                        vs.CreatedAt, vs.UpdatedAt
                    }).ToList());

                // records + channels + signal files
                foreach (var record in session.Records)
                {
                    var rPath = $"{sPath}/records/{record.Id}";

                    WriteJsonEntry(archive, $"{rPath}/record.json", new
                    {
                        record.Id,
                        record.RecordSessionId,
                        record.CollectionDate,
                        record.SessionId,
                        record.RecordType,
                        record.Notes,
                        record.CreatedAt,
                        record.UpdatedAt,
                        Channels = record.RecordChannels.Select(ch => new
                        {
                            ch.Id, ch.RecordId, ch.SensorId, ch.SignalType,
                            ch.FileUrl, ch.SamplingRate, ch.SamplesCount,
                            ch.StartTimestamp, ch.CreatedAt, ch.UpdatedAt,
                            TargetAreas = ch.TargetAreas.Select(ta => new
                            {
                                ta.Id, ta.RecordChannelId, ta.BodyStructureCode,
                                ta.LateralityCode, ta.TopographicalModifierCode,
                                ta.Notes, ta.CreatedAt, ta.UpdatedAt
                            }).ToList()
                        }).ToList()
                    });

                    foreach (var channel in record.RecordChannels)
                    {
                        var cPath = $"{rPath}/channels/{channel.Id}";

                        WriteJsonEntry(archive, $"{cPath}/channel.json", new
                        {
                            channel.Id,
                            channel.RecordId,
                            channel.SensorId,
                            channel.SignalType,
                            channel.FileUrl,
                            channel.SamplingRate,
                            channel.SamplesCount,
                            channel.StartTimestamp,
                            channel.CreatedAt,
                            channel.UpdatedAt,
                            TargetAreas = channel.TargetAreas.Select(ta => new
                            {
                                ta.Id, ta.RecordChannelId, ta.BodyStructureCode,
                                ta.LateralityCode, ta.TopographicalModifierCode,
                                ta.Notes, ta.CreatedAt, ta.UpdatedAt
                            }).ToList()
                        });

                        if (!string.IsNullOrEmpty(channel.FileUrl))
                        {
                            await WriteSignalFileAsync(archive, channel.Id, channel.FileUrl,
                                cPath, missingFiles, cancellationToken);
                        }
                    }
                }
            }

            WriteJsonEntry(archive, $"{rootFolder}/_missing_files.json", missingFiles);
        }

        memoryStream.Position = 0;

        return new ResearchExportResult
        {
            ZipStream = memoryStream,
            FileName = zipFileName
        };
    }

    private static void WriteJsonEntry(ZipArchive archive, string entryName, object data)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(JsonSerializer.Serialize(data, JsonOptions));
    }

    private async Task WriteSignalFileAsync(
        ZipArchive archive,
        Guid channelId,
        string fileUrl,
        string channelPath,
        List<string> missingFiles,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileResult = await _syncExportService.GetRecordingFileAsync(channelId, cancellationToken);

            if (fileResult == null)
            {
                _logger.LogWarning("Signal file not found for channel {ChannelId}: {FileUrl}", channelId, fileUrl);
                missingFiles.Add(channelPath);
                return;
            }

            var extension = Path.GetExtension(fileResult.Value.fileName);
            if (string.IsNullOrEmpty(extension)) extension = ".csv";

            var signalEntry = archive.CreateEntry($"{channelPath}/signal{extension}", CompressionLevel.Optimal);
            using var entryStream = signalEntry.Open();
            await entryStream.WriteAsync(fileResult.Value.data, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download signal file for channel {ChannelId}: {FileUrl}", channelId, fileUrl);
            missingFiles.Add(channelPath);
        }
    }
}
