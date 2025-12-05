using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace Wpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
   
        public partial class MainWindow : Window
        {
            List<string> list = new List<string>(); // Входная коллекция категорий
            public static ObservableCollection<string> strings = new ObservableCollection<string>(); // Динамическая коллекция категорий
            List<string> baseCollection = new List<string>(); // Базовая коллекция категорий для обновления списка
            public ObservableCollection<string> parameters = new ObservableCollection<string>(); //Коллекция параметров
            Dictionary<int, UIElement> conditionElements = new Dictionary<int, UIElement>(); // Коллекция элементов условий UIElement с индексами
            static int indexOfCondition = 0, marginVerticalConditions = 20; //Индекс условия для Dictionary и переменная вертикального Margin 
            static List<Control> controls = new List<Control>(); // Коллекция элементов условий типа Conrol для возможности изменять параметры элемента, например Margin
            public static bool proverka = false; // поле для запуска класса по определению параметров
            public static string[,] uslovia =new string[0,3]; // массив условий для параметров
            public static string[] unions = new string[0]; // массив И/ИЛИ между условиями
            public bool invert = false; // переменная для проверки нужно ли инвертировать выделение

        public MainWindow(List<string> categories)
            {
                
                InitializeComponent();
                this.Topmost = true;

                foreach (string category in categories)
                {
                list.Add(category);
                baseCollection.Add(category);
                   // parameters.Add("parametr" + i.ToString()); //TODO: удалить. Заполнение коллекции параметрами
                }

                foreach (string s in list)
                {
                    strings.Add(s);
                }

                lView.ItemsSource = strings;
                imageGood.Visibility = Visibility.Visible;
                imageBad.Visibility = Visibility.Hidden;
            }

            
            private void Button_Click(object sender, RoutedEventArgs e)
            {
                IList ilist = lView.SelectedItems;
                list.Clear();
                foreach (string s in ilist)
                {
                    list.Add((string)s);
                }
                strings.Clear();
                foreach (string s in list)
                {
                    strings.Add((string)s);
                }
                
                proverka = true;
            }

            private void Button_Click_1(object sender, RoutedEventArgs e)
            {
                lView.SelectAll();
            }

            private void Button_Click_2(object sender, RoutedEventArgs e)
            {
                lView.SelectedItems.Clear();
            }

            private void Button_Click_3(object sender, RoutedEventArgs e)
            {
                strings.Clear();
                foreach (string s in baseCollection)
                {
                    strings.Add(s.ToString());
                }
                lView.ItemsSource = strings;
            }

            private void Button_Click_4(object sender, RoutedEventArgs e)
            {
                StringBuilder sb = new StringBuilder(search.Text.ToString());
                list.Clear();
                foreach (string s in strings)
                {
                    list.Add(s.ToString());
                }
                strings.Clear();
                foreach (string s in list)
                {
                    if ((s.ToLower()).Contains((sb.ToString()).ToLower()))
                    {
                        strings.Add(s);
                    }
                }
            }

            private void Button_Click_5(object sender, RoutedEventArgs e)
            {
                ComboBox parametr = new ComboBox();
                parametr.Height = 20;
                parametr.Width = 200;
                parametr.HorizontalAlignment = HorizontalAlignment.Left;
                parametr.VerticalAlignment = VerticalAlignment.Top;
                parametr.Name = "parametr";
                parametr.Margin = new Thickness(20, marginVerticalConditions, 0, 0);
                parametr.ItemsSource = parameters;
                conditionElements.Add(indexOfCondition++, parametr);
                grid.Children.Add(parametr);
                controls.Add(parametr);

                List<string> conditions = new List<string>();
                conditions.Add("Равно");
                conditions.Add("Не равно");
                conditions.Add("Содержит");
                conditions.Add("Начинается с");
                conditions.Add("Больше");
                conditions.Add("Меньше");
                ComboBox condition1 = new ComboBox();
                condition1.Height = 20;
                condition1.Width = 100;
                condition1.HorizontalAlignment = HorizontalAlignment.Left;
                condition1.VerticalAlignment = VerticalAlignment.Top;
                condition1.Name = "condition1";
                condition1.Margin = new Thickness(240, marginVerticalConditions, 0, 0);
                condition1.ItemsSource = conditions;
                condition1.SelectedIndex = 0;
                conditionElements.Add(indexOfCondition++, condition1);
                grid.Children.Add(condition1);
                controls.Add(condition1);

                TextBox value = new TextBox();
                value.Height = 20;
                value.Width = 150;
                value.HorizontalAlignment = HorizontalAlignment.Left;
                value.VerticalAlignment = VerticalAlignment.Top;
                value.Name = "Value";
                value.Margin = new Thickness(360, marginVerticalConditions, 0, 0);
                conditionElements.Add(indexOfCondition++, value);
                grid.Children.Add(value);
                controls.Add(value);

                Button close = new Button();
                close.Height = 20;
                close.Width = 30;
                close.HorizontalAlignment = HorizontalAlignment.Left;
                close.VerticalAlignment = VerticalAlignment.Top;
                close.Background = Brushes.AliceBlue;
                close.Content = "X";
                close.Name = "close";
                close.Foreground = Brushes.Gray;
                close.Margin = new Thickness(515, marginVerticalConditions, 0, 0);
                close.Click += Close_Click;
                conditionElements.Add(indexOfCondition++, close);
                grid.Children.Add(close);
                controls.Add(close);

                if (marginVerticalConditions > 20)
                {
                    ComboBox souz = new ComboBox();
                    souz.Height = 20;
                    souz.Width = 60;
                    souz.HorizontalAlignment = HorizontalAlignment.Left;
                    souz.VerticalAlignment = VerticalAlignment.Top;
                    souz.Background = Brushes.AliceBlue;
                    souz.Name = "souz";
                    souz.ItemsSource = new string[] { "И", "ИЛИ" };
                    souz.Margin = new Thickness(20, marginVerticalConditions - 25, 0, 0);
                    conditionElements.Add(indexOfCondition++, souz);
                    grid.Children.Add(souz);
                    controls.Add(souz);
                    indexOfCondition--;
                }

                if (controls.Count > 19)
                {
                    imageGood.Visibility = Visibility.Hidden;
                    imageBad.Visibility = Visibility.Visible;
                }
                else
                {
                    imageGood.Visibility = Visibility.Visible;
                    imageBad.Visibility = Visibility.Hidden;
                }

                indexOfCondition += 6;
                marginVerticalConditions += 50;
            }



            private void Close_Click(object sender, RoutedEventArgs e)
            {
                int key = -1;
                double topMargin = 1000;

                foreach (var condition in conditionElements)
                {
                    if (sender.Equals((object)condition.Value))
                    {
                        key = (int)condition.Key;
                    }
                }

                foreach (var condition in conditionElements)
                {

                    if (Math.Round((double)condition.Key / 10, MidpointRounding.AwayFromZero) == Math.Round((double)key / 10, MidpointRounding.AwayFromZero))
                    {
                        grid.Children.Remove(condition.Value);
                        try
                        {
                            topMargin = controls.Cast<UIElement>().Where(it => it.Equals((object)condition.Value)).Cast<Control>().First().Margin.Top;
                        }
                        catch { }
                        controls = controls.Cast<UIElement>().Where(it => (it.Equals((object)condition.Value)) == false).Cast<Control>().ToList();

                    }

                }

                foreach (Control control in controls)
                {
                    if (control.Margin.Top > topMargin)
                    {
                        control.Margin = new Thickness(control.Margin.Left, control.Margin.Top - 50, control.Margin.Right, control.Margin.Bottom);
                    }

                    if (control.Margin.Top < 20)
                    {
                        grid.Children.Remove((Control)control);
                        controls = controls.Cast<object>().Where(it => it.Equals((object)control) == false).Cast<Control>().ToList();

                    }
                }
                marginVerticalConditions -= 50;

                if (controls.Count > 19)
                {
                    imageGood.Visibility = Visibility.Hidden;
                    imageBad.Visibility = Visibility.Visible;
                }
                else
                {
                    imageGood.Visibility = Visibility.Visible;
                    imageBad.Visibility = Visibility.Hidden;
                }

            }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            invert = true;
        }

        private void Click_Search(object sender, RoutedEventArgs e)
            {
            uslovia = new string[0, 3];
            unions = new string[0];
            int j = 0, k = 0, x = 0;
            for (int i = 0; i < controls.Count; i++)
            {


                if (controls[i].Name != "close" && controls[i].Name != "souz")
                {
                    string[,] vremUsl = uslovia;
                    uslovia = new string[k + 1, 3];
                    for (int q = 0; q < vremUsl.GetLength(0); q++)
                    {
                        for (int r = 0; r < 3; r++)
                        {
                            uslovia[q, r] = vremUsl[q, r];
                        }
                    }

                    if (controls[i].Name == "parametr" || controls[i].Name == "condition1")
                    {
                        if (((Selector)controls[i]).SelectedItem == null)
                        {
                            Window window = new Window();
                            window.Title = "Ошибка";
                            window.Width = 400;
                            window.Height = 150;
                            window.Content = "Ошибка: Не заполнены поля условий в конструкторе правил";
                            window.Activate();
                            window.Topmost = true;
                            window.ShowDialog();
                            break;
                        }

                        uslovia[k, j] = ((Selector)controls[i]).SelectedValue.ToString();
                    }
                    if (controls[i].Name == "Value")
                    {
                        if (((TextBox)controls[i]).Text == "")
                        {
                            Window window = new Window();
                            window.Title = "Ошибка";
                            window.Width = 400;
                            window.Height = 150;
                            window.Content = "Ошибка: Не заполнены поля условий в конструкторе правил";
                            window.Activate();
                            window.Topmost = true;
                            window.ShowDialog();
                            break;
                        }
                        uslovia[k, j] = ((TextBox)controls[i]).Text;
                    }
                    j++;
                }



                if (controls[i].Name == "souz")
                {
                    string[] vremUnion = unions;
                    unions = new string[x + 1];
                    for (int q = 0; q < vremUnion.Length; q++)
                    {
                        unions[q] = vremUnion[q];
                    }
                    if (((Selector)controls[i]).SelectedItem == null)
                    {
                        Window window = new Window();
                        window.Title = "Ошибка";
                        window.Width = 400;
                        window.Height = 150;
                        window.Content = "Ошибка: Не заполнены поля условий в конструкторе правил";
                        window.Activate();
                        window.Topmost = true;
                        window.ShowDialog();
                        break;
                    }
                    unions[x] = ((Selector)controls[i]).SelectedValue.ToString();
                    x++;
                }

                if (j == 3)
                {
                    k++;
                    j = 0;
                }
            }

        }

        

        }
    
}
