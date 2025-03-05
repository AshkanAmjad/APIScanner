using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ScannerAPIProject.Context;
using ScannerAPIProject.Models;

namespace ScannerAPIProject.Services
{
    public class JsApiScannerService
    {
        private readonly ScannerAPIContext _context;

        public JsApiScannerService(ScannerAPIContext context)
        {
            _context = context;
        }

        public async Task ScanAndSaveAllControllersAsync(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine($"Not Found => {rootPath}");
                return;
            }

            var jsFiles = Directory.GetFiles(rootPath, "*controller.js", SearchOption.AllDirectories);

            foreach (var jsFile in jsFiles)
            {
                string fileContent = await File.ReadAllTextAsync(jsFile);
                string controllerName = Path.GetFileNameWithoutExtension(jsFile);
                string folderPath = Path.GetDirectoryName(jsFile);
                string folderName = new DirectoryInfo(folderPath).Name;

                var existingPage = await _context.MenuPages
                    .FirstOrDefaultAsync(p => p.ControllerName == controllerName && p.FolderName == folderName);

                if (existingPage == null)
                {
                    existingPage = new MenuPage
                    {
                        FolderName = folderName,
                        ControllerName = controllerName
                    };
                    _context.MenuPages.Add(existingPage);
                    await _context.SaveChangesAsync();
                }

                var apiUrls = ExtractApiEndpoints(fileContent);
                var redirects = ExtractRedirects(fileContent);
                var popUps = ExtractPopupEndpoints(fileContent);
                apiUrls.AddRange(popUps);

                foreach (var api in apiUrls)
                {
                    string redirectUrl = redirects.FirstOrDefault() ?? string.Empty; // اگر مقدار ریدایرکت یافت نشد، از رشته خالی استفاده کن

                    if (!_context.MenuPageApis.Any(a => a.ApiUrl == api && a.MenuPageId == existingPage.Id))
                    {
                        _context.MenuPageApis.Add(new MenuPageApi
                        {
                            ApiUrl = api,
                            RedirectUrl = redirectUrl, // استفاده از مقدار ریدایرکت یا رشته خالی
                            MenuPageId = existingPage.Id
                        });
                        await _context.SaveChangesAsync();
                    }
                }


                var menuPageApis = await _context.MenuPageApis
                    .Where(a => a.MenuPageId == existingPage.Id)
                    .ToListAsync();

                Console.WriteLine($"Folder: {folderName}");
                Console.WriteLine($"Controller: {controllerName}");
                Console.WriteLine("APIs:");
                foreach (var api in menuPageApis)
                {
                    Console.WriteLine($"  - {api.ApiUrl} (Redirect: {api.RedirectUrl})");
                }
            }
        }

        public async Task AddManualEntryAsync()
        {
            Console.WriteLine("enter folder name :");
            string folderName = Console.ReadLine();

            Console.WriteLine("enter controller name :");
            string controllerName = Console.ReadLine();

            var existingPage = await _context.MenuPages
                .FirstOrDefaultAsync(p => p.ControllerName == controllerName && p.FolderName == folderName);

            if (existingPage == null)
            {
                existingPage = new MenuPage
                {
                    FolderName = folderName,
                    ControllerName = controllerName
                };
                _context.MenuPages.Add(existingPage);
                await _context.SaveChangesAsync();
            }

            Console.WriteLine("Enter the API (press Enter to finish):");
            while (true)
            {
                string apiUrl = Console.ReadLine();
                if (string.IsNullOrEmpty(apiUrl)) break;

                Console.WriteLine("Enter the redirect URL (press Enter to skip):");
                string redirectUrl = Console.ReadLine();

                if (!_context.MenuPageApis.Any(a => a.ApiUrl == apiUrl && a.MenuPageId == existingPage.Id))
                {
                    _context.MenuPageApis.Add(new MenuPageApi
                    {
                        ApiUrl = apiUrl,
                        RedirectUrl = redirectUrl, // وارد کردن ریدایرکت برای هر API
                        MenuPageId = existingPage.Id
                    });
                    await _context.SaveChangesAsync();
                }
            }

            Console.WriteLine("Information added successfully!");
        }

