// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Copyright (c) 2016 David Prothero

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace NgrokExtensions
{
    internal sealed class StartTunnel
    {
        private static readonly HashSet<string> PortPropertyNames = new HashSet<string>
        {
            "WebApplication.DevelopmentServerPort",
            "WebApplication.IISUrl",
            "WebApplication.CurrentDebugUrl",
            "WebApplication.NonSecureUrl",
            "WebApplication.BrowseURL",
            "NodejsPort", // Node.js project
            "FileName",    // Azure functions if ends with '.funproj'
            "ProjectUrl"
        };

        private static readonly Regex NumberPattern = new Regex(@"\d+");

        public const int CommandId = 0x0100;
        private const string NgrokSubdomainSettingName = "ngrok.subdomain";
        public static readonly Guid CommandSet = new Guid("30d1a36d-a03a-456d-b639-f28b9b23e161");
        //private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartTunnel"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private StartTunnel()
        {
			var webApp = new WebAppConfig();

			var ngrok = new NgrokUtils(webApp, "ngrok.exe", (string errorMessage) => { Console.WriteLine(errorMessage); return Task.FromResult(0); });

			ThreadHelper.JoinableTaskFactory.Run(async delegate
			{
				await ngrok.StartTunnelsAsync();
			});
		}

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static StartTunnel Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        //private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new StartTunnel();
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            
        }

        private async System.Threading.Tasks.Task ShowErrorMessageAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ShowErrorMessage(message);
        }

        private void ShowErrorMessage(string message)
        {
            //VsShellUtilities.ShowMessageBox(
            //    this.ServiceProvider,
            //    message,
            //    "ngrok",
            //    OLEMSGICON.OLEMSGICON_CRITICAL,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private bool AskUserYesNoQuestion(string message)
        {
			//var result = VsShellUtilities.ShowMessageBox(
			//    this.ServiceProvider,
			//    message,
			//    "ngrok",
			//    OLEMSGICON.OLEMSGICON_QUERY,
			//    OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
			//    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

			//return result == 6;  // Yes
			return false;
        }

        private bool IsAspNetCoreProject(string propName)
        {
            return propName == "ProjectUrl";
        }

        private static void LoadOptionsFromWebConfig(Project project, WebAppConfig webApp)
        {
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.Name.ToLower() != "web.config") continue;

                var path = item.FileNames[0];
                var webConfig = XDocument.Load(path);
                var appSettings = webConfig.Descendants("appSettings").FirstOrDefault();
                webApp.SubDomain = appSettings?.Descendants("add")
                    .FirstOrDefault(x => x.Attribute("key")?.Value == NgrokSubdomainSettingName)
                    ?.Attribute("value")?.Value;
                break;
            }
        }

        private static void LoadOptionsFromAppSettingsJson(Project project, WebAppConfig webApp)
        {
            // Read the settings from the project's appsettings.json first
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.Name.ToLower() != "appsettings.json") continue;

                ReadOptionsFromJsonFile(item.FileNames[0], webApp);
            }

            // Override any additional settings from the secrets.json file if it exists
            var userSecretsId = project.Properties.OfType<Property>()
                .FirstOrDefault(x => x.Name.Equals("UserSecretsId", StringComparison.OrdinalIgnoreCase))?.Value as String;

            if (string.IsNullOrEmpty(userSecretsId)) return;

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var secretsFile = Path.Combine(appdata, "Microsoft", "UserSecrets", userSecretsId, "secrets.json");

            ReadOptionsFromJsonFile(secretsFile, webApp);
        }

        private static void ReadOptionsFromJsonFile(string path, WebAppConfig webApp)
        {
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var appSettings = JsonConvert.DeserializeAnonymousType(json,
                new { IsEncrypted = false, Values = new Dictionary<string, string>() });
            
            if (appSettings.Values != null && appSettings.Values.TryGetValue(NgrokSubdomainSettingName, out var subdomain))
            {
                webApp.SubDomain = subdomain;
            }
        }

        private static void DebugWriteProp(Property prop)
        {
            try
            {
                Debug.WriteLine($"{prop.Name} = {prop.Value}");
            }
            catch
            {
                // ignored
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		/// 
		[Obsolete("Refactor to get this data through appsettings.json instead")]
		private IEnumerable<Project> GetSolutionProjects()
        {
			//var solution = (ServiceProvider.GetService(typeof(SDTE)) as DTE)?.Solution;
			//return solution == null ? null : ProcessProjects(solution.Projects.Cast<Project>());
			return new List<Project>();
        }

        private static IEnumerable<Project> ProcessProjects(IEnumerable<Project> projects)
        {
            var newProjectsList = new List<Project>();
            foreach (var p in projects)
            {

                if (p.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    newProjectsList.AddRange(ProcessProjects(GetSolutionFolderProjects(p)));
                }
                else
                {
                    newProjectsList.Add(p);
                }
            }

            return newProjectsList;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project project)
        {
            return project.ProjectItems.Cast<ProjectItem>()
                .Select(item => item.SubProject)
                .Where(subProject => subProject != null)
                .ToList();
        }
    }
}