using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DayZServerControllerUI.CtrlLogic
{
    public class DiscordBot
    {
        private readonly DiscordSocketClient? _client;
        private readonly DiscordBotData? _botData;
        private bool _isInitialized;

        public bool Mute { get; set; } = false;

        public DiscordBot(DiscordBotData? botData)
        {
            if (botData == null || !botData.IsDataValid)
                return;

            _botData = botData;
            _client = new DiscordSocketClient();
        }

        public async Task Init()
        {
            if (_client == null || _botData == null)
                return;

            await _client.LoginAsync(TokenType.Bot, _botData.Token);
            await _client.StartAsync();

            _isInitialized = true;
        }

        public async Task Announce(string message)
        {
            if (!_isInitialized || Mute || _client == null || _botData == null)
                return;

            var channel = await _client.GetChannelAsync(_botData.ChannelId) as IMessageChannel;

            if (channel == null)
                throw new IOException($"DiscordBot: Could not retrieve Discord-Channel with ID {_botData.ChannelId}!");

            await channel.SendMessageAsync(message);
        }

        ~DiscordBot()
        {
            if (!_isInitialized)
                return;

            _client?.Dispose();
        }
    }
}
