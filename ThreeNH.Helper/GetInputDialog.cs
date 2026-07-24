using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ThreeNH.Helper
{
    public class GetInputDialog : Window
    {
        public static readonly DependencyProperty PromptProperty =
            DependencyProperty.Register("Prompt", typeof(string), typeof(GetInputDialog));
        public static readonly DependencyProperty InputProperty =
            DependencyProperty.Register("Input", typeof(string), typeof(GetInputDialog));

        public GetInputDialog(string prompt=null, string defaultInput=null, Window owner = null)
        {
            Owner = owner;
            Prompt = prompt;
            Input = defaultInput;

            var grid = new Grid();
            grid.Margin = new Thickness(16);
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            var textBlock = new TextBlock
            {
                Margin = new Thickness(6)
            };
            textBlock.SetBinding(TextBlock.TextProperty, new Binding("Prompt") {Source = this});
            Grid.SetColumn(textBlock, 0);
            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var textBox = new TextBox
            {
                Margin = new Thickness(6)
            };
            textBox.SetBinding(TextBox.TextProperty, new Binding("Input") {Source = this});
            Grid.SetColumn(textBox, 0);
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            textBox.GotFocus += (sender, args) => textBox.SelectAll();


            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(stackPanel, 0);
            Grid.SetRow(stackPanel, 2);
            grid.Children.Add(stackPanel);

            Button btn = new Button
            {
                Content = "确定",
                IsDefault = true,
                Margin = new Thickness(6),
                MinWidth = 60
            };
            btn.Click += (sender, args) => DialogResult = true;
            stackPanel.Children.Add(btn);

            btn = new Button
            {
                Content = "取消",
                IsCancel = true,
                Margin = new Thickness(6),
                MinWidth = 60
            };
            stackPanel.Children.Add(btn);

            MinWidth = 250;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Content = grid;
            DataContext = this;
        }

        public string Prompt
        {
            get { return (string)GetValue(PromptProperty); }
            set { SetValue(PromptProperty, value);}
        }

        public string Input
        {
            get { return (string)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }
}
