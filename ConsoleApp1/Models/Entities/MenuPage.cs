using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiScannerConsole;

namespace ScannerAPIProject.Models.Entities
{
    public class MenuPage
    {
        public int Id { get; set; }
        public string FolderName { get; set; }
        public string ControllerName { get; set; }
        public ICollection<MenuPageApi> MenuPageApis { get; set; }
    }
}
