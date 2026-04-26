Services/
  ProcessingService/
    ProcessingService.Application/
      AssemblyReference.cs
      DependencyInjection.cs

      Abstractions/
        AI/
          IArchitectureAnalyzer.cs

        Clock/
          IDateTimeProvider.cs

        Messaging/
          IEventPublisher.cs
          IIntegrationEventHandler.cs
          IIntegrationEventMapper.cs

        Persistence/
          IAnalysisProcessRepository.cs
          IUnitOfWork.cs

        Storage/
          IObjectStorage.cs

      Integration/
        Consumed/
          AnalysisRequestedMessageHandler.cs

        Published/
          AnalysisCompletedIntegrationEventMapper.cs
          AnalysisFailedIntegrationEventMapper.cs
          AnalysisStartedIntegrationEventMapper.cs

      Mappings/
        AnalysisProcessMappings.cs

      UseCases/
        CompleteAnalysisProcessing/
          CompleteAnalysisProcessingCommand.cs
          CompleteAnalysisProcessingHandler.cs
          CompleteAnalysisProcessingResult.cs

        FailAnalysisProcessing/
          FailAnalysisProcessingCommand.cs
          FailAnalysisProcessingHandler.cs
          FailAnalysisProcessingResult.cs

        GetProcessingResult/
          GetProcessingResultHandler.cs
          GetProcessingResultQuery.cs
          GetProcessingResultResult.cs

        StartAnalysisProcessing/
          StartAnalysisProcessingCommand.cs
          StartAnalysisProcessingHandler.cs
          StartAnalysisProcessingResult.cs

    ProcessingService.Domain/
      Entities/
        AnalysisProcess.cs

      Enums/
        DiagramType.cs
        ProcessingStatus.cs

      Events/
        AnalysisProcessingCompletedDomainEvent.cs
        AnalysisProcessingFailedDomainEvent.cs
        AnalysisProcessingStartedDomainEvent.cs

      Exceptions/
        AiAnalysisException.cs
        DiagramProcessingException.cs
        InvalidAnalysisResultException.cs
        UnsupportedDiagramFormatException.cs

      ValueObjects/
        AnalysisRequestId.cs
        ExtractedText.cs
        ProcessingResultSummary.cs
        SourceFileLocation.cs

    ProcessingService.Infrastructure/

  ReportService/
    ReportService.Api/
      appsettings.Development.json
      appsettings.json
      Dockerfile
      Program.cs

      Configuration/
        ProblemDetailsExtensions.cs
        SwaggerExtensions.cs

      Contracts/
        Requests/
          GenerateReportRequest.cs

      Controllers/
        ReportsController.cs

      DependencyInjection/
        ServiceCollectionExtensions.cs

      Middlewares/
        ExceptionHandlingMiddleware.cs

      Responses/
        DownloadReportResponse.cs
        GenerateReportResponse.cs
        GetReportByAnalysisResponse.cs

    ReportService.Application/
      AssemblyReference.cs
      DependencyInjection.cs

      Abstractions/
        Clock/
          IDateTimeProvider.cs

        Messaging/
          IEventPublisher.cs
          IIntegrationEventHandler.cs
          IIntegrationEventMapper.cs

        Persistence/
          IAnalysisReportRepository.cs
          IUnitOfWork.cs

        Rendering/
          IReportRenderer.cs

        Storage/
          IReportStorage.cs

      Integration/
        Consumed/
          AnalysisCompletedMessageHandler.cs

        Published/
          ReportGeneratedIntegrationEventMapper.cs

      Mappings/
        AnalysisReportMappings.cs

      UseCases/
        DownloadReport/
          DownloadReportHandler.cs
          DownloadReportQuery.cs
          DownloadReportResult.cs

        GenerateReport/
          GenerateReportCommand.cs
          GenerateReportHandler.cs
          GenerateReportResult.cs
          GenerateReportValidator.cs

        GetReportByAnalysis/
          GetReportByAnalysisHandler.cs
          GetReportByAnalysisQuery.cs
          GetReportByAnalysisResult.cs

        UpdateReportStatus/
          UpdateReportStatusCommand.cs
          UpdateReportStatusHandler.cs
          UpdateReportStatusResult.cs

    ReportService.Domain/
      Entities/
        AnalysisReport.cs

      Enums/
        ReportFormat.cs
        ReportStatus.cs

      Events/
        ReportGeneratedDomainEvent.cs
        ReportGenerationFailedDomainEvent.cs
        ReportGenerationRequestedDomainEvent.cs

      Exceptions/
        EmptyReportContentException.cs
        ReportGenerationException.cs
        UnsupportedReportFormatException.cs

      ValueObjects/
        AnalysisRequestId.cs
        GeneratedFileLocation.cs
        ReportContent.cs
        ReportId.cs

    ReportService.Infrastructure/
      Clock/
        SystemDateTimeProvider.cs

      Configuration/
        DependencyInjection.cs

        Options/
          DatabaseOptions.cs
          RabbitMqOptions.cs
          ReportOptions.cs
          StorageOptions.cs

      Exceptions/
        MessagePublishException.cs
        ReportRenderingException.cs
        ReportStorageUnavailableException.cs

      HealthChecks/
        MinIoHealthCheck.cs
        PostgreSqlHealthChecks.cs
        RabbitMqHealthCheck.cs

      Messaging/
        QueueNames.cs

        RabbitMq/
          RabbitMqPublisher.cs
          RabbitMqSubscriberService.cs

          Internals/
            RabbitMqChannel.cs
            RabbitMqConsumerDescriptor.cs
            RabbitMqMessageDispatcher.cs

      Migrations/
        20260421203333_InitialCreate.cs
        20260421203333_InitialCreate.Designer.cs
        ReportDbContextModelSnapshot.cs

      Persistence/
        Configurations/
          AnalysisReportConfiguration.cs

        Context/
          ReportDbContext.cs

        Repositories/
          AnalysisReportRepository.cs

        UnitOfWork/
          EfUnitOfWork.cs

      Rendering/
        Json/
          JsonReportRenderer.cs

        Markdown/
          MarkdownReportRenderer.cs

        Pdf/
          PdfReportRenderer.cs

      Storage/
        MinIO/
          MinIoOptions.cs
          MinIoReportStorage.cs

  UploadService/
    UploadService.Api/
      appsettings.Development.json
      appsettings.json
      Dockerfile
      Program.cs

      Configuration/
        ProblemDetailsExtensions.cs
        SwaggerExtensions.cs

      Contracts/
        Requests/
          CreateAnalysisRequest.cs

        Responses/
          CreateAnalysisResponse.cs
          GetAnalysisStatusResponse.cs

      Controllers/
        AnalysesController.cs

      DependencyInjection/
        ServiceCollectionExtensions.cs

      Middlewares/
        ExceptionHandlingMiddleware.cs

      Services/
        SystemDateTimeProvider.cs

    UploadService.Application/
      AssemblyReference.cs
      DependencyInjection.cs

      Abstractions/
        Clock/
          IDateTimeProvider.cs

        Identity/
          IUserContext.cs

        Messaging/
          IEventPublisher.cs
          IIntegrationEventHandler.cs
          IIntegrationEventMapper.cs

        Persistence/
          IAnalysisRequestRepository.cs
          IUnitOfWork.cs

        Storage/
          IObjectStorage.cs
          IStorageObjectKeyFactory.cs
          IUploadPolicy.cs

      Integration/
        Consumed/
          AnalysisCompletedMessageHandler.cs
          AnalysisFailedMessageHandler.cs
          AnalysisStartedMessageHandler.cs

        Published/
          AnalysisRequestedIntegrationEventMapper.cs

      UseCases/
        CreateAnalysis/
          CreateAnalysisCommand.cs
          CreateAnalysisHandler.cs
          CreateAnalysisResult.cs
          CreateAnalysisValidator.cs

        GetAnalysisStatus/
          GetAnalysisStatusHandler.cs
          GetAnalysisStatusQuery.cs
          GetAnalysisStatusResult.cs

        UpdateAnalysisStatus/
          UpdateAnalysisStatusCommand.cs
          UpdateAnalysisStatusHandler.cs
          UpdateAnalysisStatusResult.cs

    UploadService.Domain/
      Entities/
        AnalysisRequest.cs

      Enums/
        AnalysisStatus.cs
        FileType.cs

      Events/
        AnalysisRequestCreatedDomainEvent.cs
        AnalysisStatusChangedDomainEvent.cs

      Exceptions/
        EmptyUploadException.cs
        FileSizeExceededException.cs
        UnsupportedFileTypeException.cs

      ValueObjects/
        FileHash.cs
        FileMetadata.cs
        StorageLocation.cs
        StorageObjectKey.cs

    UploadService.Infrastructure/
      Dockerfile.migrator

      Configuration/
        DependencyInjection.cs
        UploadPolicy.cs

        Options/
          DatabaseOptions.cs
          RabbitMqOptions.cs
          StorageOptions.cs
          UploadOptions.cs

      Exceptions/
        MessagePublishException.cs
        StorageUnavailableException.cs

      HealthChecks/
        MinIoHealthCheck.cs
        PostgreSqlHealthChecks.cs
        RabbitMqHealthCheck.cs

      Identity/
        StubUserContext.cs

      Messaging/
        QueueNames.cs

        RabbitMq/
          RabbitMqMessageDispatcher.cs
          RabbitMqPublisher.cs
          RabbitMqSubscriberService.cs

          Internals/
            RabbitMqChannel.cs
            RabbitMqConsumerDescriptor.cs

      Persistence/
        AnalysisRequestConfiguration.cs
        AnalysisRequestRepository.cs
        EfUnitOfWork.cs
        UploadDbContext.cs
        UploadDbContextFactory.cs

        Migrations/
          20260420152230_InitialUploadSchema.cs
          20260420152230_InitialUploadSchema.Designer.cs
          UploadDbContextModelSnapshot.cs

      Storage/
        StorageObjectKeyFactory.cs

        MinIO/
          MinIoObjectStorage.cs
          MinIoOptions.cs