        private List<string> ExtractApiEndpoints(string fileContent)
        {
            var apiUrls = new List<string>();

            var regexPatterns = new List<string>
            {
                @"['""](\/api\/[\w\/-]+)['""]",
                @"['""](\/api\/[^'""?]+\?[^'""]+)['""]",
                @"`(\/api\/[^`]+)`",
                @"\$scope\.\w+API\s*=\s*['""]([^'""]+)['""]",
                @"['""](api\/[^'""?]+|\/api\/[^'""?]+)['""]",
                @"url:\s*['""](api\/[^'""?]+|\/api\/[^'""?]+)['""]",
                @"api/[^\""?']+"
            };

            foreach (var pattern in regexPatterns)
            {
                var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var apiUrl = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(apiUrl))
                    {
                        apiUrls.Add(apiUrl);
                    }
                }
            }

            return apiUrls.Distinct().ToList();
        }


        private List<string> ExtractPopupEndpoints(string fileContent)
        {
            var apiUrls = new List<string>();

            var regexPatterns = new List<string>
            {
               @"\/Sida\/App\/directives\/[a-zA-Z0-9_]+\.js"  
            };

            string basePath = @"C:\Users\reza.o\source\repos\sida-cross-platform2\Pajoohesh.School.Web\wwwroot\";
            var fullPaths = new List<string>();

            foreach (var pattern in regexPatterns)
            {
                var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    var apiUrl = match.Value.Trim(); 
                    if (!string.IsNullOrEmpty(apiUrl) && !apiUrls.Contains(apiUrl))
                    {
                        apiUrls.Add(apiUrl);
                    }
                }
            }

            foreach (var item in apiUrls)
            {
                string correctedPath = item.Replace("/", "\\");
                string fullPath = Path.Combine(basePath, correctedPath.TrimStart('\\'));
                fullPaths.Add(fullPath);
            }

            var result = ExtractApiFromDirectives(fullPaths);

            return result;
        }



        private List<string> ExtractRedirects(string fileContent)
        {
            var redirects = new List<string>();

            var regexPatterns = new List<string>
            {
                @"\$state\.go\(['""]([^'"",\s\)]+)['""]\s*\)",   // برای $state.go('stateName') و $state.go("stateName")
                @"\$state\.go\(['""]([^'"",\s\)]+)['""]\s*,",    // برای $state.go('stateName', { ... })
                @"\$state\.go\(`([^`]+)`\)",                    // برای $state.go(`stateName`)
                @"\$state\.go\(\s*([\w\d_]+)\s*\)",             // برای $state.go(stateName) بدون کوتیشن
                @"\$state\.go\(\s*['""]([^'""]+)['""],\s*\{",   // برای $state.go("stateName", {...})
            };

            foreach (var pattern in regexPatterns)
            {
                var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var redirectUrl = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(redirectUrl))
                    {
                        redirects.Add(redirectUrl);
                        Console.WriteLine($"Matched: {match.Value} → Extracted: {redirectUrl}"); // برای بررسی همه موارد
                    }
                }
            }

            return redirects.Distinct().ToList();
        }

        public List<string> ExtractApiFromDirectives(List<string> directives)
        {
            Regex apiPattern = new Regex(@"(?:\/)?api\/[^\""""?']+[^\.html]", RegexOptions.IgnoreCase);

            string[] jsFiles = directives.ToArray();

            List<string> apis = new();


            foreach (var file in jsFiles)
            {
                string[] lines = File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    var apiMatch = apiPattern.Match(line);
                    if (apiMatch.Success)
                    {
                        string apiRoute = apiMatch.Value
                                                  .Trim()
                                                  .ToLower();

                        apiRoute =  apiRoute.TrimEnd('\'');

                        if (!apis.Contains(apiRoute))
                        {
                            apis.Add(apiRoute);
                        }

                    }

                }
            }

            return apis;

        }
    }
}





