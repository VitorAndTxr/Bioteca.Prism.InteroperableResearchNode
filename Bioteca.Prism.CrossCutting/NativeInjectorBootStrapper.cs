﻿using Bioteca.Prism.Core.Cache;
using Bioteca.Prism.Core.Cache.Session;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Core.Security.Cryptography;
using Bioteca.Prism.Data.Interfaces.Application;
using Bioteca.Prism.Data.Interfaces.Device;
using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Data.Interfaces.Sensor;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Data.Interfaces.Volunteer;
using Bioteca.Prism.Data.Repositories.Application;
using Bioteca.Prism.Data.Repositories.Device;
using Bioteca.Prism.Data.Repositories.Node;
using Bioteca.Prism.Data.Repositories.Record;
using Bioteca.Prism.Data.Repositories.Research;
using Bioteca.Prism.Data.Repositories.Researcher;
using Bioteca.Prism.Data.Repositories.Sensor;
using Bioteca.Prism.Data.Repositories.Snomed;
using Bioteca.Prism.Data.Repositories.User;
using Bioteca.Prism.Data.Repositories.Volunteer;
using Bioteca.Prism.Service.Interfaces.Application;
using Bioteca.Prism.Service.Interfaces.Clinical;
using Bioteca.Prism.Service.Interfaces.Device;
using Bioteca.Prism.Service.Interfaces.Record;
using Bioteca.Prism.Service.Interfaces.Research;
using Bioteca.Prism.Service.Interfaces.Researcher;
using Bioteca.Prism.Service.Interfaces.Sensor;
using Bioteca.Prism.Service.Interfaces.Snomed;
using Bioteca.Prism.Service.Interfaces.Volunteer;
using Bioteca.Prism.Service.Services.Application;
using Bioteca.Prism.Service.Services.Cache;
using Bioteca.Prism.Service.Services.Clinical;
using Bioteca.Prism.Service.Services.Device;
using Bioteca.Prism.Service.Services.Node;
using Bioteca.Prism.Service.Services.Record;
using Bioteca.Prism.Service.Services.Research;
using Bioteca.Prism.Service.Services.Researcher;
using Bioteca.Prism.Service.Services.Sensor;
using Bioteca.Prism.Service.Services.Session;
using Bioteca.Prism.Service.Services.Snomed; 
using Bioteca.Prism.Service.Services.Volunteer;
using Microsoft.Extensions.DependencyInjection;

namespace Bioteca.Prism.CrossCutting
{
    public class NativeInjectorBootStrapper
    {

        public static void RegisterAllDependencies(IServiceCollection services)
        {
            RegisterCache(services);
            RegisterDatabase(services);
            RegisterRepositories(services);
            RegisterServices(services);
        }

        public static void RegisterServices(IServiceCollection services)
        {

            // Register Phase 1 services (Channel Establishment)
            services.AddSingleton<IEphemeralKeyService, EphemeralKeyService>();
            services.AddSingleton<IChannelEncryptionService, ChannelEncryptionService>();

            // Register Phase 3 services (Mutual Authentication)
            services.AddSingleton<IChallengeService, ChallengeService>();

            services.AddSingleton<IChannelStore, RedisChannelStore>();
            services.AddSingleton<ISessionStore, RedisSessionStore>();
            services.AddSingleton<ISessionService, SessionService>();

            // Research data services
            services.AddScoped<IResearchService, ResearchService>();
            services.AddScoped<IVolunteerService, VolunteerService>();
            services.AddScoped<IResearcherService, ResearcherService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<ISensorService, SensorService>();

            // Record services
            services.AddScoped<IRecordSessionService, RecordSessionService>();
            services.AddScoped<IRecordService, RecordService>();
            services.AddScoped<IRecordChannelService, RecordChannelService>();
            services.AddScoped<ITargetAreaService, TargetAreaService>();

            // SNOMED CT services
            services.AddScoped<ISnomedLateralityService, SnomedLateralityService>();
            services.AddScoped<ISnomedTopographicalModifierService, SnomedTopographicalModifierService>();
            services.AddScoped<ISnomedBodyRegionService, SnomedBodyRegionService>();
            services.AddScoped<ISnomedBodyStructureService, SnomedBodyStructureService>();

            // Clinical services
            services.AddScoped<IClinicalConditionService, ClinicalConditionService>();
            services.AddScoped<IClinicalEventService, ClinicalEventService>();
            services.AddScoped<IMedicationService, MedicationService>();
            services.AddScoped<IAllergyIntoleranceService, AllergyIntoleranceService>();
            services.AddScoped<IVitalSignsService, VitalSignsService>();
            services.AddScoped<IVolunteerClinicalService, VolunteerClinicalService>();

            // Register PostgreSQL-backed node registry service
            services.AddScoped<IResearchNodeService, ResearchNodeService>();
        }


        public static void RegisterRepositories(IServiceCollection services)
        {
            // Register repositories
            services.AddScoped<INodeRepository, NodeRepository>();

            // Research data repositories
            services.AddScoped<IResearchRepository, ResearchRepository>();
            services.AddScoped<IVolunteerRepository, VolunteerRepository>();
            services.AddScoped<IResearcherRepository, ResearcherRepository>();
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<ISensorRepository, SensorRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Record repositories
            services.AddScoped<IRecordSessionRepository, RecordSessionRepository>();
            services.AddScoped<IRecordRepository, RecordRepository>();
            services.AddScoped<IRecordChannelRepository, RecordChannelRepository>();
            services.AddScoped<ITargetAreaRepository, TargetAreaRepository>();

            // SNOMED CT repositories
            services.AddScoped<ISnomedLateralityRepository, SnomedLateralityRepository>();
            services.AddScoped<ISnomedTopographicalModifierRepository, SnomedTopographicalModifierRepository>();
            services.AddScoped<ISnomedBodyRegionRepository, SnomedBodyRegionRepository>();
            services.AddScoped<ISnomedBodyStructureRepository, SnomedBodyStructureRepository>();

            // Clinical repositories
            services.AddScoped<IResearchResearcherRepository, ResearchResearcherRepository>();
        }

        public static void RegisterDatabase(IServiceCollection services)
        {

        }

        public static void RegisterCache(IServiceCollection services)
        {
            services.AddSingleton<IRedisConnectionService, RedisConnectionService>();
        }
    }
}