Shared/
  Shared.Contracts/
    IntegrationEvents/
      AnalysisCompletedIntegrationEvent.cs
      AnalysisFailedIntegrationEvent.cs
      AnalysisRequestedIntegrationEvent.cs
      AnalysisStartedIntegrationEvent.cs
      ReportGeneratedIntegrationEvent.cs

      Abstractions/
        IntegrationEventBase.cs

      Enums/
        ComponentType.cs
        RecommendationCategory.cs
        RiskSeverityLevel.cs

      Schemas/
        AnalysisResultDto.cs
        AnalysisSummaryDto.cs
        ArchitecturalRecommendationDto.cs
        ArchitecturalRiskDto.cs
        IdentifiedComponentDto.cs

    Messaging/
      ExchangeNames.cs
      HeaderNames.cs
      RoutingKeys.cs

  Shared.Kernel/
    Exceptions/
      AppException.cs
      DomainException.cs
      NotFoundException.cs
      ValidationException.cs

    Pagination/
      PagedResult.cs
      PaginationParams.cs

    Primitives/
      AggregateRoot.cs
      DomainEvent.cs
      Entity.cs
      ValueObject.cs

    Result/
      Error.cs
      ErrorType.cs
      Result.cs

  Shared.Observability/
    Correlation/
      CorrelationContextAccessor.cs
      CorrelationMiddlewareExtension.cs
      ICorrelationContextAccessor.cs

    HealthChecks/
      HealthCheckExtensions.cs

    Logging/
      LogEnrichers.cs
      SerilogExtensions.cs

    Messaging/
      MessageCorrelationContext.cs
      MessageCorrelationExtensions.cs

    Telemetry/
      ActivitySources.cs
      MetricNames.cs
      OpenTelemetryExtensions.cs
      TelemetryConstants.cs