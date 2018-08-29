using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Ngrok.AspNetCore
{
	public static class RegistrationExtensions
	{

		public static NgrokBuilder AddNgrokBuilder(this IServiceCollection services)
		{
			return new NgrokBuilder(services);
		}

		public static NgrokBuilder AddAtlassianConnect(this IServiceCollection services, Action<NgrokOptions> options)
		{
			var builder = new NgrokBuilder(services);

			builder.Services.Configure(options);

			//builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			builder.Services.AddOptions();
			builder.Services.AddSingleton(
				resolver => resolver.GetRequiredService<IOptions<NgrokOptions>>().Value);

			return builder;
		}
	}

	public class NgrokOptions
	{
		public int Port { get; set; }
	}

	public class NgrokBuilder
	{
		public NgrokBuilder(IServiceCollection services)
		{
			Services = services ?? throw new ArgumentNullException(nameof(services));
		}
		public IServiceCollection Services { get; }
	}
}
