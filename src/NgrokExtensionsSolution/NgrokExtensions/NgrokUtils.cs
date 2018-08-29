// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Copyright (c) 2016 David Prothero

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NgrokExtensions
{
    public class NgrokUtils
    {
        public const string NgrokNotFoundMessage = "ngrok executable not found. Configure the path in the via the add-in options or add the location to your PATH.";
        private readonly HttpClient _ngrokApi;
        private Tunnel[] _tunnels;
        private readonly NgrokProcess _ngrokProcess;
		private readonly WebAppConfig _webAppConfig;
		private readonly ILogger _logger;

        public NgrokUtils(
			WebAppConfig webAppConfig, 
			string exePath,
			ILogger<NgrokUtils> logger,
			HttpClient client = null, NgrokProcess ngrokProcess = null)
        {
            _ngrokProcess = ngrokProcess ?? new NgrokProcess(exePath);
            _ngrokApi = client ?? new HttpClient();
            _ngrokApi.BaseAddress = new Uri("http://localhost:4040");
            _ngrokApi.DefaultRequestHeaders.Accept.Clear();
            _ngrokApi.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_webAppConfig = webAppConfig;
			_logger = logger;

		}

        public bool NgrokIsInstalled()
        {
            return _ngrokProcess.IsInstalled();
        }

        public async Task<IEnumerable<Tunnel>> StartTunnelsAsync()
        {
            Exception uncaughtException = null;

            try
            {
                return await DoStartTunnelsAsync();
            }
            catch (FileNotFoundException)
            {
				_logger.Log(LogLevel.Error, NgrokNotFoundMessage);
            }
            catch (Win32Exception ex)
            {
                if (ex.ErrorCode.ToString("X") == "80004005")
                {
					_logger.Log(LogLevel.Error, NgrokNotFoundMessage);
                }
                else
                {
                    uncaughtException = ex;
                }
            }
            catch (Exception ex)
            {
                uncaughtException = ex;
            }

            if (uncaughtException != null)
            {
                _logger.Log(LogLevel.Critical, "Ran into a problem trying to start the ngrok tunnel(s): {uncaughtException}", uncaughtException);
            }
			return null;
        }

        private async Task<IEnumerable<Tunnel>> DoStartTunnelsAsync()
        {
            await StartNgrokAsync();
			return await StartNgrokTunnelAsync("project name");
		}

        private async Task StartNgrokAsync(bool retry = false)
        {
            if (await CanGetTunnelList()) return;

            _ngrokProcess.StartNgrokProcess();
            await Task.Delay(250);

            if (await CanGetTunnelList(retry:true)) return;
             _logger.Log(LogLevel.Critical, "Cannot start ngrok. Is it installed and in your PATH?");
        }

		public Task StopNgrok()
		{
			_ngrokProcess.StopNgrokProcess();
			return Task.CompletedTask;
		}

        private async Task<bool> CanGetTunnelList(bool retry = false)
        {
            try
            {
                await GetTunnelList();
            }
            catch(Exception ex)
            {
                if (retry) throw;
            }
            return (_tunnels != null);
        }

        private async Task GetTunnelList()
        {
            var response = await _ngrokApi.GetAsync("/api/tunnels");
            if (response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"responseText: '{responseText}'");
				NgrokTunnelsApiResponse apiResponse = null;
				try
				{
					apiResponse = JsonConvert.DeserializeObject<NgrokTunnelsApiResponse>(responseText);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					throw;
				}
                
                _tunnels = apiResponse.tunnels;
            }
        }

        private async Task<IEnumerable<Tunnel>> StartNgrokTunnelAsync(string projectName)
        {
            var addr = $"localhost:{_webAppConfig.PortNumber}";

			var existingTunnels = _tunnels.Where(t => t.config.addr == addr);

			if (existingTunnels.Any())
			{
				_logger.Log(LogLevel.Information, "Existing tunnels: {@tunnels}", existingTunnels);
				return existingTunnels;
			} else
			{
				var tunnel = await CreateTunnelAsync(projectName, addr);
				return IEnumerableExt.SingleItemAsEnumerable(tunnel);
			}
        }

        private async Task<Tunnel> CreateTunnelAsync(string projectName, string addr, bool retry = false)
        {
            var request = new NgrokTunnelApiRequest
            {
                name = projectName,
                addr = addr,
                proto = "http",
                host_header = addr
            };
            if (!string.IsNullOrEmpty(_webAppConfig.SubDomain))
            {
                request.subdomain = _webAppConfig.SubDomain;
            }

            Debug.WriteLine($"request: '{JsonConvert.SerializeObject(request)}'");
			var json = JsonConvert.SerializeObject(request);
			var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await _ngrokApi.PostAsync("/api/tunnels", httpContent);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"{response.StatusCode} errorText: '{errorText}'");
                NgrokErrorApiResult error;

                try
                {
                    error = JsonConvert.DeserializeObject<NgrokErrorApiResult>(errorText);
                }
                catch(JsonReaderException)
                {
                    error = null;
                }

                if (error != null)
                {
                    _logger.Log(LogLevel.Error, $"Could not create tunnel for {projectName} ({addr}): " +
                                         $"\n[{error.error_code}] {error.msg}" +
                                         $"\nDetails: {error.details.err.Replace("\\n", "\n")}");
                }
                else
                {
                    if (retry)
                    {
                        _logger.Log(LogLevel.Error, $"Could not create tunnel for {projectName} ({addr}): " +
                                             $"\n{errorText}");
                    }
                    else
                    {
                        await Task.Delay(1000);  // wait for ngrok to spin up completely?
                        await CreateTunnelAsync(projectName, addr, true);
                    }
                }
                return null;
            }

            var responseText = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"responseText: '{responseText}'");
            return JsonConvert.DeserializeObject<Tunnel>(responseText);
        }
    }
	public static class IEnumerableExt
	{
		// usage: someObject.SingleItemAsEnumerable();
		public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
		{
			yield return item;
		}
	}
}