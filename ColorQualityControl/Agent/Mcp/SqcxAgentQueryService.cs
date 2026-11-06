using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Agent.Mcp
{
    /// <summary>
    /// Produces serializable snapshots and user-confirmed exports for the local MCP bridge.
    /// All access is dispatched to the WPF UI thread because the legacy job model
    /// and its adapters are owned by MainWindow.
    /// </summary>
    internal sealed class SqcxAgentQueryService
    {
        public JToken Execute(string method, JObject arguments)
        {
            if (Application.Current == null)
            {
                throw new InvalidOperationException("SQCX application is not ready.");
            }

            return Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.MainWindow as MainWindow;
                if (window == null)
                {
                    throw new InvalidOperationException("SQCX main window is not ready.");
                }

                switch (method)
                {
                    case "get_application_context":
                        return GetApplicationContext(window);
                    case "list_standards":
                        return ListStandards(window, arguments);
                    case "list_tests":
                        return ListTests(window, arguments);
                    case "get_test_details":
                        return GetTestDetails(window, arguments);
                    case "export_data":
                        return ExportData(window, arguments);
                    default:
                        throw new ArgumentException("Unknown SQCX agent method: " + method, nameof(method));
                }
            });
        }

        private static JToken GetApplicationContext(MainWindow window)
        {
            var database = window.Database;
            var instrument = window.Colorimeter;
            var session = window.Session;
            var currentStandard = window.CurrentStandard;
            var currentTest = window.CurrentTest;
            var specular = window.SpecularComponent;
            var illuminant = window.Illuminant;
            var observer = window.Observer;
            var instrumentType = window.InstrumentType;
            var instrumentConnection = session?.Instrument;

            var job = new
            {
                fileName = (database == null || string.IsNullOrWhiteSpace(database.FilePath))
                    ? null
                    : Path.GetFileName(database.FilePath),
                isModified = database?.IsModified ?? false,
                lastModifiedAt = database?.LatestModificationTimestamp,
                standardCount = database?.StandardSpecimens?.Length ?? 0,
                testCount = database?.TestSpecimens?.Length ?? 0,
                orphanTestCount = database?.OrphanTests?.Length ?? 0
            };

            return JToken.FromObject(new
            {
                application = new
                {
                    name = App.Title,
                    version = App.AppVersion?.ToString()
                },
                job = job,
                measurementContext = new
                {
                    specularComponent = specular.ToString(),
                    illuminant = illuminant.ToString(),
                    observer = observer.ToString()
                },
                selection = new
                {
                    currentStandard = currentStandard == null ? null : SpecimenIdentity(currentStandard),
                    currentTest = currentTest == null ? null : TestIdentity(currentTest)
                },
                instrument = new
                {
                    connected = instrument != null && instrument.IsOpen,
                    connectionState = instrumentConnection?.ConnectionState.ToString(),
                    type = instrumentType.ToString(),
                    model = instrument?.Model,
                    deviceName = instrument?.DeviceName,
                    serialNumber = instrument?.SN
                },
                safety = new
                {
                    access = "read-only-data-with-confirmed-export",
                    note = "MCP cannot connect, calibrate, measure, modify, save, or delete job data. Exporting a new file requires user confirmation in SQCX."
                }
            });
        }

        private static JToken DatabaseNotReady()
        {
            return JToken.FromObject(new
            {
                available = false,
                reason = "SQCX 当前未打开 Job 数据，无法执行该操作。请先在软件中打开或新建一个 Job。"
            });
        }

        private static JToken ListStandards(MainWindow window, JObject arguments)
        {
            if (window.Database == null) return DatabaseNotReady();
            string query = OptionalString(arguments, "query");
            int limit = ReadLimit(arguments);
            var standards = window.Database.StandardSpecimens
                .Where(s => Matches(s.Name, query) || Matches(s.Material, query))
                .Take(limit)
                .Select(s => new
                {
                    id = s.ID,
                    name = s.Name,
                    material = s.Material,
                    measuredAt = s.DateTime,
                    instrument = s.Instrument,
                    isTransmission = s.IsTransmission,
                    testCount = s.TestSpecimens?.Count ?? 0
                })
                .ToArray();

            return JToken.FromObject(new
            {
                count = standards.Length,
                limit,
                items = standards
            });
        }

        private static JToken ListTests(MainWindow window, JObject arguments)
        {
            if (window.Database == null) return DatabaseNotReady();
            int? standardId = OptionalInt(arguments, "standard_id");
            string query = OptionalString(arguments, "query");
            int limit = ReadLimit(arguments);

            IEnumerable<TestSpecimen> tests = window.Database.TestSpecimens;
            if (standardId.HasValue)
            {
                tests = tests.Where(t => t.StandardSpecimen != null && t.StandardSpecimen.ID == standardId.Value);
            }

            var items = tests
                .Where(t => Matches(t.Name, query) || Matches(t.Material, query))
                .Take(limit)
                .Select(t => new
                {
                    id = t.ID,
                    name = t.Name,
                    material = t.Material,
                    measuredAt = t.DateTime,
                    instrument = t.Instrument,
                    isTransmission = t.IsTransmission,
                    standard = t.StandardSpecimen == null ? null : SpecimenIdentity(t.StandardSpecimen)
                })
                .ToArray();

            return JToken.FromObject(new
            {
                count = items.Length,
                limit,
                items
            });
        }

        private static JToken GetTestDetails(MainWindow window, JObject arguments)
        {
            if (window.Database == null) return DatabaseNotReady();
            int testId = RequiredInt(arguments, "test_id");
            int? standardId = OptionalInt(arguments, "standard_id");
            var test = window.Database.TestSpecimens.FirstOrDefault(t =>
                t.ID == testId &&
                (!standardId.HasValue || (t.StandardSpecimen != null && t.StandardSpecimen.ID == standardId.Value)));

            if (test == null)
            {
                throw new KeyNotFoundException("Test specimen was not found. Supply standard_id when test IDs repeat between standards.");
            }

            TestSpecimenAdapter adapter = window.Database.GetAdapter(
                test,
                window.SpecularComponent,
                window.Observer,
                window.Illuminant);

            return JToken.FromObject(new
            {
                specimen = new
                {
                    id = test.ID,
                    name = test.Name,
                    material = test.Material,
                    measuredAt = test.DateTime,
                    instrument = test.Instrument,
                    isTransmission = test.IsTransmission,
                    temperature = Finite(test.Temperature),
                    gloss8 = test.Gloss8,
                    standard = test.StandardSpecimen == null ? null : SpecimenIdentity(test.StandardSpecimen)
                },
                measurementContext = new
                {
                    specularComponent = window.SpecularComponent.ToString(),
                    illuminant = window.Illuminant.ToString(),
                    observer = window.Observer.ToString()
                },
                color = new
                {
                    lab = adapter.Lab == null ? null : new
                    {
                        l = Finite(adapter.L),
                        a = Finite(adapter.a),
                        b = Finite(adapter.b),
                        c = Finite(adapter.C),
                        h = Finite(adapter.h)
                    },
                    difference = new
                    {
                        deltaL = Finite(adapter.dL),
                        deltaA = Finite(adapter.da),
                        deltaB = Finite(adapter.db),
                        deltaE76 = Finite(adapter.dEab),
                        deltaE94 = Finite(adapter.dE94),
                        deltaE00 = Finite(adapter.dE00),
                        deltaECmc = Finite(adapter.dEcmc)
                    },
                    judgement = adapter.Judgement?.TotalJudgement.ToString()
                },
                provenance = new
                {
                    source = "ThreeNH.ColorQualityControl deterministic adapters",
                    authoritative = true,
                    generatedAt = DateTimeOffset.Now
                }
            });
        }

        private static JToken ExportData(MainWindow window, JObject arguments)
        {
            if (window.Database == null) return DatabaseNotReady();
            string scope = RequiredString(arguments, "scope").ToLowerInvariant();
            string format = RequiredString(arguments, "format").ToLowerInvariant();
            int? standardId = OptionalInt(arguments, "standard_id");

            if (format != "xlsx" && format != "txt")
            {
                throw new ArgumentException("format must be xlsx or txt.");
            }

            StandardSpecimen standard = null;
            TestSpecimen test = null;
            int standardCount = 0;
            int testCount = 0;
            string defaultName;

            switch (scope)
            {
                case "all":
                    standardCount = window.Database.StandardSpecimens.Length;
                    testCount = window.Database.TestSpecimens.Length;
                    defaultName = string.IsNullOrWhiteSpace(window.Database.FilePath)
                        ? DateTime.Now.ToString("yyyy-MM-dd")
                        : Path.GetFileNameWithoutExtension(window.Database.FilePath);
                    break;

                case "standard":
                case "standard_with_tests":
                    standard = FindStandard(window, RequiredInt(arguments, "standard_id"));
                    standardCount = 1;
                    testCount = scope == "standard_with_tests"
                        ? standard.TestSpecimens?.Count ?? 0
                        : 0;
                    defaultName = SafeFileName(standard.Name, "standard-" + standard.ID);
                    break;

                case "test":
                    test = FindTest(window, RequiredInt(arguments, "test_id"), standardId);
                    standard = test.StandardSpecimen;
                    testCount = 1;
                    defaultName = SafeFileName(test.Name, "test-" + test.ID);
                    break;

                case "selected":
                    test = window.CurrentTest;
                    standard = test?.StandardSpecimen ?? window.CurrentStandard;
                    if (test != null)
                    {
                        testCount = 1;
                        defaultName = SafeFileName(test.Name, "test-" + test.ID);
                    }
                    else if (standard != null)
                    {
                        standardCount = 1;
                        defaultName = SafeFileName(standard.Name, "standard-" + standard.ID);
                    }
                    else
                    {
                        throw new InvalidOperationException("No specimen is currently selected in SQCX.");
                    }
                    break;

                default:
                    throw new ArgumentException(
                        "scope must be selected, standard, standard_with_tests, test, or all.");
            }

            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "." + format,
                Filter = format == "txt"
                    ? "Text (*.txt)|*.txt"
                    : "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = defaultName + "." + format,
                OverwritePrompt = true,
                Title = "Export SQCX data"
            };
            if (!string.IsNullOrWhiteSpace(window.Database.FilePath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(window.Database.FilePath);
            }

            if (dialog.ShowDialog(window) != true)
            {
                return JToken.FromObject(new
                {
                    success = false,
                    cancelled = true,
                    scope,
                    format
                });
            }

            IMyWriter writer = format == "txt"
                ? (IMyWriter)new MyTextWriter()
                : new MyExcelWriter();

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                writer.Create(dialog.FileName);
                var export = new SpecimenExportService(window.Database);

                if (scope == "all")
                {
                    var helper = window.GetSpecimenAdapterHelper(window.Database.SystemChromaticitySetting);
                    export.ExportAll(window.Database.SystemChromaticitySetting, writer, helper);
                }
                else if (scope == "standard_with_tests")
                {
                    var helper = window.GetSpecimenAdapterHelper(window.Database.GetChromaticitySetting(standard));
                    export.ExportStandardWithTests(writer, standard, helper, true);
                }
                else if (test != null)
                {
                    var setting = standard == null
                        ? window.Database.SystemChromaticitySetting
                        : window.Database.GetChromaticitySetting(standard);
                    var helper = window.GetSpecimenAdapterHelper(setting);
                    bool withStandard = standard != null;
                    writer.WriteRow(export.MakeColumnLabels(helper, withStandard));
                    export.WriteSpecimenRows(
                        writer,
                        helper,
                        test,
                        withStandard,
                        standardForTests: standard);
                }
                else
                {
                    var helper = window.GetSpecimenAdapterHelper(window.Database.GetChromaticitySetting(standard));
                    writer.WriteRow(export.MakeColumnLabels(helper, false));
                    export.WriteSpecimenRows(writer, helper, standard, false, false);
                }

                writer.Close();
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }

            return JToken.FromObject(new
            {
                success = true,
                cancelled = false,
                scope,
                format,
                filePath = dialog.FileName,
                standardCount,
                testCount
            });
        }

        private static StandardSpecimen FindStandard(MainWindow window, int standardId)
        {
            var standard = window.Database.StandardSpecimens.FirstOrDefault(s => s.ID == standardId);
            if (standard == null)
            {
                throw new KeyNotFoundException("Standard specimen was not found: " + standardId);
            }

            return standard;
        }

        private static TestSpecimen FindTest(MainWindow window, int testId, int? standardId)
        {
            var matches = window.Database.TestSpecimens.Where(t =>
                t.ID == testId &&
                (!standardId.HasValue ||
                 (t.StandardSpecimen != null && t.StandardSpecimen.ID == standardId.Value)))
                .Take(2)
                .ToArray();

            if (matches.Length == 0)
            {
                throw new KeyNotFoundException("Test specimen was not found: " + testId);
            }

            if (!standardId.HasValue && matches.Length > 1)
            {
                throw new InvalidOperationException(
                    "Test ID is ambiguous. Supply standard_id when test IDs repeat between standards.");
            }

            return matches[0];
        }

        private static string SafeFileName(string value, string fallback)
        {
            string result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(result) ? fallback : result;
        }

        private static object SpecimenIdentity(StandardSpecimen specimen)
        {
            return new { id = specimen.ID, name = specimen.Name };
        }

        private static object TestIdentity(TestSpecimen specimen)
        {
            return new
            {
                id = specimen.ID,
                name = specimen.Name,
                standardId = specimen.StandardSpecimen?.ID
            };
        }

        private static bool Matches(string value, string query)
        {
            return string.IsNullOrWhiteSpace(query)
                || (!string.IsNullOrWhiteSpace(value)
                    && value.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private static int ReadLimit(JObject arguments)
        {
            int requested = OptionalInt(arguments, "limit") ?? 50;
            return Math.Max(1, Math.Min(200, requested));
        }

        private static string OptionalString(JObject arguments, string name)
        {
            return arguments?[name]?.Type == JTokenType.String
                ? arguments.Value<string>(name)
                : null;
        }

        private static string RequiredString(JObject arguments, string name)
        {
            string value = OptionalString(arguments, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(name + " is required.");
            }

            return value.Trim();
        }

        private static int? OptionalInt(JObject arguments, string name)
        {
            var token = arguments?[name];
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            int value;
            if (!int.TryParse(token.ToString(), out value))
            {
                throw new ArgumentException(name + " must be an integer.");
            }

            return value;
        }

        private static int RequiredInt(JObject arguments, string name)
        {
            int? value = OptionalInt(arguments, name);
            if (!value.HasValue)
            {
                throw new ArgumentException(name + " is required.");
            }

            return value.Value;
        }

        private static double? Finite(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? (double?)null : value;
        }

        private static string RedactSerialNumber(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                return null;
            }

            string value = serialNumber.Trim();
            return value.Length <= 4
                ? "****"
                : new string('*', value.Length - 4) + value.Substring(value.Length - 4);
        }
    }
}
