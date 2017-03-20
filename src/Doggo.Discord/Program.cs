using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Doggo.Discord
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private ModuleManager _manager;

        public async Task StartAsync()
        {
            PrettyConsole.NewLine($"===   Doggo {ConfigurationBase.Version}   ===");
            PrettyConsole.NewLine();

            Configuration.EnsureExists();

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 1000
            });

            _client.Log += OnLogAsync;

            //await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token.Discord);
            //await _client.StartAsync();

            _manager = new ModuleManager(_client);
            await _manager.LoadModulesAsync();

            await Task.Delay(-1);
        }

        private Task OnLogAsync(LogMessage msg)
            => PrettyConsole.LogAsync(msg.Severity, msg.Source, msg.Message);
    }
}