using RevitAdvancedSelectionTool.Core;
using RevitAdvancedSelectionTool.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Converters;
using Troyan;
using Wpf.Constants;

namespace Wpf.ViewModel
{
    internal class ViewModel : INotifyPropertyChanged
    {
        private CategoryService categoryService = new CategoryService(); // экземпляр revitApi

        private ObservableCollection<string> constantListOfCategories = new ObservableCollection<string>();
        private ObservableCollection<string> categories_ChangeableCollection;
        private ObservableCollection<string> parameters;
        private ObservableCollection<string> storagetTypesOfParameters;
        private ObservableCollection<string> selectedCategories = new ObservableCollection<string>();
        private ObservableCollection<string> previouslySelectedCategories = new ObservableCollection<string>();
        private HashSet<string> temporaryPreviouslySelected = new HashSet<string>();
        private ObservableCollection<Condition> conditions = new ObservableCollection<Condition>();

        private bool invertButtonIsEnabled=false;

        private string[,] uslovia;
        private string[] unions;

        private string textOfSearchPanel;
        private string selectionCountState;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool InvertButtonIsEnabled
        {
            get { return invertButtonIsEnabled; }
            set 
            { 
                invertButtonIsEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.InvertButtonIsEnabled)));
            }
        }
        public ObservableCollection<Condition> Conditions
        {
            get { return conditions; }
            set { conditions = value; }
        }
        public ObservableCollection<string> PreviouslySelectedCategories
        {
            get { return previouslySelectedCategories; }
            set { previouslySelectedCategories = value; }
        }
        public ObservableCollection<string> Categories
        {
            get { return categories_ChangeableCollection; }
            set
            {
                categories_ChangeableCollection = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Categories)));
            }
        }
        public ObservableCollection<string> SelectedCategories
        {
            get { return selectedCategories; }
            set { selectedCategories = value; }
        }
        public ObservableCollection<string> Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Parameters)));
            }
        }
        public string SelectionCountState
        {
            get { return selectionCountState; }
            set
            {
                selectionCountState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectionCountState)));
            }
        }
        public string TextOfSearchPanel
        {
            get { return textOfSearchPanel; }
            set
            {
                textOfSearchPanel = value;
                SearchPanelChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.TextOfSearchPanel)));
            }
        }

        public ICommand ApplyCategoryCommand { get; }
        public ICommand UpdateCategoriesCommand { get; }
        public ICommand FamilySearchCommand { get; }

        public ViewModel()
        {
            Categories = categoryService.GetCategoryNamesObservableAsync().Result; 
            ApplyCategoryCommand = new Contracts.CommandBinding(ApplyCategory);
            UpdateCategoriesCommand = new Contracts.CommandBinding(UpdateCollectionOfCategory);
            FamilySearchCommand = new Contracts.CommandBinding(FamilySearch);

            for (int i = 0; i < Categories.Count; i++)
            {
                constantListOfCategories.Add(Categories[i]);
            }
        }



        private void ApplyCategory()
        {
            TextOfSearchPanel = "";
            Categories = selectedCategories;
            Parameters = TroyankaCommand.GetParameterNamesForCategories(Categories); //метод по заполнению параметров
            storagetTypesOfParameters = TroyankaCommand.GetParameterStorageTypesForCategories(Categories);// метод по заполнению типов параметров
        }

        private void UpdateCollectionOfCategory()
        {
            Categories = new ObservableCollection<string>();
            foreach (var category in constantListOfCategories)
            {
                Categories.Add(category);
            }
            TextOfSearchPanel = null;
            try
            {
                Parameters.Clear();
            }
            catch { }
            Conditions.Clear();
            InvertButtonIsEnabled = false;
        }

        private void SearchPanelChanged()
        {

            foreach (var selectedCategory in selectedCategories)
            {
                temporaryPreviouslySelected.Add(selectedCategory);
            }
 
            temporaryPreviouslySelected = temporaryPreviouslySelected.Where(it => !(Categories.Contains(it)) || SelectedCategories.Contains(it)).ToHashSet();

            if (TextOfSearchPanel != null)
            {
                Categories.Clear();
                foreach (var category in constantListOfCategories)
                {
                    if ((category.ToLower()).Contains((TextOfSearchPanel.ToString()).ToLower()))
                    {
                        Categories.Add(category);
                    }
                }
            }
            else
            {
                Categories.Clear();
                foreach (var category in constantListOfCategories)
                {
                    Categories.Add(category);
                }
            }

            if (temporaryPreviouslySelected.Count > 0)
            {
                PreviouslySelectedCategories.Clear();
                foreach (var category in temporaryPreviouslySelected)
                {
                    PreviouslySelectedCategories.Add(category);
                }

                if (TextOfSearchPanel.Count<char>() == 0)
                {
                    previouslySelectedCategories.Clear();
                    temporaryPreviouslySelected.Clear();
                }
            }

        }

        private void FamilySearch()
        {
            Condition currentText = null;
            Condition currentParametr = null;
            bool breaking = false;
            uslovia = new string[0, 3];
            unions = new string[0];
            int j = 0, k = 0, x = 0;
            for (int i = 0; i < Conditions.Count; i++)
            {
                if (Conditions[i].Name != "close" && Conditions[i].Name != "souz")
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

                    if (Conditions[i].Name == "parametr" || Conditions[i].Name == "condition1")
                    {
                        if (Conditions[i].SelectedItem == null)
                        {
                            ShowErrorDialog();
                            breaking = true;
                            break;
                        }

                        if (Conditions[i].Name == "parametr")
                        {
                            currentParametr = Conditions[i];
                        }
                        uslovia[k, j] = (Conditions[i].SelectedValue).ToString();
                    }
                    if (Conditions[i].Name == "Value")
                    {
                        if (Conditions[i].Text == "" || Conditions[i].Text==null)
                        {
                            ShowErrorDialog();
                            breaking = true;
                            break;
                        }
                        Conditions[i].Background = Brushes.White;
                        currentText = Conditions[i];
                        uslovia[k, j] = Conditions[i].Text;
                    }
                    j++;
                }



                if (Conditions[i].Name == "souz")
                {
                    string[] vremUnion = unions;
                    unions = new string[x + 1];
                    for (int q = 0; q < vremUnion.Length; q++)
                    {
                        unions[q] = vremUnion[q];
                    }
                    if (Conditions[i].SelectedItem == null)
                    {
                        ShowErrorDialog();
                        breaking = true;
                        break;
                    }
                    unions[x] = Conditions[i].SelectedValue.ToString();
                    x++;
                }

                if (j == 3 && !breaking)
                {
                    StorageType storageType = StorageType.String;
                    int actualIndex = 0;
                    
                    DefineStorageType(currentText,currentParametr,ref actualIndex, ref storageType);

                    if (storagetTypesOfParameters[actualIndex] == "Integer" && (storageType == StorageType.Double || storageType == StorageType.String))
                    {
                        ErrorTextBox(currentText, StorageType.Integer);
                        breaking = true;
                        break;
                    }

                    if (storagetTypesOfParameters[actualIndex] == "Double" && storageType == StorageType.String)
                    {
                        ErrorTextBox(currentText, StorageType.Double);
                        breaking = true;
                        break;
                    }

                    k++;
                    j = 0;
                }
            }
            if (!breaking)
            {
                RevitRuleFilter.ApplyFilterAndSelect(uslovia,unions);
                InvertButtonIsEnabled = true;
            }

        }

        private void DefineStorageType(Condition currentText, Condition currentParametr, ref int actualIndex, ref StorageType storageType)
        {
            Regex regex = new Regex(@"^\d*\.\d*$");

            if (regex.IsMatch(currentText.Text))
            {
                try
                {
                    Convert.ToInt32(Regex.Replace(currentText.Text, @"\.", ""));
                    currentText.Text = Regex.Replace(currentText.Text, @"\.", ",");
                }
                catch { }
            }

            for (int y = 0; y < Parameters.Count; y++)
            {
                if (Parameters[y] == currentParametr.SelectedValue.ToString())
                {
                    actualIndex = y;
                    continue;
                }
            }

            try
            {
                Convert.ToInt32(currentText.Text);
                storageType = StorageType.Integer;
            }
            catch { }

            if (storageType != StorageType.Integer)
            {
                try
                {
                    Convert.ToDouble(currentText.Text);
                    storageType = StorageType.Double;
                }
                catch { }
            }

        }

        public void SelectionCategoriesChanged(IList selection)
        {
            SelectedCategories.Clear();
            foreach (var category in selection)
            {
                SelectedCategories.Add(category.ToString());
            }

            SelectionCountState = String.Format("Выбрано {0} категорий", SelectedCategories.Count);
        }

        private void ShowErrorDialog()
        {
            Window window = new Window();
            window.Title = "Ошибка";
            window.Width = 400;
            window.Height = 150;
            window.HorizontalAlignment = HorizontalAlignment.Center;
            window.VerticalAlignment = VerticalAlignment.Center;
            window.Margin = new Thickness(0, 0, 0, 0);
            window.Content = "Ошибка: Не заполнены поля условий в конструкторе правил";
            window.Activate();
            window.Topmost = true;
            window.ShowDialog();
        }

        private void ErrorTextBox(Condition textBox, StorageType storageType)
        {
            BrushValueSerializer brushValueSerializer = new BrushValueSerializer();
            textBox.Background = (Brush)brushValueSerializer.ConvertFromString("#FFF18B8B", null);
            textBox.Background.Opacity = 70;
            switch (storageType)
            {
                case StorageType.Double:
                    textBox.Text = String.Format("Введите число");
                    break;

                case StorageType.Integer:
                    textBox.Text = String.Format("Введите целое число");
                    break;

                default:
                    textBox.Text = String.Format("Введите число");
                    break;
            }

        }

    }
}
