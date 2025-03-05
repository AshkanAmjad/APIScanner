using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerAPIProject.Models.Entities
{
    public class MenuPageApi
    {
        public int Id { get; set; }
        public string ApiUrl { get; set; }
        public string? RedirectUrl { get; set; } 
        public int MenuPageId { get; set; }
        public virtual MenuPage MenuPage { get; set; }
    }
}

