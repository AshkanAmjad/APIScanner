
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace ApiScannerConsole
//{
//    public class MenuPage
//    {
//        public int Id { get; set; }
//        public string FolderName { get; set; }
//        public string ControllerName { get; set; }
//        public ICollection<MenuPageApi> MenuPageApis { get; set; }
//    }

//    public class MenuPageApi
//    {
//        public int Id { get; set; }
//        public string ApiUrl { get; set; }
//        public int MenuPageId { get; set; }

//        public string RedirectUrl { get; set; }
//        public MenuPage MenuPage { get; set; }
//    }

   
//    public class ApplicationDbContext : DbContext
//    {
//        public DbSet<MenuPage> MenuPages { get; set; }
//        public DbSet<MenuPageApi> MenuPageApis { get; set; }

//        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
//    }

//    public class JsApiScannerService
//    {
//        private readonly ApplicationDbContext _context;

//        public JsApiScannerService(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task ScanAndSaveAllControllersAsync(string rootPath)
//        {
//            if (!Directory.Exists(rootPath))
//            {
//                Console.WriteLine($"مسیر مشخص‌شده '{rootPath}' وجود ندارد!");
//                return;
//            }

//            var jsFiles = Directory.GetFiles(rootPath, "*controller.js", SearchOption.AllDirectories);

//            foreach (var jsFile in jsFiles)
//            {
//                string fileContent = await File.ReadAllTextAsync(jsFile);
//                string controllerName = Path.GetFileNameWithoutExtension(jsFile);
//                string folderPath = Path.GetDirectoryName(jsFile);
//                string folderName = new DirectoryInfo(folderPath).Name;

//                var existingPage = await _context.MenuPages
//                    .FirstOrDefaultAsync(p => p.ControllerName == controllerName && p.FolderName == folderName);

//                if (existingPage == null)
//                {
//                    existingPage = new MenuPage
//                    {
//                        FolderName = folderName,
//                        ControllerName = controllerName
//                    };
//                    _context.MenuPages.Add(existingPage);
//                    await _context.SaveChangesAsync();
//                }

//                var apiUrls = ExtractApiEndpoints(fileContent);
//                foreach (var api in apiUrls)
//                {
//                    if (!_context.MenuPageApis.Any(a => a.ApiUrl == api && a.MenuPageId == existingPage.Id && a.RedirectUrl == api))
//                    {
//                        _context.MenuPageApis.Add(new MenuPageApi
//                        {
//                            ApiUrl = api,
//                            MenuPageId = existingPage.Id,
//                            RedirectUrl = api
//                        });
//                        await _context.SaveChangesAsync();
//                    }
//                }

                

//                var menuPageApis = await _context.MenuPageApis
//                    .Where(a => a.MenuPageId == existingPage.Id)
//                    .ToListAsync();


//                Console.WriteLine($"فولدر: {folderName}");
//                Console.WriteLine($"کنترلر: {controllerName}");
//                Console.WriteLine("APIهای موجود:");
//                foreach (var api in menuPageApis)
//                {
//                    Console.WriteLine($"  - {api.ApiUrl}");
//                }

//            }
//        }

//        public async Task AddManualEntryAsync()
//        {
//            Console.WriteLine("enter folder name :");
//            string folderName = Console.ReadLine();

//            Console.WriteLine("enter contorller name :");
//            string controllerName = Console.ReadLine();

//            var existingPage = await _context.MenuPages
//                .FirstOrDefaultAsync(p => p.ControllerName == controllerName && p.FolderName == folderName);

//            if (existingPage == null)
//            {
//                existingPage = new MenuPage
//                {
//                    FolderName = folderName,
//                    ControllerName = controllerName
//                };
//                _context.MenuPages.Add(existingPage);
//                await _context.SaveChangesAsync();
//            }

//            Console.WriteLine("Enter the API (press Enter to finish):");
//            while (true)
//            {
//                string apiUrl = Console.ReadLine();
//                if (string.IsNullOrEmpty(apiUrl)) break;

//                if (!_context.MenuPageApis.Any(a => a.ApiUrl == apiUrl && a.MenuPageId == existingPage.Id))
//                {
//                    _context.MenuPageApis.Add(new MenuPageApi
//                    {
//                        ApiUrl = apiUrl,
//                        MenuPageId = existingPage.Id
//                    });
//                    await _context.SaveChangesAsync();
//                }
//            }

//            Console.WriteLine("Information added successfully!");
//        }

//        private List<string> ExtractApiEndpoints(string fileContent)
//        {
//            var apiUrls = new List<string>();


