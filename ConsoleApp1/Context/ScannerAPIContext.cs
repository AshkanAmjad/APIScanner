using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScannerAPIProject.Models.Entities;

namespace ScannerAPIProject.Context
{
    public class ScannerAPIContext : DbContext
    {
        public ScannerAPIContext(DbContextOptions<ScannerAPIContext> options) : base(options) { }

        public virtual DbSet<MenuPage> MenuPages { get; set; }
        public virtual DbSet<MenuPageApi> MenuPageApis { get; set; }
    }
}
