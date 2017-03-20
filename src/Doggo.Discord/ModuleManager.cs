using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Doggo.Discord
{
    public class ModuleManager
    {
        public IReadOnlyDictionary<string, Assembly> Assemblies => _assemblies;
        public IReadOnlyDictionary<Type, object> Modules => _modules;

        private DiscordSocketClient _client;
        private ConcurrentDictionary<string, Assembly> _assemblies = new ConcurrentDictionary<string, Assembly>();
        private ConcurrentDictionary<Type, object> _modules = new ConcurrentDictionary<Type, object>();
        private string _modulePath = Configuration.Load().ModulePath;

        public ModuleManager(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task LoadModulesAsync()
        {
            var files = Directory.EnumerateFiles(_modulePath);

            foreach (var file in files)
                await LoadModuleAsync(file).ConfigureAwait(false);
        }

        public Task LoadModuleAsync(string path)
        {
            var assembly = GetAssembly(path);
            var module = GetModule(assembly);
            Initialize(module);
            return Task.CompletedTask;
        }
        
        private Assembly GetAssembly(string file)
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
            _assemblies.TryAdd(assembly.FullName, assembly);
            return assembly;
        }

        private object GetModule(Assembly assembly)
        {
            var types = assembly.GetTypes();
            var initializer = types.First();
            if (initializer == null)
                throw new InvalidOperationException($"Assembly {assembly.FullName} is not a valid Doggo module.");
            
            var module = Activator.CreateInstance(initializer, _client);
            _modules.TryAdd(module.GetType(), module);
            return module;
        }

        private void Initialize(object module)
        {
            var type = module.GetType().GetTypeInfo();
            var method = type.GetMethods().FirstOrDefault(x => x.GetCustomAttribute(typeof(InitializerAttribute)) != null);

            if (method == null)
                throw new InvalidOperationException($"Module {type.FullName} does not contain a valid initializer method.");

            var task = (Task)method.Invoke(module, null);
            task.GetAwaiter().GetResult();
        }
    }
}
