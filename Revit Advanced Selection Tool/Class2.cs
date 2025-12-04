using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using YourRevitPluginNamespace;

namespace Troyan
{
    internal class Button : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location,
                icons = Path.GetDirectoryName(assemblyLocation) + @"\icons\",
                tabName = "Тайна Revit api";
            application.CreateRibbonTab(tabName);
            RibbonPanel panell = application.CreateRibbonPanel(tabName, "КВН");
            PushButtonData buttomData = new PushButtonData(nameof(Troyanka), "Галустян", assemblyLocation, typeof(Troyanka).FullName)
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