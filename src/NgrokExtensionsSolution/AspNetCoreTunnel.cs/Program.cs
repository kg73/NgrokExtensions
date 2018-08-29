using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NgrokExtensions;
using Serilog;
using Serilog.Formatting.Compact;

namespace AspNetCoreTunnel.cs
{
	class Program
	{
		static async System.Threading.Tasks.Task Main(string[] args)
		{
			// Create service collection
			var serviceCollection = new ServiceCollection();
			ConfigureServices(serviceCollection);

			// Create service provider
			var serviceProvider = serviceCollection.BuildServiceProvider();

			// Run app
			//serviceProvider.GetServices().Run();

			var logger = serviceProvider.GetService<ILogger<Program>>();


			var webApp = new WebAppConfig()
			{
				PortNumber = 5000
			};

			//var logger = new ConsoleLogger

			var ngrok = new NgrokUtils(webApp, "ngrok.exe", serviceProvider.GetService<ILogger<NgrokUtils>>());

			var tunnels = await ngrok.StartTunnelsAsync();
			var httpsPreferredTunnel = tunnels.FirstOrDefault(t => t.proto == "https") ?? tunnels.FirstOrDefault();
			var publicUrl = httpsPreferredTunnel.public_url;

			logger.Log(LogLevel.Information, "Public Url: {publicUrl}", publicUrl);
			Console.WriteLine("Press Enter to close ngrok process");
			Console.ReadLine();
			await ngrok.StopNgrok();
			Console.WriteLine("Ngrok stopped");
			Console.WriteLine("Press Enter to close this app");
			Console.ReadLine();
			
		}

		private static void ConfigureServices(IServiceCollection serviceCollection)
		{
			// Add logging
			serviceCollection.AddSingleton(new LoggerFactory()
				//.AddConsole()
				.AddSerilog()
				);
			serviceCollection.AddLogging();

			// Build configuration
			var configuration = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", true)
				.Build();

			// Initialize serilog logger
			Log.Logger = new LoggerConfiguration()
				 .WriteTo.Console()
				 .MinimumLevel.Debug()
				 .Enrich.FromLogContext()
				 .CreateLogger();

			// Add access to generic IConfigurationRoot
			serviceCollection.AddSingleton(configuration);
		}
	}
}
