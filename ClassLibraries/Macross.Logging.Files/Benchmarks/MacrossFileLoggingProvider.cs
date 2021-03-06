﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LoggingBenchmarks
{
	public static class MacrossFileLoggingProvider
	{
		public static (IHost Host, ILoggerProvider LoggerProvider) CreateMacrossProvider()
		{
			IHost host = Host
				.CreateDefaultBuilder()
				.ConfigureLogging(builder =>
				{
					builder.ClearProviders();
					builder.AddFiles(files =>
					{
						files.LogFileDirectory = "C:\\LogsPerf\\Macross";
						files.LogFileArchiveDirectory = "C:\\LogsPerf\\Macross\\Archive";
						files.LogFileMaxSizeInKilobytes = 0;
					});
				}).Build();

			return (host, host.Services.GetRequiredService<ILoggerProvider>());
		}
	}
}
