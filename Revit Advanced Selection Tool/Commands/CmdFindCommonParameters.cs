using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using RevitAdvancedSelectionTool.Services;

namespace Troyan
{
    [Transaction(TransactionMode.ReadOnly)]
    public class CmdFindCommonParameters : IExternalCommand
    {
        private readonly IFilterService _filterService;

        public CmdFindCommonParameters(IFilterService filterService)
        {
            _filterService = filterService;
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Получить общие параметры
                var commonParams = _filterService.GetAvailableParametersAsync(new List<string>(SharedData.exitSelect)).Result;

                // Показать результат
                if (commonParams.Any())
                {
                    string resultText = string.Join("\n",
                        commonParams.Select(p => $"{p.Name} > {p.StorageType}"));

                    TaskDialog.Show("Общие параметры",
                        $"Общих параметров: {commonParams.Count}\n\n{resultText}");
                }
                else
                {
                    TaskDialog.Show("Общие параметры",
                        "Общих параметров в выбранных категориях не найдено.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Ошибка", ex.ToString());
                return Result.Failed;
            }
        }
    }
}