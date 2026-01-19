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
        private ObservableCollection<string> storageTypesOfParameters;
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
        public ICommand InvertSelectionCommand { get; }

        public ViewModel()
        {
            Categories = categoryService.GetCategoryNamesObservableAsync().Result; 
            ApplyCategoryCommand = new Contracts.CommandBinding(ApplyCategory);
            UpdateCategoriesCommand = new Contracts.CommandBinding(UpdateCollectionOfCategory);
            FamilySearchCommand = new Contracts.CommandBinding(FamilySearch);
            InvertSelectionCommand = new Contracts.CommandBinding(InvertSelection);

            for (int i = 0; i < Categories.Count; i++)
            {
                constantListOfCategories.Add(Categories[i]);
            }
        }



        private void ApplyCategory()
        {
            TextOfSearchPanel = "";
            Categories = selectedCategories;
            Troyan.SharedData.selectedCategoriesForFilter = new System.Collections.ObjectModel.ObservableCollection<string>(selectedCategories);
            Parameters = TroyankaCommand.GetParameterNamesForCategories(Categories); //метод по заполнению параметров
            storageTypesOfParameters = TroyankaCommand.GetParameterStorageTypesForCategories(Categories);// метод по заполнению типов параметров
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

        private void InvertSelection()
        {
            RevitNot.GOG();
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
            int indexOfColumn = 0, indexOfRow = 0, indexOfUnion = 0;
            const int maxControlsInRow = 3;
            for (int conditionIndex = 0; conditionIndex < Conditions.Count; conditionIndex++)
            {
                AddUslovia(conditionIndex,indexOfRow,ref indexOfColumn,ref currentText,ref currentParametr,ref breaking);

                if(breaking)
                    break;

                AddUnion(conditionIndex, ref indexOfUnion, ref breaking);

                if(breaking)
                    break;

                if (indexOfColumn == maxControlsInRow && !breaking)
                {
                    AnalysisOfStorageType(currentParametr, currentText, ref breaking);

                    if (breaking)
                        break;

                    indexOfRow++;
                    indexOfColumn = 0;
                }
            }
            if (!breaking)
            {
                RevitRuleFilter.ApplyFilterAndSelect(uslovia,unions);
                InvertButtonIsEnabled = true;
            }

        }

        private void DefineStorageTypeOfValue(Condition currentText, Condition currentParametr,string storageTypeOfParameter, ref StorageType storageType)
        {
            if (storageTypeOfParameter == "Double")
            {
                ReplacingPoints(ref currentText);
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

        private void ReplacingPoints(ref Condition currentText)
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
        }

        private void DefineStorageTypeOfCurrentParameter(ref string storageTypeOfParameter, Condition currentParametr)
        {
            int actualIndex = 0;

            for (int y = 0; y < Parameters.Count; y++)
            {
                if (Parameters[y] == currentParametr.SelectedValue.ToString())
                {
                    actualIndex = y;
                    continue;
                }
            }

            storageTypeOfParameter = storageTypesOfParameters[actualIndex];
        }

        private void CheckErrors(string storageTypeOfParameter, StorageType storageType,Condition currentText,ref bool breaking)
        {
            if (storageTypeOfParameter == "Integer" && (storageType == StorageType.Double || storageType == StorageType.String))
            {
                ErrorTextBox(currentText, StorageType.Integer);
                breaking = true;
                return;
            }

            if (storageTypeOfParameter == "Double" && storageType == StorageType.String)
            {
                ErrorTextBox(currentText, StorageType.Double);
                breaking = true;
                return;
            }
        }

        private void AnalysisOfStorageType(Condition currentParametr,Condition currentText,ref bool breaking)
        {
            StorageType storageTypeOfValue = StorageType.String;
            string storageTypeOfParameter = "String";

            DefineStorageTypeOfCurrentParameter(ref storageTypeOfParameter, currentParametr);

            DefineStorageTypeOfValue(currentText, currentParametr, storageTypeOfParameter, ref storageTypeOfValue);

            CheckErrors(storageTypeOfParameter, storageTypeOfValue, currentText, ref breaking);
        }

        private void AddRowForUslovia(int indexOfRow)
        {
            string[,] vremUsl = uslovia;
            uslovia = new string[indexOfRow + 1, 3];
            for (int q = 0; q < vremUsl.GetLength(0); q++)
            {
                for (int r = 0; r < 3; r++)
                {
                    uslovia[q, r] = vremUsl[q, r];
                }
            }
        }

        private void AddControl_ParameterOrCondition(int conditionIndex,int indexOfRow,int indexOfColumn, ref Condition currentParametr)
        {
            if (Conditions[conditionIndex].Name == "parametr")
            {
                currentParametr = Conditions[conditionIndex];
            }
            uslovia[indexOfRow, indexOfColumn] = (Conditions[conditionIndex].SelectedValue).ToString();
        }

        private void AddControl_Value(int conditionIndex, int indexOfRow, int indexOfColumn, ref Condition currentText)
        {
            Conditions[conditionIndex].Background = Brushes.White;
            currentText = Conditions[conditionIndex];
            uslovia[indexOfRow, indexOfColumn] = Conditions[conditionIndex].Text;
        }

        private void AddUslovia(int conditionIndex, int indexOfRow,ref int indexOfColumn, ref Condition currentText, ref Condition currentParametr,ref bool breaking)
        {
            if (Conditions[conditionIndex].Name != "close" && Conditions[conditionIndex].Name != "souz")
            {
                AddRowForUslovia(indexOfRow);

                if (Conditions[conditionIndex].Name == "parametr" || Conditions[conditionIndex].Name == "condition1")
                {
                    if (Conditions[conditionIndex].SelectedItem == null)
                    {
                        ShowErrorDialog();
                        breaking = true;
                        return;
                    }

                    AddControl_ParameterOrCondition(conditionIndex, indexOfRow, indexOfColumn, ref currentParametr);
                }

                if (Conditions[conditionIndex].Name == "Value")
                {
                    if (Conditions[conditionIndex].Text == "" || Conditions[conditionIndex].Text == null)
                    {
                        ShowErrorDialog();
                        breaking = true;
                        return;
                    }

                    AddControl_Value(conditionIndex, indexOfRow, indexOfColumn, ref currentText);
                }
                indexOfColumn++;
            }
        }

        private void AddRowForUnions(int indexOfUnion)
        {
            string[] vremUnion = unions;
            unions = new string[indexOfUnion + 1];
            for (int q = 0; q < vremUnion.Length; q++)
            {
                unions[q] = vremUnion[q];
            }
        }

        private void AddUnion(int conditionIndex,ref int indexOfUnion,ref bool breaking)
        {
            if (Conditions[conditionIndex].Name == "souz")
            {
                AddRowForUnions(indexOfUnion);

                if (Conditions[conditionIndex].SelectedItem == null)
                {
                    ShowErrorDialog();
                    breaking = true;
                    return;
                }
                unions[indexOfUnion] = Conditions[conditionIndex].SelectedValue.ToString();
                indexOfUnion++;
            }
        }
    }
}
