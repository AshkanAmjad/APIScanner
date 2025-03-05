using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ScannerAPIProject.Context;
using ScannerAPIProject.Models;
using ScannerAPIProject.Models.Entities;

namespace ScannerAPIProject.Services
{
    public class JsApiScannerService
    {
        private readonly ScannerAPIContext _context;

        public JsApiScannerService(ScannerAPIContext context)
        {
            _context = context;
        }

        public async Task ScanAndSaveAllControllersAndApisAsync(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine($"Not Found => {rootPath}");
                return;
            }
            HashSet<MenuPageApi> menuPageApi = new();

            var jsFiles = Directory.GetFiles(rootPath, "*controller.js", SearchOption.AllDirectories);

            foreach (var jsFile in jsFiles)
            {
                string fileContent = await File.ReadAllTextAsync(jsFile);
                string controllerName = Path.GetFileNameWithoutExtension(jsFile);
                string folderPath = Path.GetDirectoryName(jsFile);
                string folderName = new DirectoryInfo(folderPath).Name;

                var existingPage = await _context.MenuPages.Where(p => p.ControllerName == controllerName && p.FolderName == folderName) 
                                                           .FirstOrDefaultAsync();

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


                if (apiUrls.Count>0)
                {
                    foreach (var api in apiUrls)
                    {
                        string redirectUrl = redirects.FirstOrDefault() ?? string.Empty; // اگر مقدار ریدایرکت یافت نشد، از رشته خالی استفاده کن

                        menuPageApi.Add(new MenuPageApi
                        {
                            ApiUrl = api,
                            RedirectUrl = redirectUrl,
                            MenuPageId = existingPage.Id
                        });

                    }
                }
            }

            if (menuPageApi.Count>0)
            {
                _context.MenuPageApis.AddRange(menuPageApi);
                await _context.SaveChangesAsync();
            }
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
            var popups = new List<string>();

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
                    if (!string.IsNullOrEmpty(apiUrl) && !popups.Contains(apiUrl))
                    {
                        popups.Add(apiUrl);
                    }
                }
            }

            foreach (var item in popups)
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
                    }
                }
            }

            return redirects.Distinct().ToList();
        }

        public List<string> ExtractApiFromDirectives(List<string> directives)
        {
            Regex apiPattern = new Regex(@"(?:\/)?api\/[^\""""?']+[^\.html]", RegexOptions.IgnoreCase);

            string[] jsFiles = directives.ToArray();

            List<string> apiUrls = new();


            foreach (var file in jsFiles)
            {
                string[] lines = File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    var apiMatch = apiPattern.Match(line);
                    if (apiMatch.Success)
                    {
                        string apiRoute = apiMatch.Value
                                                  .Trim();

                        apiRoute = apiRoute.TrimEnd('\"', '\'');

                        if (!apiUrls.Contains(apiRoute))
                        {
                            apiUrls.Add(apiRoute);
                        }
                    }
                }
            }
            return apiUrls;
        }
    }
}





