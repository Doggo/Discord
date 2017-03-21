using Discord.WebSocket;
using Doggo.Commands;
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
            var files = Directory.EnumerateFiles(_modulePath).Where(x => Path.GetExtension(x) == "dll");

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
            var initializer = assembly.GetTypes().FirstOrDefault(x => x.GetTypeInfo().GetCustomAttribute(typeof(InitializerAttribute)) != null);

            if (initializer == null)
                throw new InvalidOperationException($"Assembly {assembly.FullName} does not contain a valid module initializer.");
            
            var module = Activator.CreateInstance(initializer, _client);
            _modules.TryAdd(module.GetType(), module);
            return module;
        }

        private void Initialize(object module)
        {
            var type = module.GetType().GetTypeInfo();

            var instance = module as IModule;
            if (instance == null)
                throw new InvalidOperationException($"Module {type.FullName} does not implement the IModule interface.");

            var task = instance.InitalizeAsync(new DependencyMap());
            task.GetAwaiter().GetResult();
        }
    }
}
