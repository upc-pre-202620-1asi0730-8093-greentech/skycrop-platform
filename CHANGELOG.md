# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-11-14

### Added

- Shared Bounded Context:
  - Domain model for `Aggregates`, `Entities`, `Errors`, `Events` and `ValueObjects` with interfaces and base classes.
  - Application layer with `IEventHandler` and `Result`
  - Infrastructure layer with configuration for ASP.NET and persistence via Entity Framework Core.
  - Interfaces layer with `ProblemDetailsFactory`
- Plots Bounded Context:
  - Domain model for `AgriculturalPlot` aggregate, including commands, errors, events, queries and repositories.
  - Infrastructure layer with `ServiceCollectionExtensions` and persistence configuration via `ModelBuilderExtensions` and `AgriculturalPlotRepository` implementation.
  - Interfaces layer with `Acl` exposing `IPlotsContextFacade`, `Events` exposing `PlotRegisteredIntegrationEvent` and `Rest` with `PlotsController` and resources.
  - Application layer with `Acl` implementation, `AgriculturalPlotCommandServices` and `AgriculturalPlotQueryServices`
- Drones Bounded Context:
  - Domain model for `Drone`, `Flight`, `FlightRoute` aggregates, including commands, errors, events, queries and repositories.
  - Infrastructure layer with `ServiceCollectionExtensions` and persistence configuration via `ModelBuilderExtensions` and repositories implementation.
  - Interfaces layer with `Rest` having `DronesController`, `AerialImagesController`, `FlightRoutesController`, `FlightsController`, resources and assemblers.
  - Application layer with `DroneCommandService` and `DroneQueryService`
- Crops Bounded Context:
  - Domain model for `Crop` aggregate, including commands, errors, events, queries, value objects and repositories.
  - Infrastructure layer with `ServiceCollectionExtensions` and persistence configuration via `ModelBuilderExtensions` and repositories implementation.
  - Interfaces layer with `Rest` having `CropsController`, resources and assemblers.
  - Application layer with `CropCommandService` and `CropQueryService`
  - Resources for error messages
- Diagnoses Bounded Context:
  - Domain model for `Diagnosis` aggregate, including commands, events, entities, errors, value objects and repositories.
  - Infrastructure layer with `ImageAnalysis`, `Integration`, `ServiceCollectionExtensions`, persistence configuration via `ModelBuilderExtensions` and repositories implementation.
  - Interfaces layer with `Acl` exposing `IDiagnosesContextFacade` and `DiagnosisSummary`, `Events` exposing integration events and `Rest` with `DiagnosesController` and resources.
  - Application layer with `Acl` implementation, `IntegrationServices`, `DiagnosisCommandService`, `DiagnosisQueryService` and `DiagnosisEventHandlers`
  - Resources for error messages
- Reports Bounded Context:
  - Domain model for `Report` aggregate, including commands, errors, events, queries, value objects and repositories.
  - Infrastructure layer with persistence configuration via `ModelBuilderExtensions` and repositories implementation.
  - Interfaces layer with `Rest` having `ReportsController`, resources and assemblers.
  - Application layer with `ReportCommandService` and `ReportQueryService`.
- Other shared components:
  - Migrations for each context needing persistence
  - Error message resources for localization
  - Dynamic API documentation using Scalar