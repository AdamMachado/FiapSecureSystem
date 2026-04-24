├───Services
│   ├───ProcessingService
│   │   ├───ProcessingService.Application
│   │   ├───ProcessingService.Domain
│   │   │   ├───Entities
│   │   │   │       AnalysisProcess.cs
│   │   │   │
│   │   │   ├───Enums
│   │   │   │       ComponentDiscoverySource.cs
│   │   │   │       DiagramType.cs
│   │   │   │       ProcessingStatus.cs
│   │   │   │
│   │   │   ├───Events
│   │   │   │       AnalysisProcessingCompletedDomainEvent.cs
│   │   │   │       AnalysisProcessingFailedDomainEvent.cs
│   │   │   │       AnalysisProcessingStartedDomainEvent.cs
│   │   │   │
│   │   │   ├───Exceptions
│   │   │   │       AiAnalysisException.cs
│   │   │   │       DiagramProcessingException.cs
│   │   │   │       InvalidAnalysisResultException.cs
│   │   │   │       UnsupportedDiagramFormatException.cs
│   │   │   │
│   │   │   └───ValueObjects
│   │   │           AnalysisRequestId.cs
│   │   │           ExtractedText.cs
│   │   │           ProcessingResultSummary.cs
│   │   │           SourceFileLocation.cs
│   │   │
│   │   └───ProcessingService.Infrastructure
│   │ 
│   ├───ReportService
│   │   ├───ReportService.Api
│   │   │   │   appsettings.Development.json
│   │   │   │   appsettings.json
│   │   │   │   Dockerfile
│   │   │   │   Program.cs
│   │   │   │
│   │   │   ├───Configuration
│   │   │   │       ProblemDetailsExtensions.cs
│   │   │   │       SwaggerExtensions.cs
│   │   │   │
│   │   │   ├───Contracts
│   │   │   │   └───Requests
│   │   │   │           GenerateReportRequest.cs
│   │   │   │
│   │   │   ├───Controllers
│   │   │   │       ReportsController.cs
│   │   │   │
│   │   │   ├───DependencyInjection
│   │   │   │       ServiceCollectionExtensions.cs
│   │   │   │
│   │   │   ├───Middlewares
│   │   │   │       ExceptionHandlingMiddleware.cs
│   │   │   │
│   │   │   └───Responses
│   │   │           DownloadReportResponse.cs
│   │   │           GenerateReportResponse.cs
│   │   │           GetReportByAnalysisResponse.cs
│   │   │
│   │   ├───ReportService.Application
│   │   │   │   AssemblyReference.cs
│   │   │   │   DependencyInjection.cs
│   │   │   │
│   │   │   ├───Abstractions
│   │   │   │   ├───Clock
│   │   │   │   │       IDateTimeProvider.cs
│   │   │   │   │
│   │   │   │   ├───Messaging
│   │   │   │   │       IEventPublisher.cs
│   │   │   │   │       IIntegrationEventHandler.cs
│   │   │   │   │       IIntegrationEventMapper.cs
│   │   │   │   │
│   │   │   │   ├───Persistence
│   │   │   │   │       IAnalysisReportRepository.cs
│   │   │   │   │       IUnitOfWork.cs
│   │   │   │   │
│   │   │   │   ├───Rendering
│   │   │   │   │       IReportRenderer.cs
│   │   │   │   │
│   │   │   │   └───Storage
│   │   │   │           IReportStorage.cs
│   │   │   │
│   │   │   ├───Integration
│   │   │   │   ├───Consumed
│   │   │   │   │       AnalysisCompletedMessageHandler.cs
│   │   │   │   │
│   │   │   │   └───Published
│   │   │   │           ReportGeneratedIntegrationEventMapper.cs
│   │   │   │
│   │   │   ├───Mappings
│   │   │   │       AnalysisReportMappings.cs
│   │   │   │
│   │   │   └───UseCases
│   │   │       ├───DownloadReport
│   │   │       │       DownloadReportHandler.cs
│   │   │       │       DownloadReportQuery.cs
│   │   │       │       DownloadReportResult.cs
│   │   │       │
│   │   │       ├───GenerateReport
│   │   │       │       GenerateReportCommand.cs
│   │   │       │       GenerateReportHandler.cs
│   │   │       │       GenerateReportResult.cs
│   │   │       │       GenerateReportValidator.cs
│   │   │       │
│   │   │       ├───GetReportByAnalysis
│   │   │       │       GetReportByAnalysisHandler.cs
│   │   │       │       GetReportByAnalysisQuery.cs
│   │   │       │       GetReportByAnalysisResult.cs
│   │   │       │
│   │   │       └───UpdateReportStatus
│   │   │               UpdateReportStatusCommand.cs
│   │   │               UpdateReportStatusHandler.cs
│   │   │               UpdateReportStatusResult.cs
│   │   │
│   │   ├───ReportService.Domain
│   │   │   ├───Entities
│   │   │   │       AnalysisReport.cs
│   │   │   │
│   │   │   ├───Enums
│   │   │   │       ReportFormat.cs
│   │   │   │       ReportStatus.cs
│   │   │   │
│   │   │   ├───Events
│   │   │   │       ReportGeneratedDomainEvent.cs
│   │   │   │       ReportGenerationFailedDomainEvent.cs
│   │   │   │       ReportGenerationRequestedDomainEvent.cs
│   │   │   │
│   │   │   ├───Exceptions
│   │   │   │       EmptyReportContentException.cs
│   │   │   │       ReportGenerationException.cs
│   │   │   │       UnsupportedReportFormatException.cs
│   │   │   │
│   │   │   └───ValueObjects
│   │   │           AnalysisRequestId.cs
│   │   │           GeneratedFileLocation.cs
│   │   │           ReportContent.cs
│   │   │           ReportId.cs
│   │   │
│   │   └───ReportService.Infrastructure
│   │       ├───Clock
│   │       │       SystemDateTimeProvider.cs
│   │       │
│   │       ├───Configuration
│   │       │   │   DependencyInjection.cs
│   │       │   │
│   │       │   └───Options
│   │       │           DatabaseOptions.cs
│   │       │           RabbitMqOptions.cs
│   │       │           ReportOptions.cs
│   │       │           StorageOptions.cs
│   │       │
│   │       ├───Exceptions
│   │       │       MessagePublishException.cs
│   │       │       ReportRenderingException.cs
│   │       │       ReportStorageUnavailableException.cs
│   │       │
│   │       ├───HealthChecks
│   │       │       MinIoHealthCheck.cs
│   │       │       PostgreSqlHealthChecks.cs
│   │       │       RabbitMqHealthCheck.cs
│   │       │
│   │       ├───Messaging
│   │       │   │   QueueNames.cs
│   │       │   │
│   │       │   └───RabbitMq
│   │       │       │   RabbitMqPublisher.cs
│   │       │       │   RabbitMqSubscriberService.cs
│   │       │       │
│   │       │       └───Internals
│   │       │               RabbitMqChannel.cs
│   │       │               RabbitMqConsumerDescriptor.cs
│   │       │               RabbitMqMessageDispatcher.cs
│   │       │
│   │       ├───Migrations
│   │       │       20260421203333_InitialCreate.cs
│   │       │       20260421203333_InitialCreate.Designer.cs
│   │       │       ReportDbContextModelSnapshot.cs
│   │       │
│   │       ├───Persistence
│   │       │   ├───Configurations
│   │       │   │       AnalysisReportConfiguration.cs
│   │       │   │
│   │       │   ├───Context
│   │       │   │       ReportDbContext.cs
│   │       │   │
│   │       │   ├───Repositories
│   │       │   │       AnalysisReportRepository.cs
│   │       │   │
│   │       │   └───UnitOfWork
│   │       │           EfUnitOfWork.cs
│   │       │
│   │       ├───Rendering
│   │       │   ├───Json
│   │       │   │       JsonReportRenderer.cs
│   │       │   │
│   │       │   ├───Markdown
│   │       │   │       MarkdownReportRenderer.cs
│   │       │   │
│   │       │   └───Pdf
│   │       │           PdfReportRenderer.cs
│   │       │
│   │       └───Storage
│   │           └───MinIO
│   │                   MinIoOptions.cs
│   │                   MinIoReportStorage.cs
│   │
│   └───UploadService
│       ├───UploadService.Api
│       │   │   appsettings.Development.json
│       │   │   appsettings.json
│       │   │   Dockerfile
│       │   │   Program.cs
│       │   │
│       │   ├───Configuration
│       │   │       ProblemDetailsExtensions.cs
│       │   │       SwaggerExtensions.cs
│       │   │
│       │   ├───Contracts
│       │   │   ├───Requests
│       │   │   │       CreateAnalysisRequest.cs
│       │   │   │
│       │   │   └───Responses
│       │   │           CreateAnalysisResponse.cs
│       │   │           GetAnalysisStatusResponse.cs
│       │   │
│       │   ├───Controllers
│       │   │       AnalysesController.cs
│       │   │
│       │   ├───DependencyInjection
│       │   │       ServiceCollectionExtensions.cs
│       │   │
│       │   ├───Middlewares
│       │   │       ExceptionHandlingMiddleware.cs
│       │   │
│       │   └───Services
│       │           SystemDateTimeProvider.cs
│       │
│       ├───UploadService.Application
│       │   │   AssemblyReference.cs
│       │   │
│       │   ├───Abstractions
│       │   │   ├───Clock
│       │   │   │       IDateTimeProvider.cs
│       │   │   │
│       │   │   ├───Identity
│       │   │   │       IUserContext.cs
│       │   │   │
│       │   │   ├───Messaging
│       │   │   │       IEventPublisher.cs
│       │   │   │       IIntegrationEventHandler.cs
│       │   │   │       IIntegrationEventMapper.cs
│       │   │   │
│       │   │   ├───Persistence
│       │   │   │       IAnalysisRequestRepository.cs
│       │   │   │       IUnitOfWork.cs
│       │   │   │
│       │   │   └───Storage
│       │   │           IObjectStorage.cs
│       │   │           IStorageObjectKeyFactory.cs
│       │   │           IUploadPolicy.cs
│       │   │
│       │   ├───Integration
│       │   │   ├───Consumed
│       │   │   │       AnalysisCompletedMessageHandler.cs
│       │   │   │       AnalysisFailedMessageHandler.cs
│       │   │   │       AnalysisStartedMessageHandler.cs
│       │   │   │
│       │   │   └───Published
│       │   │           AnalysisRequestedIntegrationEventMapper.cs
│       │   │ 
│       │   └───UseCases
│       │       ├───CreateAnalysis
│       │       │       CreateAnalysisCommand.cs
│       │       │       CreateAnalysisHandler.cs
│       │       │       CreateAnalysisResult.cs
│       │       │       CreateAnalysisValidator.cs
│       │       │
│       │       ├───GetAnalysisStatus
│       │       │       GetAnalysisStatusHandler.cs
│       │       │       GetAnalysisStatusQuery.cs
│       │       │       GetAnalysisStatusResult.cs
│       │       │
│       │       └───UpdateAnalysisStatus
│       │               UpdateAnalysisStatusCommand.cs
│       │               UpdateAnalysisStatusHandler.cs
│       │               UpdateAnalysisStatusResult.cs
│       │
│       ├───UploadService.Domain
│       │   ├───Entities
│       │   │       AnalysisRequest.cs
│       │   │
│       │   ├───Enums
│       │   │       AnalysisStatus.cs
│       │   │       FileType.cs
│       │   │
│       │   ├───Events
│       │   │       AnalysisRequestCreatedDomainEvent.cs
│       │   │       AnalysisStatusChangedDomainEvent.cs
│       │   │
│       │   ├───Exceptions
│       │   │       EmptyUploadException.cs
│       │   │       FileSizeExceededException.cs
│       │   │       UnsupportedFileTypeException.cs
│       │   │
│       │   └───ValueObjects
│       │           FileHash.cs
│       │           FileMetadata.cs
│       │           StorageLocation.cs
│       │           StorageObjectKey.cs
│       │
│       └───UploadService.Infrastructure
│           │   Dockerfile.migrator
│           │   UploadService.Infrastructure.csproj
│           │
│           ├───bin
│           │   ├───Debug
│           │   │   └───net10.0
│           │   │       │   Shared.Contracts.dll
│           │   │       │   Shared.Contracts.pdb
│           │   │       │   Shared.Kernel.dll
│           │   │       │   Shared.Kernel.pdb
│           │   │       │   Shared.Observability.dll
│           │   │       │   Shared.Observability.pdb
│           │   │       │   UploadService.Application.dll
│           │   │       │   UploadService.Application.pdb
│           │   │       │   UploadService.Domain.dll
│           │   │       │   UploadService.Domain.pdb
│           │   │       │   UploadService.Infrastructure.deps.json
│           │   │       │   UploadService.Infrastructure.dll
│           │   │       │   UploadService.Infrastructure.pdb
│           │   │       │   UploadService.Infrastructure.runtimeconfig.json
│           │   │       │
│           │   │       ├───BuildHost-net472
│           │   │       │   │   Microsoft.Build.Locator.dll
│           │   │       │   │   Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.exe
│           │   │       │   │   Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.exe.config
│           │   │       │   │   Microsoft.IO.Redist.dll
│           │   │       │   │   Newtonsoft.Json.dll
│           │   │       │   │   System.Buffers.dll
│           │   │       │   │   System.Collections.Immutable.dll
│           │   │       │   │   System.CommandLine.dll
│           │   │       │   │   System.Memory.dll
│           │   │       │   │   System.Numerics.Vectors.dll
│           │   │       │   │   System.Runtime.CompilerServices.Unsafe.dll
│           │   │       │   │   System.Threading.Tasks.Extensions.dll
│           │   │       │   │
│           │   │       │   ├───cs
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───de
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───es
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───fr
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───it
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───ja
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───ko
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───pl
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───pt-BR
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───ru
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───tr
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   ├───zh-Hans
│           │   │       │   │       System.CommandLine.resources.dll
│           │   │       │   │
│           │   │       │   └───zh-Hant
│           │   │       │           System.CommandLine.resources.dll
│           │   │       │
│           │   │       └───BuildHost-netcore
│           │   │           │   Microsoft.Build.Locator.dll
│           │   │           │   Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.deps.json
│           │   │           │   Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll
│           │   │           │   Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.runtimeconfig.json
│           │   │           │   Newtonsoft.Json.dll
│           │   │           │   System.Collections.Immutable.dll
│           │   │           │   System.CommandLine.dll
│           │   │           │
│           │   │           ├───cs
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───de
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───es
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───fr
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───it
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───ja
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───ko
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───pl
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───pt-BR
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───ru
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───tr
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           ├───zh-Hans
│           │   │           │       System.CommandLine.resources.dll
│           │   │           │
│           │   │           └───zh-Hant
│           │   │                   System.CommandLine.resources.dll
│           │   │
│           │   └───Release
│           │       └───net10.0
│           ├───Configuration
│           │   │   DependencyInjection.cs
│           │   │   UploadPolicy.cs
│           │   │
│           │   └───Options
│           │           DatabaseOptions.cs
│           │           RabbitMqOptions.cs
│           │           StorageOptions.cs
│           │           UploadOptions.cs
│           │
│           ├───Exceptions
│           │       MessagePublishException.cs
│           │       StorageUnavailableException.cs
│           │
│           ├───HealthChecks
│           │       MinIoHealthCheck.cs
│           │       PostgreSqlHealthChecks.cs
│           │       RabbitMqHealthCheck.cs
│           │
│           ├───Identity
│           │       StubUserContext.cs
│           │
│           ├───Messaging
│           │   │   QueueNames.cs
│           │   │
│           │   └───RabbitMq
│           │       │   RabbitMqMessageDispatcher.cs
│           │       │   RabbitMqPublisher.cs
│           │       │   RabbitMqSubscriberService.cs
│           │       │
│           │       └───Internals
│           │               RabbitMqChannel.cs
│           │               RabbitMqConsumerDescriptor.cs
│           │
│           ├───obj
│           │   │   project.assets.json
│           │   │   project.nuget.cache
│           │   │   UploadService.Infrastructure.csproj.nuget.dgspec.json
│           │   │   UploadService.Infrastructure.csproj.nuget.g.props
│           │   │   UploadService.Infrastructure.csproj.nuget.g.targets
│           │   │
│           │   ├───Debug
│           │   │   └───net10.0
│           │   │       │   .NETCoreApp,Version=v10.0.AssemblyAttributes.cs
│           │   │       │   UploadSe.A6DCD3F3.Up2Date
│           │   │       │   UploadService.Infrastructure.AssemblyInfo.cs
│           │   │       │   UploadService.Infrastructure.AssemblyInfoInputs.cache
│           │   │       │   UploadService.Infrastructure.assets.cache
│           │   │       │   UploadService.Infrastructure.csproj.AssemblyReference.cache
│           │   │       │   UploadService.Infrastructure.csproj.CoreCompileInputs.cache
│           │   │       │   UploadService.Infrastructure.csproj.FileListAbsolute.txt
│           │   │       │   UploadService.Infrastructure.dll
│           │   │       │   UploadService.Infrastructure.GeneratedMSBuildEditorConfig.editorconfig
│           │   │       │   UploadService.Infrastructure.genruntimeconfig.cache
│           │   │       │   UploadService.Infrastructure.GlobalUsings.g.cs
│           │   │       │   UploadService.Infrastructure.pdb
│           │   │       │   UploadService.Infrastructure.sourcelink.json
│           │   │       │
│           │   │       ├───ref
│           │   │       │       UploadService.Infrastructure.dll
│           │   │       │
│           │   │       └───refint
│           │   │               UploadService.Infrastructure.dll
│           │   │
│           │   └───Release
│           │       └───net10.0
│           │           │   .NETCoreApp,Version=v10.0.AssemblyAttributes.cs
│           │           │   UploadService.Infrastructure.AssemblyInfo.cs
│           │           │   UploadService.Infrastructure.AssemblyInfoInputs.cache
│           │           │   UploadService.Infrastructure.assets.cache
│           │           │   UploadService.Infrastructure.GeneratedMSBuildEditorConfig.editorconfig
│           │           │   UploadService.Infrastructure.GlobalUsings.g.cs
│           │           │
│           │           ├───ref
│           │           └───refint
│           ├───Persistence
│           │   │   AnalysisRequestConfiguration.cs
│           │   │   AnalysisRequestRepository.cs
│           │   │   EfUnitOfWork.cs
│           │   │   UploadDbContext.cs
│           │   │   UploadDbContextFactory.cs
│           │   │
│           │   └───Migrations
│           │           20260420152230_InitialUploadSchema.cs
│           │           20260420152230_InitialUploadSchema.Designer.cs
│           │           UploadDbContextModelSnapshot.cs
│           │
│           └───Storage
│               │   StorageObjectKeyFactory.cs
│               │
│               └───MinIO
│                       MinIoObjectStorage.cs
│                       MinIoOptions.cs
│
└───Shared
    ├───bin
    │   └───Debug
    │       └───net10.0
    ├───obj
    │   │   project.assets.json
    │   │   project.nuget.cache
    │   │   Shared.csproj.nuget.dgspec.json
    │   │   Shared.csproj.nuget.g.props
    │   │   Shared.csproj.nuget.g.targets
    │   │
    │   └───Debug
    │       └───net10.0
    │           │   .NETCoreApp,Version=v10.0.AssemblyAttributes.cs
    │           │   Shared.AssemblyInfo.cs
    │           │   Shared.AssemblyInfoInputs.cache
    │           │   Shared.assets.cache
    │           │   Shared.csproj.AssemblyReference.cache
    │           │   Shared.GeneratedMSBuildEditorConfig.editorconfig
    │           │   Shared.GlobalUsings.g.cs
    │           │
    │           ├───ref
    │           └───refint
    ├───Shared.Contracts
    │   │   Shared.Contracts.csproj
    │   │
    │   ├───bin
    │   │   └───Debug
    │   │       └───net10.0
    │   │               Shared.Contracts.deps.json
    │   │               Shared.Contracts.dll
    │   │               Shared.Contracts.pdb
    │   │
    │   ├───IntegrationEvents
    │   │   │   AnalysisCompletedIntegrationEvent.cs
    │   │   │   AnalysisFailedIntegrationEvent.cs
    │   │   │   AnalysisRequestedIntegrationEvent.cs
    │   │   │   AnalysisStartedIntegrationEvent.cs
    │   │   │   ReportGeneratedIntegrationEvent.cs
    │   │   │
    │   │   ├───Abstractions
    │   │   │       IntegrationEventBase.cs
    │   │   │
    │   │   ├───Enums
    │   │   │       ComponentType.cs
    │   │   │       RecommendationCategory.cs
    │   │   │       RiskSeverityLevel.cs
    │   │   │
    │   │   └───Schemas
    │   │           AnalysisResultDto.cs
    │   │           AnalysisSummaryDto.cs
    │   │           ArchitecturalRecommendationDto.cs
    │   │           ArchitecturalRiskDto.cs
    │   │           IdentifiedComponentDto.cs
    │   │
    │   ├───Messaging
    │   │       ExchangeNames.cs
    │   │       HeaderNames.cs
    │   │       RoutingKeys.cs
    │   │
    │   └───obj
    │       │   project.assets.json
    │       │   project.nuget.cache
    │       │   Shared.Contracts.csproj.nuget.dgspec.json
    │       │   Shared.Contracts.csproj.nuget.g.props
    │       │   Shared.Contracts.csproj.nuget.g.targets
    │       │
    │       └───Debug
    │           └───net10.0
    │               │   .NETCoreApp,Version=v10.0.AssemblyAttributes.cs
    │               │   Shared.Contracts.AssemblyInfo.cs
    │               │   Shared.Contracts.AssemblyInfoInputs.cache
    │               │   Shared.Contracts.assets.cache
    │               │   Shared.Contracts.csproj.CoreCompileInputs.cache
    │               │   Shared.Contracts.csproj.FileListAbsolute.txt
    │               │   Shared.Contracts.dll
    │               │   Shared.Contracts.GeneratedMSBuildEditorConfig.editorconfig
    │               │   Shared.Contracts.GlobalUsings.g.cs
    │               │   Shared.Contracts.pdb
    │               │   Shared.Contracts.sourcelink.json
    │               │
    │               ├───ref
    │               │       Shared.Contracts.dll
    │               │
    │               └───refint
    │                       Shared.Contracts.dll
    │
    ├───Shared.Kernel
    │   │   Shared.Kernel.csproj
    │   │
    │   ├───bin
    │   │   └───Debug
    │   │       └───net10.0
    │   │               Shared.Kernel.deps.json
    │   │               Shared.Kernel.dll
    │   │               Shared.Kernel.pdb
    │   │
    │   ├───Exceptions
    │   │       AppException.cs
    │   │       DomainException.cs
    │   │       NotFoundException.cs
    │   │       ValidationException.cs
    │   │
    │   ├───obj
    │   │   │   project.assets.json
    │   │   │   project.nuget.cache
    │   │   │   Shared.Kernel.csproj.nuget.dgspec.json
    │   │   │   Shared.Kernel.csproj.nuget.g.props
    │   │   │   Shared.Kernel.csproj.nuget.g.targets
    │   │   │
    │   │   └───Debug
    │   │       └───net10.0
    │   │           │   .NETCoreApp,Version=v10.0.AssemblyAttributes.cs
    │   │           │   Shared.Kernel.AssemblyInfo.cs
    │   │           │   Shared.Kernel.AssemblyInfoInputs.cache
    │   │           │   Shared.Kernel.assets.cache
    │   │           │   Shared.Kernel.csproj.CoreCompileInputs.cache
    │   │           │   Shared.Kernel.csproj.FileListAbsolute.txt
    │   │           │   Shared.Kernel.dll
    │   │           │   Shared.Kernel.GeneratedMSBuildEditorConfig.editorconfig
    │   │           │   Shared.Kernel.GlobalUsings.g.cs
    │   │           │   Shared.Kernel.pdb
    │   │           │   Shared.Kernel.sourcelink.json
    │   │           │
    │   │           ├───ref
    │   │           │       Shared.Kernel.dll
    │   │           │
    │   │           └───refint
    │   │                   Shared.Kernel.dll
    │   │
    │   ├───Pagination
    │   │       PagedResult.cs
    │   │       PaginationParams.cs
    │   │
    │   ├───Primitives
    │   │       AggregateRoot.cs
    │   │       DomainEvent.cs
    │   │       Entity.cs
    │   │       ValueObject.cs
    │   │
    │   └───Result
    │           Error.cs
    │           ErrorType.cs
    │           Result.cs
    │
    └───Shared.Observability
        │   Shared.Observability.csproj
        │
        ├───bin
        │   └───Debug
        │       └───net10.0
        │               Shared.Contracts.dll
        │               Shared.Contracts.pdb
        │               Shared.Observability.deps.json
        │               Shared.Observability.dll
        │               Shared.Observability.pdb
        │
        ├───Correlation
        │       CorrelationContextAccessor.cs
        │       CorrelationMiddlewareExtension.cs
        │       ICorrelationContextAccessor.cs
        │
        ├───HealthChecks
        │       HealthCheckExtensions.cs
        │
        ├───Logging
        │       LogEnrichers.cs
        │       SerilogExtensions.cs
        │
        ├───Messaging
        │       MessageCorrelationContext.cs
        │       MessageCorrelationExtensions.cs
        │
        ├───obj
        │   │   project.assets.json
        │   │   project.nuget.cache
        │   │   Shared.Observability.csproj.nuget.dgspec.json
        │   │   Shared.Observability.csproj.nuget.g.props
        │   │   Shared.Observability.csproj.nuget.g.targets
        │   │
        │   └───Debug
        │       └───net10.0
        │           │   .NETCoreApp,Version=v10.0.AssemblyAttributes.cs
        │           │   Shared.O.7AB68918.Up2Date
        │           │   Shared.Observability.AssemblyInfo.cs
        │           │   Shared.Observability.AssemblyInfoInputs.cache
        │           │   Shared.Observability.assets.cache
        │           │   Shared.Observability.csproj.AssemblyReference.cache
        │           │   Shared.Observability.csproj.CoreCompileInputs.cache
        │           │   Shared.Observability.csproj.FileListAbsolute.txt
        │           │   Shared.Observability.dll
        │           │   Shared.Observability.GeneratedMSBuildEditorConfig.editorconfig
        │           │   Shared.Observability.GlobalUsings.g.cs
        │           │   Shared.Observability.pdb
        │           │   Shared.Observability.sourcelink.json
        │           │
        │           ├───ref
        │           │       Shared.Observability.dll
        │           │
        │           └───refint
        │                   Shared.Observability.dll
        │
        └───Telemetry
                ActivitySources.cs
                MetricNames.cs
                OpenTelemetryExtensions.cs
                TelemetryConstants.cs