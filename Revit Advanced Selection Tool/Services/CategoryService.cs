using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using RevitAdvancedSelectionTool.Models;

namespace RevitAdvancedSelectionTool.Services
{
    public class CategoryService : ICategoryService
    {
        public async Task<List<Category>> LoadCategoriesAsync()
        {
            // Реализация загрузки категорий
            return new List<Category>();
        }

        public async Task<List<Category>> FilterCategoriesAsync(string searchText)
        {
            var allCategories = await LoadCategoriesAsync();
            if (string.IsNullOrWhiteSpace(searchText))
                return allCategories;

            return allCategories.Where(c => c.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        public async Task SaveCategorySelectionAsync(List<Category> selectedCategories)
        {
            // Сохранить выбранные категории в SharedData для совместимости
            // SharedData.exitSelect = selectedCategories.Select(c => c.Name).ToList();
        }

        public async Task<ObservableCollection<Category>> GetCategoriesObservableAsync()
        {
            var categories = await LoadCategoriesAsync();
            return new ObservableCollection<Category>(categories);
        }
    }
}