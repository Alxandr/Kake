using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;

namespace Kake
{
    public class Program
    {
        static string test = @"
@default default
@using System

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

        IApplicationEnvironment _applicationEnvironment;
        ILibraryExportProvider _libraryExportProvider;
        IAssemblyLoaderEngine _assemblyLoaderEngine;

        public Program(
            IApplicationEnvironment applicationEnvironment,
            ILibraryExportProvider libraryExportProvider,
            IAssemblyLoaderEngine assemblyLoaderEngine)
        {
            _applicationEnvironment = applicationEnvironment;
            _libraryExportProvider = libraryExportProvider;
            _assemblyLoaderEngine = assemblyLoaderEngine;
        }

        public async Task Main(string[] args)
        {
            try
            {
                var unit = await Parser.Parse(new StringReader(test));
                var tree = Generator.Generate("build.kake", unit, _applicationEnvironment, _libraryExportProvider, _assemblyLoaderEngine);
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
        }
    }
}
