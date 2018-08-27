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

        

        //private static void LoadOptionsFromWebConfig(Project project, WebAppConfig webApp)
        //{
        //    foreach (ProjectItem item in project.ProjectItems)
        //    {
        //        if (item.Name.ToLower() != "web.config") continue;

        //        var path = item.FileNames[0];
        //        var webConfig = XDocument.Load(path);
        //        var appSettings = webConfig.Descendants("appSettings").FirstOrDefault();
        //        webApp.SubDomain = appSettings?.Descendants("add")
        //            .FirstOrDefault(x => x.Attribute("key")?.Value == NgrokSubdomainSettingName)
        //            ?.Attribute("value")?.Value;
        //        break;
        //    }
        //}

        //private static void LoadOptionsFromAppSettingsJson(Project project, WebAppConfig webApp)
        //{
        //    // Read the settings from the project's appsettings.json first
        //    foreach (ProjectItem item in project.ProjectItems)
        //    {
        //        if (item.Name.ToLower() != "appsettings.json") continue;

        //        ReadOptionsFromJsonFile(item.FileNames[0], webApp);
        //    }

        //    // Override any additional settings from the secrets.json file if it exists
        //    var userSecretsId = project.Properties.OfType<Property>()
        //        .FirstOrDefault(x => x.Name.Equals("UserSecretsId", StringComparison.OrdinalIgnoreCase))?.Value as String;

        //    if (string.IsNullOrEmpty(userSecretsId)) return;

        //    var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        //    var secretsFile = Path.Combine(appdata, "Microsoft", "UserSecrets", userSecretsId, "secrets.json");

        //    ReadOptionsFromJsonFile(secretsFile, webApp);
        //}

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
    }
}