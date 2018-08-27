using System;
using System.Threading.Tasks;
using NgrokExtensions;

namespace AspNetCoreTunnel.cs
{
	class Program
	{
		static async System.Threading.Tasks.Task Main(string[] args)
		{
			var webApp = new WebAppConfig()
			{
				PortNumber = 5000
			};

			var ngrok = new NgrokUtils(webApp, "ngrok.exe", (string error) => 
			{
				Console.WriteLine(error); return Task.FromResult(0);
			});

			var publicUrl = await ngrok.StartTunnelsAsync();

			Console.WriteLine($"Public Url: {publicUrl}");
			Console.ReadLine();
		}
	}
}
