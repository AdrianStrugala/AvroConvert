using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Http.Server
{
    public class Program
    {
        private const string DefaultUrl = "https://localhost:2137";

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>

        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseUrls(DefaultUrl);
    }
}
