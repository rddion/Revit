using Autodesk.Revit.UI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace Troyan
{
    internal class Button : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location,
                icons = Path.GetDirectoryName(assemblyLocation) + @"\icons\",
                tabName = "Плагин P.P.";
            application.CreateRibbonTab(tabName);
            RibbonPanel panell = application.CreateRibbonPanel(tabName, "Список");
            PushButtonData buttomData = new PushButtonData(nameof(TroyankaCommand), "Запуск", assemblyLocation, typeof(TroyankaCommand).FullName)
            {
                LargeImage = new BitmapImage(new Uri(icons + "Revit.png"))
            };
            panell.AddItem(buttomData);

            return Result.Succeeded;

        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}