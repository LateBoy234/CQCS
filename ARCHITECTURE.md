# ColorQualityControl architecture

The solution is being migrated incrementally to Clean Architecture.

## Dependency direction

```text
ColorQualityControl (WPF presentation)
             |
             v
        Application <----- Infrastructure (composition/adapters)
             |
             v
           Domain
```

- `Domain` contains business rules and has no project dependencies.
- `Application` contains use cases and depends only on `Domain`.
- `Infrastructure` implements external concerns and wires use cases.
- `ColorQualityControl` is the presentation/composition host. Legacy code remains
  here until each feature is migrated through an application boundary.

## Migration rule

Move one feature at a time: define the domain rule, expose an application use
case, add infrastructure adapters, then replace the legacy presentation call.
Do not reference WPF, databases, device SDKs, files, or network APIs from
`Domain` or `Application`.

## Migrated features

Tolerance-based quality judgement is implemented by:

- `Domain/QualityControl/QualityJudgementService`
- `Application/QualityControl/EvaluateQualityUseCase`
- `Infrastructure/DependencyInjection/ApplicationServices`
- the legacy `Judgement` presentation adapter

Measurement command orchestration is implemented by:

- `Domain/Measurement/MeasurementTarget`
- `Application/Measurement/ExecuteMeasurementUseCase`
- `Application/Measurement/IMeasurementGateway`
- `ColorQualityControl/Adapter/LegacyMeasurementGateway`

The application use case now owns connection checks, calibration routing,
measurement locking, normal/haze routing, and failure cleanup. WPF routed
commands only forward `Standard` or `Trial` intent. The legacy gateway is a
temporary adapter around the existing device and UI-aware measurement service.

## Next migration slices

1. Move specimen selection policy and nearest-standard matching into Domain.
2. Split device capture from UI prompts in `MeasurementService`.
3. Implement device and persistence gateways in Infrastructure.
4. Move job document save/load behind Application ports.
