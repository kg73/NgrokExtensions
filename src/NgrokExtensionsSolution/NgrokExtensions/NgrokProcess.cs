using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NgrokExtensions
{
    public class NgrokProcess
    {
        private readonly string _exePath;
		private Process _process;

        public NgrokProcess(string exePath)
        {
            _exePath = exePath;
        }

        public void StartNgrokProcess()
        {
            var path = GetNgrokPath();

            var pi = new ProcessStartInfo(path, "start --none")
            {
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal    
            };

			pi.UseShellExecute = true;

            Start(pi);
        }

		public void StopNgrokProcess()
		{
			Stop();
		}

        private string GetNgrokPath()
        {
            var path = "ngrok.exe";

            if (!string.IsNullOrWhiteSpace(_exePath) && File.Exists(_exePath))
            {
                path = _exePath;
            }

            return path;
        }

        protected virtual void Start(ProcessStartInfo pi)
        {
            var startedProcess = Process.Start(pi);
			_process = startedProcess;
        }

		protected virtual void Stop()
		{
			if (_process != null && !_process.HasExited)
			{
				_process.Kill();
				foreach (var p in Process.GetProcessesByName("ngrok"))
				{
					p.Kill();
				}
			}
		}

        public bool IsInstalled()
        {
            var fileName = GetNgrokPath();

            if (File.Exists(fileName))
                return true;

            var values = Environment.GetEnvironmentVariable("PATH") ?? "";
            return values.Split(Path.PathSeparator)
                .Select(path => Path.Combine(path, fileName))
                .Any(File.Exists);
        }
    }
}