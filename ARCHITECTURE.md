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
Within that compatibility layer, `IMeasurementCaptureGateway` now separates
instrument capture and result calculation from `IMeasurementHost`, which owns
prompts, progress presentation, naming, and view/database completion callbacks.

Standard selection is implemented by:

- `ThreeNH.SQCX.Domain/StandardSelection/StandardSelectionPolicy`
- `ThreeNH.SQCX.Application/StandardSelection/SelectStandardUseCase`
- `ColorQualityControl/Adapter/LegacyStandardSelectionGateway`

Domain owns the ΔL/Δa/Δb/ΔE acceptance policy. Application owns manual versus
automatic selection and manual fallback. The legacy adapter performs the
existing database query and color-algorithm conversion without leaking those
dependencies into the inner layers.

XML job document persistence is implemented by:

- `ThreeNH.SQCX.Application/JobDocuments/IJobDocumentStore`
- `ThreeNH.SQCX.Application/JobDocuments/JobDocumentPersistence`
- `ThreeNH.SQCX.Infrastructure/Persistence/FileSystemJobDocumentStore`

The legacy job service remains responsible for translating the existing
`SpecimenDatabase` to and from XML. Application validates and coordinates the
persistence request, while Infrastructure owns physical UTF-8 file access.
`MainWindowSession` is the composition root for this dependency. The
proprietary QTX reader/writer remains in the compatibility layer for now.

## Device adapter migration

Device/SDK projects (PTS, ColorReader, LedSpectrometer, GratingSpectrometer,
NRColorimeter, YRColorimeter, CR400, BTS, Spectrophotometer, YL30, ThreeNH.YL30)
are decomposed into `ThreeNH.SQCX.Infrastructure` under an `Instruments/<Name>/`
folder. Each device project keeps its original C# `namespace` (e.g. `PTS`,
`ThreeNH.ColorReader`, `BTS`, `ThreeNH.YL30`) so the presentation layer keeps
compiling unchanged; only the physical assembly boundary moves from a standalone
`<Name>.dll` into `ThreeNH.SQCX.Infrastructure.dll`.

Mapping used:

- `PTS` → `Instruments/PTS`
- `ColorReader` → `Instruments/ColorReader`
- `LedSpectrometer` → `Instruments/LedSpectrometer` (+ `GratingSpectrometer` subfolder)
- `Spectrophotometer` → `Instruments/Common/Spectrophotometer`
- `CR400` → `Instruments/CR400`
- `BTS`, `NRColorimeter`, `YRColorimeter` → `Instruments/Bluetooth/<Name>`
- `YL30` + `ThreeNH.YL30` → `Instruments/YL30` (`ThreeNH.YL30` in `Instruments/YL30/ThreeNH.YL30`)

Rule applied for every device:

1. Copy the device project's `.cs` files (not `Properties\AssemblyInfo.cs`,
   `bin\`, `obj\`, or unreferenced leftovers such as `ColorReader/Spectrometer接口用.cs`
   and `GratingSpectrometer/Properties/Resources.Designer.cs`) into
   `Infrastructure\Instruments\<Name>\`.
2. Add those files to `ThreeNH.SQCX.Infrastructure.csproj` and adopt the device
   project's `ProjectReference`s to the shared projects (ColorAlgorithm is the
   native C++ `ColorAlgorithm.vcxproj`). The full shared-project set is:
   ColorAlgorithm, ColorimeterInterface, ColorSpecimen, CommunicationBase, DataModel,
   OpticalDefines, PortService, ThreeNH.BleProtocol, ThreeNH.CommDevice,
   ThreeNH.CommProtocol, ThreeNH.Helper, ThreeNH.StructHelper (plus ColorLibrary used
   by other Infrastructure adapters).
3. Remove the standalone device project from `ColorQualityControl.sln` and from every
   `ProjectReference` in `ColorQualityControl.csproj` (the presentation layer already
   references `Infrastructure`).
4. Keep the orphaned device project folders as rollback backups until the build is
   confirmed locally, then delete them.

### Namespace audit (merged into one assembly)

Eleven projects now compile into a single `ThreeNH.SQCX.Infrastructure` assembly.
Audit results:

- Each device keeps its own top-level `namespace` (`PTS`, `ThreeNH.ColorReader`,
  `ThreeNH.LedSpectrometer`, `ThreeNH.GratingSpectrometer`, `ThreeNH.NRColorimeter`,
  `ThreeNH.YRColorimeter`, `ThreeNH.CR400`, `BTS`, `ThreeNH.Spectrophotometer`,
  `YL30`, `ThreeNH.YL30`). Top-level type names (e.g. `Spectrometer`, `DeviceInfo`,
  `BleSpectrophotometer`) do NOT collide because they live in different namespaces.
- Four files declare a nested `namespace Instrument`
  (`ThreeNH.LedSpectrometer.Instrument`, `ThreeNH.GratingSpectrometer.Instrument`,
  `ThreeNH.NRColorimeter.Instrument`, `ThreeNH.YRColorimeter.Instrument`). These are
  distinct fully-qualified namespaces, so there is no CS0101 duplicate-definition
  conflict. The nested `Instrument.DateTime` / `Instrument.SampleData` structs in
  each are likewise isolated by their parent namespace.
- `YL30/Comm/Result/*.cs` declares `namespace YL30Comm.Result` (note: no dot between
  `YL30` and `Comm` — a pre-existing typo in the original YL30 project). It is used
  only internally within the YL30 adapters and remains self-consistent, so it does
  not break the build; left as-is for now.
- All device `using` directives resolve to either an internal `Instruments` namespace
  or one of the shared-project references listed above. No file imports an assembly
  that `Infrastructure` does not reference.

### Migrated device adapters (complete)

All eleven device/SDK projects are migrated; `ColorQualityControl` no longer
references any standalone device project. Their types now resolve through
`Infrastructure`:

- `PTS` → `Instruments/PTS` (pilot)
- `ColorReader` → `Instruments/ColorReader`
- `LedSpectrometer` (incl. `GratingSpectrometer`) → `Instruments/LedSpectrometer`
- `Spectrophotometer` → `Instruments/Common/Spectrophotometer`
- `CR400` → `Instruments/CR400`
- `BTS`, `NRColorimeter`, `YRColorimeter` → `Instruments/Bluetooth`
- `YL30` + `ThreeNH.YL30` → `Instruments/YL30`

## Next migration slices

1. Move the Loading-based capture implementation behind an Infrastructure adapter.
2. Move backup discovery/retention behind Application ports.
3. Extract QTX serialization behind a format adapter.
4. Remove superseded legacy selection strategies after regression coverage.
