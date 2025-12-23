using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Wpf.ViewModel
{
    internal class ViewModel : MainWindow
    {
        List<string> list = new List<string>(); // Входная коллекция категорий
        public static ObservableCollection<string> strings = new ObservableCollection<string>(); // Динамическая коллекция категорий
        List<string> baseCollection = new List<string>(); // Базовая коллекция категорий для обновления списка
        public ObservableCollection<string> parameters = new ObservableCollection<string>(); //Коллекция параметров
        Dictionary<int, UIElement> conditionElements = new Dictionary<int, UIElement>(); // Коллекция элементов условий UIElement с индексами
        static int indexOfCondition = 0, marginVerticalConditions; //Индекс условия для Dictionary и переменная вертикального Margin 
        static List<Control> controls = new List<Control>(); // Коллекция элементов условий типа Conrol для возможности изменять параметры элемента, например Margin
        public static bool proverka = false; // поле для запуска класса по определению параметров
        public static string[,] uslovia = new string[0, 3]; // массив условий для параметров
        public static string[] unions = new string[0]; // массив И/ИЛИ между условиями
        public bool invert = false; // переменная для проверки нужно ли инвертировать выделение
        public static IList selectCategories = new List<string>(); //выбранные категории
        bool test = false; // проверка для возможности снятия выбора категории вручную
        IList preSelected = new List<string>(); // коллекция выбранных категорий до использования строки поиска
        public static List<string> exitSelect = new List<string>(); // итоговая выходная коллекция выбранных категорий для RevitAPI
        public ObservableCollection<string> exitParameters = new ObservableCollection<string>(); // выходные параметры для RevitAPI
        public ObservableCollection<string> storageTypesOfParameters = new ObservableCollection<string>(); // типы параметров
        public event EventHandler @event = null; // событие для RevitApi при нажатии «Применить» для выбора категорий
        public event EventHandler SearchingEvent = null; // событие для RevitAPI при нажатии «Найти и выбрать» для нахождения элементов Revit, подходящим по правилам
        public event EventHandler invertEvent = null; // событие для RevitAPI при нажатии «Инвертировать» для инвертирования выбора

        public ICommand command;

        public ViewModel()
        {
            //list = метод из Model 
            parameters.Clear();
            baseCollection.Clear();
            strings.Clear();
            selectCategories.Clear();
            controls.Clear();
            exitParameters.Clear();
            marginVerticalConditions = 20;
            foreach (string category in list)
            {

                baseCollection.Add(category);

            }
            
            foreach (string s in list)
            {
                strings.Add(s);
            }
            
        }
    }
}
