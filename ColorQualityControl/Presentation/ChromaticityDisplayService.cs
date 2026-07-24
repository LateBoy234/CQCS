using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Converters;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Helper;

namespace ThreeNH.ColorQualityControl.Presentation
{
    /// <summary>
    /// 标试样 DataGrid 列与容差表构建；从 MainWindow.Data 迁出以便阶段2 事件驱动刷新（2-T05）。
    /// </summary>
    internal sealed class ChromaticityDisplayService
    {
        private readonly IChromaticityDisplayHost _host;

        public ChromaticityDisplayService(IChromaticityDisplayHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void RefreshToleranceTable()
        {
            var tolerance = _host.Database.GetTolerance(
                _host.SingleStandardInShowing,
                new ObserverIlluminantPair(_host.Observer, _host.Illuminant));
            ResetToleranceDataGrid(tolerance);
        }

        public void ResetToleranceDataGrid(Tolerance tolerance)
        {
            var grid = _host.ToleranceDataGrid;
            if (tolerance == null)
            {
                grid.Visibility = Visibility.Collapsed;
                return;
            }

            grid.Visibility = Visibility.Visible;
            grid.Columns.Clear();
            grid.Items.Clear();

            var illuminantColumn = new DataGridTextColumn
            {
                Header = S._("Label_Tolerance")
            };
            var binding = new Binding
            {
                Converter = new ToleranceConverter(),
                ConverterParameter = _host.ObserverIlluminant
            };
            illuminantColumn.Binding = binding;
            grid.Columns.Add(illuminantColumn);

            foreach (var filed in tolerance.ItemKeys)
            {
                if (!tolerance[filed].UseForJudgement)
                {
                    continue;
                }

                var column = new DataGridTextColumn();
                grid.Columns.Add(column);
                binding = new Binding
                {
                    Converter = new ToleranceConverter(),
                    ConverterParameter = filed
                };
                column.Binding = binding;

                if (filed == ChromaticityField.dE00)
                {
                    column.Header = string.Format("{0}({1}:{2}:{3})", filed.Description(), tolerance.KL00, tolerance.KC00, tolerance.KH00);
                }
                else if (filed == ChromaticityField.dE94)
                {
                    column.Header = string.Format("{0}({1}:{2}:{3})", filed.Description(), tolerance.KL94, tolerance.KC94, tolerance.KH94);
                }
                else if (filed == ChromaticityField.dEcmc)
                {
                    column.Header = string.Format("{0}({1}:{2})", filed.Description(), tolerance.KLcmc, tolerance.KCcmc);
                }
                else
                {
                    column.Header = filed.Description();
                }
            }

            grid.Items.Add(tolerance);
        }

        public void ResetTestDataGrid(ChromaticitySetting setting)
        {
            try
            {
                var grid = _host.TestDataGrid;
                grid.BeginInit();
                grid.Columns.Clear();
                grid.SelectedValuePath = "Specimen";

                var tolerance = _host.Database.GetTolerance(
                    _host.SingleStandardInShowing,
                    new ObserverIlluminantPair(_host.Observer, _host.Illuminant));

                var passedColumn = new DataGridTemplateColumn
                {
                    CanUserResize = false,
                    CanUserReorder = false,
                    Header = ""
                };
                var text = new FrameworkElementFactory(typeof(Image));
                var imgBinding = new Binding("Judgement.TotalJudgement")
                {
                    Converter = new JudgementToImageConverter()
                };
                text.SetBinding(Image.SourceProperty, imgBinding);
                var template = new DataTemplate { VisualTree = text };
                passedColumn.CellTemplate = template;
                grid.Columns.Add(passedColumn);

                foreach (var field in setting.DisplayFields)
                {
                    var converter = TestSpecimenAdapter.GetPropertyValueConverter(field);

                    if (field.IsReflectance() || field.IsAbsoluteStrengthField())
                    {
                        int wavelength = field.ToWavelength();

                        var column = new DataGridTextColumn
                        {
                            MinWidth = 60,
                            Header = field.IsReflectance()
                                ? string.Format("{0} nm", wavelength)
                                : field.Description()
                        };
                        grid.Columns.Add(column);

                        var fieldBinding = new Binding("Adapter.Spectrum")
                        {
                            Converter = converter
                        };
                        if (field.IsAbsoluteStrengthField() && _host.CurrentTest != null)
                        {
                            fieldBinding.ConverterParameter = $"{wavelength}+{_host.CurrentTest.IsTransmission}";
                        }
                        else
                        {
                            fieldBinding.ConverterParameter = wavelength;
                        }

                        column.Binding = fieldBinding;
                        column.SortMemberPath = "Adapter.Spectrum[" + wavelength + "]";
                    }
                    else if (field.IsAbsorbanceField())
                    {
                        int wavelength = field.ToWavelength();

                        var column = new DataGridTextColumn
                        {
                            MinWidth = 60,
                            Header = field.Description()
                        };
                        grid.Columns.Add(column);

                        var multiBinding = new MultiBinding
                        {
                            Converter = new AbsorbanceConverter(),
                            ConverterParameter = wavelength
                        };
                        multiBinding.Bindings.Add(new Binding("Adapter.Spectrum"));
                        multiBinding.Bindings.Add(new Binding("Specimen.IsTransmission"));
                        column.Binding = multiBinding;
                        column.SortMemberPath = "Adapter.Spectrum[" + wavelength + "]";
                    }
                    else if (field == ChromaticityField.PseudoColor)
                    {
                        grid.Columns.Add(CreatePseudoColorColumn(field.Name()));
                    }
                    else if (field.IsReflectanceDifference())
                    {
                        int wavelength = field.ToWavelength();

                        var column = new DataGridTextColumn
                        {
                            MinWidth = 60,
                            Header = field.Description()
                        };
                        grid.Columns.Add(column);

                        var diffBinding = new Binding("Adapter.ReflectanceDifferences")
                        {
                            Converter = converter,
                            ConverterParameter = wavelength
                        };
                        column.Binding = diffBinding;
                        column.SortMemberPath = "Adapter.ReflectanceDifferences[" + wavelength + "]";
                    }
                    else if (field == ChromaticityField.Judgement)
                    {
                        var column = new DataGridTextColumn
                        {
                            Header = S._("Label_Judgement")
                        };
                        var judgementBinding = new Binding("Judgement.TotalJudgement")
                        {
                            Converter = converter
                        };
                        column.Binding = judgementBinding;
                        grid.Columns.Add(column);
                    }
                    else
                    {
                        var column = new DataGridTextColumn { MinWidth = 60 };
                        grid.Columns.Add(column);

                        Formula ref1, ref2;
                        _host.Database.GetReferenceFormula(_host.Formula, out ref1, out ref2);

                        switch (field)
                        {
                            case ChromaticityField.dEcmc:
                                column.Header = string.Format("{0}({1}:{2})", field.Name(), tolerance.KLcmc, tolerance.KCcmc);
                                break;
                            case ChromaticityField.dE94:
                                column.Header = string.Format("{0}({1}:{2}:{3})", field.Name(), tolerance.KL94, tolerance.KC94, tolerance.KH94);
                                break;
                            case ChromaticityField.dE00:
                                column.Header = string.Format("{0}({1}:{2}:{3})", field.Name(), tolerance.KL00, tolerance.KC00, tolerance.KH00);
                                break;
                            case ChromaticityField.MI1:
                                column.Header = ref1 != null ? SpecimenAdapter.MakeMIName(ref1) : "MI1";
                                break;
                            case ChromaticityField.MI2:
                                column.Header = ref2 != null ? SpecimenAdapter.MakeMIName(ref2) : "MI2";
                                break;
                            default:
                                column.Header = field.Name();
                                break;
                        }

                        if (field == ChromaticityField.MH || field == ChromaticityField.dMH)
                        {
                            var multiBinding = new MultiBinding
                            {
                                Converter = new MunsellValueConverter()
                            };
                            multiBinding.Bindings.Add(new Binding("Adapter." + field.ToString()));
                            multiBinding.Bindings.Add(new Binding("Adapter.MunsellHVC.Hstr"));
                            column.Binding = multiBinding;
                        }
                        else
                        {
                            var valueBinding = new Binding
                            {
                                Converter = converter
                            };
                            if (field == ChromaticityField.Name || field == ChromaticityField.Material)
                            {
                                valueBinding.Path = new PropertyPath(field.ToString());
                            }
                            else
                            {
                                valueBinding.Path = new PropertyPath("Adapter." + field.ToString());
                            }

                            column.Binding = valueBinding;
                        }
                    }
                }

                grid.EndInit();
            }
            catch (Exception)
            {
            }
        }

        public void ResetStandardDataGrid(ChromaticitySetting setting)
        {
            var grid = _host.StandardDataGrid;
            grid.BeginInit();
            grid.Columns.Clear();
            grid.SelectedValuePath = "Specimen";

            foreach (var field in setting.DisplayFields)
            {
                if (field.IsNeedStandard())
                {
                    continue;
                }

                if (field.IsReflectance() || field.IsAbsoluteStrengthField())
                {
                    var converter = SpecimenAdapter.GetPropertyValueConverter(field);
                    int wavelength = field.ToWavelength();

                    var column = new DataGridTextColumn { MinWidth = 60 };
                    grid.Columns.Add(column);

                    column.Header = field.IsReflectance()
                        ? string.Format("{0} nm", wavelength)
                        : field.Description();

                    var binding = new Binding("Adapter.Spectrum")
                    {
                        Converter = converter
                    };
                    if (field.IsAbsoluteStrengthField() && _host.CurrentStandard != null)
                    {
                        binding.ConverterParameter = $"{wavelength}+{_host.CurrentStandard.IsTransmission}";
                    }
                    else
                    {
                        binding.ConverterParameter = wavelength;
                    }

                    column.Binding = binding;
                    column.SortMemberPath = "Adapter.Spectrum[" + wavelength + "]";
                }
                else if (field.IsAbsorbanceField())
                {
                    int wavelength = field.ToWavelength();

                    var column = new DataGridTextColumn
                    {
                        MinWidth = 60,
                        Header = field.Description()
                    };
                    grid.Columns.Add(column);

                    var multiBinding = new MultiBinding
                    {
                        Converter = new AbsorbanceConverter(),
                        ConverterParameter = wavelength
                    };
                    multiBinding.Bindings.Add(new Binding("Adapter.Spectrum"));
                    multiBinding.Bindings.Add(new Binding("Specimen.IsTransmission"));
                    column.Binding = multiBinding;
                    column.SortMemberPath = "Adapter.Spectrum[" + wavelength + "]";
                }
                else if (field == ChromaticityField.PseudoColor)
                {
                    grid.Columns.Add(CreatePseudoColorColumn(field.Name()));
                }
                else
                {
                    var column = new DataGridTextColumn
                    {
                        MinWidth = 60,
                        Header = field.Name()
                    };
                    grid.Columns.Add(column);

                    if (field == ChromaticityField.MH || field == ChromaticityField.dMH)
                    {
                        var multiBinding = new MultiBinding
                        {
                            Converter = new MunsellValueConverter()
                        };
                        multiBinding.Bindings.Add(new Binding("Adapter." + field.ToString()));
                        multiBinding.Bindings.Add(new Binding("Adapter.MunsellHVC.Hstr"));
                        column.Binding = multiBinding;
                    }
                    else
                    {
                        var binding = new Binding
                        {
                            Converter = SpecimenAdapter.GetPropertyValueConverter(field)
                        };
                        if (field == ChromaticityField.Name || field == ChromaticityField.Material)
                        {
                            binding.Path = new PropertyPath(field.ToString());
                        }
                        else
                        {
                            binding.Path = new PropertyPath("Adapter." + field.ToString());
                        }

                        column.Binding = binding;
                    }
                }
            }

            grid.EndInit();
        }

        private static DataGridTemplateColumn CreatePseudoColorColumn(object columnHeader)
        {
            var column = new DataGridTemplateColumn
            {
                Header = columnHeader,
                SortMemberPath = "Adapter.ColorValue"
            };

            var factoryRectangle = new FrameworkElementFactory(typeof(Rectangle));
            factoryRectangle.Name = "pseudoColorRect";
            factoryRectangle.SetValue(Rectangle.MinWidthProperty, 16.0);
            factoryRectangle.SetValue(Rectangle.MinHeightProperty, 16.0);
            factoryRectangle.SetValue(Rectangle.FillProperty, new Binding("PseudoColor"));

            column.CellTemplate = new DataTemplate { VisualTree = factoryRectangle };
            return column;
        }
    }
}