//            var regexPatterns = new List<string>
//{
//    @"['""](\/api\/[\w\/-]+)['""]",
//    @"['""](\/api\/[^'""?]+\?[^'""]+)['""]",
//    @"`(\/api\/[^`]+)`",
//    @"\$scope\.\w+API\s*=\s*['""]([^'""]+)['""]",
//    @"['""](api\/[^'""?]+|\/api\/[^'""?]+)['""]",
//    @"url:\s*['""](api\/[^'""?]+|\/api\/[^'""?]+)['""]",
//};

//            var regexPatterns1 = new List<string>
//    {
//        @"\$state\.go\(['""]([^'"",\s\)]+)['""]\s*\)",   // برای $state.go('stateName') و $state.go("stateName")
//        @"\$state\.go\(['""]([^'"",\s\)]+)['""]\s*,",    // برای $state.go('stateName', { ... })
//        @"\$state\.go\(`([^`]+)`\)",                    // برای $state.go(`stateName`)
//        @"\$state\.go\(\s*([\w\d_]+)\s*\)",             // برای $state.go(stateName) بدون کوتیشن
//        @"\$state\.go\(\s*['""]([^'""]+)['""],\s*\{",   // برای $state.go("stateName", {...})
//    };

//            foreach (var pattern in regexPatterns)
//            {
//                var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase);
//                foreach (Match match in matches)
//                {
//                    var apiUrl = match.Groups[1].Value.Trim();
//                    if (!string.IsNullOrEmpty(apiUrl))
//                    {
//                        apiUrls.Add(apiUrl);
//                    }
//                }
//            }

//            foreach (var pattern in regexPatterns1)
//            {
//                var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase);
//                foreach (Match match in matches)
//                {
//                    var apiUrl = match.Groups[1].Value.Trim();
//                    if (!string.IsNullOrEmpty(apiUrl))
//                    {
//                        apiUrls.Add(apiUrl);
//                    }
//                }
//            }

//            return apiUrls.Distinct().ToList();
//        }

//        private List<string> ExtractRedirects(string fileContent)
//        {
//            var redirects = new List<string>();

//            var regexPatterns = new List<string>
//    {
//        @"\$state\.go\(['""]([^'"",\s\)]+)['""]\s*\)",   // برای $state.go('stateName') و $state.go("stateName")
//        @"\$state\.go\(['""]([^'"",\s\)]+)['""]\s*,",    // برای $state.go('stateName', { ... })
//        @"\$state\.go\(`([^`]+)`\)",                    // برای $state.go(`stateName`)
//        @"\$state\.go\(\s*([\w\d_]+)\s*\)",             // برای $state.go(stateName) بدون کوتیشن
//        @"\$state\.go\(\s*['""]([^'""]+)['""],\s*\{",   // برای $state.go("stateName", {...})
//    };

//            foreach (var pattern in regexPatterns)
//            {
//                var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase);
//                foreach (Match match in matches)
//                {
//                    var redirectUrl = match.Groups[1].Value.Trim();
//                    if (!string.IsNullOrEmpty(redirectUrl))
//                    {
//                        redirects.Add(redirectUrl);
//                        Console.WriteLine($"Matched: {match.Value} → Extracted: {redirectUrl}"); // برای بررسی همه موارد
//                    }
//                }
//            }

//            return redirects.Distinct().ToList();
//        }

//    }

//    class Program
//    {
//        static async Task Main(string[] args)
//        {
//            var host = Host.CreateDefaultBuilder(args)
//                .ConfigureServices((context, services) =>
//                {
//                    var connectionString = "Data Source=DESKTOP-912MTT6;Initial Catalog=ApiScanner;Integrated Security=True;TrustServerCertificate=true";
//                    services.AddDbContext<ApplicationDbContext>(options =>
//                        options.UseSqlServer(connectionString));

//                    services.AddScoped<JsApiScannerService>();
//                })
//                .Build();

//            var service = host.Services.GetRequiredService<JsApiScannerService>();

//            while (true)
//            {
//                Console.WriteLine("1. Atumatic Scan");
//                Console.WriteLine("2. Add Your Api and Redirect");
//                Console.WriteLine("3. Exit");
//                Console.Write("chooose: ");
//                string choice = Console.ReadLine();

//                if (choice == "1")
//                {
//                    string rootPath = @"F:\new\sida-cross-platform-main\Pajoohesh.School.Web\wwwroot\Sida\App\views";
//                    await service.ScanAndSaveAllControllersAsync(rootPath);
//                }
//                else if (choice == "2")
//                {
//                    await service.AddManualEntryAsync();
//                }
//                else if (choice == "3")
//                {
//                    break;
//                }
//                else
//                {
//                    Console.WriteLine("wrong option");
//                }
//            }
//        }
//    }
//}
