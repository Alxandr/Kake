using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Common.CommandLine;

namespace Kake
{
    public class Program
    {
        static string test = @"
@default default
@using System
@using System.Text

string message;

default:
  @dependsOn world
  // this is the default task btw xD
  
  Console.WriteLine(message);

world: 
  @dependsOn hello
  
  message += "" world!"";

hello:
  message = ""Hello"";
";

        private readonly IServiceProvider _hostServices;
        private readonly IApplicationEnvironment _environment;
        private readonly object _lock = new object();
        private readonly ModuleStore _moduleStore;
        private ConsoleColor _originalForeground;

        public Program(IServiceProvider hostServices, IApplicationEnvironment environment)
        {
            _hostServices = hostServices;
            _environment = environment;
            _moduleStore = new ModuleStore();
        }

        public async Task<int> Main(string[] args)
        {
            _originalForeground = Console.ForegroundColor;

            var app = new CommandLineApplication();
            app.Name = "kake";

            var optionVerbose = app.Option("-v|--verbose", "Show verbose output", CommandOptionType.NoValue);
            app.HelpOption("-?|-h|--help");
            app.VersionOption("--version", GetVersion());

            app.OnExecute(async () =>
            {
                var currentDir = _environment.ApplicationBasePath;
                var filePath = Path.Combine(currentDir, "build.kake");
                var content = test;

                var module = await _moduleStore.Build(filePath, content, _hostServices);
                return 0;
            });

            int result = await app.Execute(args);
            Console.ReadLine();
            return result;
        }

        private static string GetVersion()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            var assemblyInformationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return assemblyInformationalVersionAttribute.InformationalVersion;
        }
    }
}
