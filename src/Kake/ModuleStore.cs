using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Common.DependencyInjection;

namespace Kake
{
    internal class ModuleStore : IModuleStore
    {
        // TODO: Use ConcurrentDictionary

        readonly object _lock = new object();
        readonly Dictionary<string, Module> _cache = new Dictionary<string, Module>();

        internal async Task<Module> Build(
            string filePath,
            string content,
            IServiceProvider services)
        {
            var parsed = await Parser.Parse(new StringReader(content));
            var name = Path.GetFileName(filePath);

            lock (_lock)
            {
                var assembly = Generator.Generate(
                    filePath,
                    parsed,
                    services.Get<IApplicationEnvironment>(),
                    services.Get<ILibraryManager>(),
                    services.Get<IAssemblyLoaderEngine>());

                var type = assembly.ExportedTypes.First();
                var module = (Module)ActivatorUtilities.CreateInstance(services, type);
                _cache.Add(name, module);
                return module;
            }
        }
    }
}