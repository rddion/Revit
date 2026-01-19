using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Constants;

namespace Wpf.View.ViewServices
{
    internal class RuleManager
    {
        private int controlHeight = 20;
        private int controlParameterWidth = 200;
        private int controlConditionWidth = 100;
        private int controlTextBoxWidth = 150;
        private int controlCloseWidth = 30;
        private int controlSouzWidth = 60;
        private int marginVerticalConditions = 20;
        private int indexOfCondition = 0;


        private ObservableCollection<Control> controls = new ObservableCollection<Control>();
        Dictionary<int, UIElement> conditionElements = new Dictionary<int, UIElement>();

        private MainWindow window;


        public RuleManager(MainWindow mainWindow)
        {
            this.window = mainWindow;
        }

        public void AddRule()
        {
            Binding bindingParameters = new Binding("Parameters");
            bindingParameters.Source = window.ViewModel;
            bindingParameters.Mode = BindingMode.TwoWay;
            bindingParameters.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            Guid ruleId = Guid.NewGuid();

            if (controls.Count < ((int)ControlTrigger.FullStack))
            {
                ViewModel.Condition conditionParam = new ViewModel.Condition("parametr", ruleId);
                window.ViewModel.Conditions.Add(conditionParam);

                ComboBox parametr = new ComboBox();
                parametr.DataContext = conditionParam;
                parametr.Height = controlHeight;
                parametr.Width = controlParameterWidth;
                parametr.HorizontalAlignment = HorizontalAlignment.Left;
                parametr.VerticalAlignment = VerticalAlignment.Top;
                parametr.Margin = new Thickness(20, marginVerticalConditions, 0, 0);
                parametr.Name = "parametr";
                parametr.SetBinding(ComboBox.ItemsSourceProperty, bindingParameters);
                parametr.SetBinding(ComboBox.SelectedValueProperty, new Binding("SelectedValue") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                parametr.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedItem") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                parametr.SetBinding(ComboBox.BackgroundProperty, new Binding("Background") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                window.grid.Children.Add(parametr);
                controls.Add(parametr);
                conditionElements.Add(indexOfCondition++, parametr);

                ViewModel.Condition conditionCond = new ViewModel.Condition("condition1", ruleId);
                window.ViewModel.Conditions.Add(conditionCond);

                List<string> conditionsList = new List<string> { "Равно", "Не равно", "Содержит", "Начинается с", "Больше", "Меньше" };
                ComboBox condition1 = new ComboBox();
                condition1.DataContext = conditionCond;
                condition1.Height = controlHeight;
                condition1.Width = controlConditionWidth;
                condition1.HorizontalAlignment = HorizontalAlignment.Left;
                condition1.VerticalAlignment = VerticalAlignment.Top;
                condition1.Name = "condition1";
                condition1.Margin = new Thickness(240, marginVerticalConditions, 0, 0);
                condition1.ItemsSource = conditionsList;
                condition1.SelectedIndex = 0;
                condition1.SetBinding(ComboBox.SelectedValueProperty, new Binding("SelectedValue") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                condition1.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedItem") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                condition1.SetBinding(ComboBox.BackgroundProperty, new Binding("Background") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                window.grid.Children.Add(condition1);
                controls.Add(condition1);
                conditionElements.Add(indexOfCondition++, condition1);

                ViewModel.Condition conditionValue = new ViewModel.Condition("Value", ruleId);
                window.ViewModel.Conditions.Add(conditionValue);

                TextBox value = new TextBox();
                value.DataContext = conditionValue;
                value.Height = controlHeight;
                value.Width = controlTextBoxWidth;
                value.HorizontalAlignment = HorizontalAlignment.Left;
                value.VerticalAlignment = VerticalAlignment.Top;
                value.Background = Brushes.White;
                value.Opacity = 1;
                value.Name = "Value";
                value.Margin = new Thickness(360, marginVerticalConditions, 0, 0);
                value.SetBinding(TextBox.TextProperty, new Binding("Text") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                value.SetBinding(TextBox.BackgroundProperty, new Binding("Background") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                window.grid.Children.Add(value);
                controls.Add(value);
                conditionElements.Add(indexOfCondition++, value);

                Button close = new Button();
                close.Height = controlHeight;
                close.Width = controlCloseWidth;
                close.HorizontalAlignment = HorizontalAlignment.Left;
                close.VerticalAlignment = VerticalAlignment.Top;
                close.Background = Brushes.AliceBlue;
                close.Content = "X";
                close.Name = "close";
                close.Foreground = Brushes.Gray;
                close.Margin = new Thickness(515, marginVerticalConditions, 0, 0);
                close.Click += Close_Click;
                window.grid.Children.Add(close);
                controls.Add(close);
                conditionElements.Add(indexOfCondition++, close);

                if (marginVerticalConditions > ((int)ControlTrigger.TopMargin))
                {
                    ViewModel.Condition conditionSouz = new ViewModel.Condition("souz", ruleId);
                    window.ViewModel.Conditions.Add(conditionSouz);

                    ComboBox souz = new ComboBox();
                    souz.DataContext = conditionSouz;
                    souz.Height = controlHeight;
                    souz.Width = controlSouzWidth;
                    souz.HorizontalAlignment = HorizontalAlignment.Left;
                    souz.VerticalAlignment = VerticalAlignment.Top;
                    souz.Background = Brushes.AliceBlue;
                    souz.Name = "souz";
                    souz.ItemsSource = new string[] { "И", "ИЛИ" };
                    souz.Margin = new Thickness(20, marginVerticalConditions - 25, 0, 0);
                    souz.SetBinding(ComboBox.SelectedValueProperty, new Binding("SelectedValue") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                    souz.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedItem") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                    souz.SetBinding(ComboBox.BackgroundProperty, new Binding("Background") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                    window.grid.Children.Add(souz);
                    controls.Add(souz);
                    conditionElements.Add(indexOfCondition++, souz);
                    indexOfCondition--;
                }

                if (controls.Count > ((int)ControlTrigger.ALotOfRules))
                {
                    window.imageGood.Visibility = Visibility.Hidden;
                    window.imageBad.Visibility = Visibility.Visible;
                }
                else
                {
                    window.imageGood.Visibility = Visibility.Visible;
                    window.imageBad.Visibility = Visibility.Hidden;
                }

                indexOfCondition += ((int)ControlTrigger.IndexOfNewRow);
                marginVerticalConditions += ((int)ControlTrigger.MarginOfNewRow);
            }

            if (controls.Count >= ((int)ControlTrigger.FullStack))
            {
                window.addRule.IsEnabled = false;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            int keyOfCurrentRow = -1;
            double topMargin = 1000;
            double minMargin = 20;
            window.addRule.IsEnabled = true;
            Guid ruleIdToRemove = Guid.Empty;

            foreach (var condition in conditionElements)
            {
                if (sender.Equals((object)condition.Value))
                {
                    keyOfCurrentRow = (int)condition.Key;
                }
            }

            DeleteSelectRowOfControls(keyOfCurrentRow,ruleIdToRemove,ref topMargin);

            foreach (Control control in controls)
            {
                RaiseControlsBeforeDeletion(control,topMargin);

                DeleteControlsAboveTheBorder(control,minMargin);
            }

            DeleteUnnecessaryCondition();

            marginVerticalConditions -= ((int)ControlTrigger.MarginOfNewRow);

            DefineImageOfBackground();
        }

        public void ClearAllRules()
        {   
            for (int i = 0; i < controls.Count; i++)
            {
                if(controls[i].Name!= "imageGood" || controls[i].Name != "imageBad")
                {
                    window.grid.Children.Remove(controls[i]);
                }
            }
            controls.Clear();
            marginVerticalConditions = 20;
        } 

        private void DeleteSelectRowOfControls(int keyOfCurrentRow,Guid IdRemove,ref double topMargin)
        {
            foreach (var condition in conditionElements)
            {

                if (Math.Round((double)condition.Key / 10, MidpointRounding.AwayFromZero) == Math.Round((double)keyOfCurrentRow / 10, MidpointRounding.AwayFromZero))
                {
                    window.grid.Children.Remove(condition.Value);
                    try
                    {
                        topMargin = controls.Cast<UIElement>().Where(it => it.Equals((object)condition.Value)).Cast<Control>().First().Margin.Top;
                    }
                    catch { }
                    controls = controls.Cast<UIElement>().Where(it => (it.Equals((object)condition.Value)) == false).Cast<Control>().ToObservsbleCollection();

                    if (condition.Value is Control ctrl && ctrl.DataContext is ViewModel.Condition cond)
                    {
                        IdRemove = cond.RuleId;
                    }
                }
            }

            var conditionsToRemove = window.ViewModel.Conditions.Where(c => c.RuleId == IdRemove).ToList();
            foreach (var cond in conditionsToRemove)
            {
                window.ViewModel.Conditions.Remove(cond);
            }
        }

        private void RaiseControlsBeforeDeletion(Control control, double topMargin)
        {
            if (control.Margin.Top > topMargin)
            {
                control.Margin = new Thickness(control.Margin.Left, control.Margin.Top - ((int)ControlTrigger.MarginOfNewRow), control.Margin.Right, control.Margin.Bottom);
            }
        }

        private void DeleteControlsAboveTheBorder(Control control,double minMargin)
        {
            if (control.Margin.Top < minMargin)
            {
                window.grid.Children.Remove((Control)control);
                controls = controls.Cast<object>().Where(it => it.Equals((object)control) == false).Cast<Control>().ToObservsbleCollection();
            }
        }

        private void DefineImageOfBackground()
        {
            if (controls.Count > ((int)ControlTrigger.ALotOfRules))
            {
                window.imageGood.Visibility = Visibility.Hidden;
                window.imageBad.Visibility = Visibility.Visible;
            }
            else
            {
                window.imageGood.Visibility = Visibility.Visible;
                window.imageBad.Visibility = Visibility.Hidden;
            }
        }

        private void DeleteUnnecessaryCondition()
        {
            if (controls.Count == 4 && window.ViewModel.Conditions.Count == 4)
            {
                for (int i = 0; i < window.ViewModel.Conditions.Count; i++)
                {
                    if (window.ViewModel.Conditions[i].Name == "souz")
                    {
                        window.ViewModel.Conditions.Remove(window.ViewModel.Conditions[i]);
                    }
                }
            }
        }
    }

    public static class EnumerableExtension
    {
        public static ObservableCollection<T> ToObservsbleCollection<T>(this IEnumerable<T> collection)
        {
            return new ObservableCollection<T>(collection);
        }
    }
}
