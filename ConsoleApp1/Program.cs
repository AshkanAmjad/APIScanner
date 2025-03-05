using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScannerAPIProject.Context;
using ScannerAPIProject.Services;

namespace ApiScannerConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    var connectionString = "Data Source=.;Initial Catalog=ApiScanner_DB;Integrated Security=True;TrustServerCertificate=true";
                    services.AddDbContext<ScannerAPIContext>(options =>
                        options.UseSqlServer(connectionString));

                    services.AddScoped<JsApiScannerService>();
                })
                .Build();

            var service = host.Services.GetRequiredService<JsApiScannerService>();

            while (true)
            {
                Console.WriteLine("1. Automatic Scan");
                Console.WriteLine("2. Exit");
                Console.Write("Choose: ");
                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    string rootPath = @"C:\Users\reza.o\source\repos\sida-cross-platform2\Pajoohesh.School.Web\wwwroot\Sida\App\views";
                    await service.ScanAndSaveAllControllersAsync(rootPath);
                }
                else if (choice == "2")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Error");
                }
            }
        }
    }
}
