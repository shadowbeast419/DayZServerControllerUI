using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DayZServerControllerUI.CtrlLogic
{
    public class DiscordBot
    {
        private DiscordSocketClient? _client;
        private DiscordBotData? _botData;
        private bool _isInit;

        public bool Mute { get; set; } = false;

        public DiscordBot(DiscordBotData? botData)
        {
            if (botData is not { IsDataValid: true })
                return;

            _botData = botData;
            _client = new DiscordSocketClient();
            _isInit = true;
        }

        public async Task Init()
        {
            if (!_isInit)
                return;

            await _client.LoginAsync(TokenType.Bot, _botData.Token);
            await _client.StartAsync();
        }

        public async Task Announce(string message)
        {
            if (!_isInit || Mute)
                return;

            var channel = await _client.GetChannelAsync(_botData.ChannelId) as IMessageChannel;
            await channel.SendMessageAsync(message);
        }

        ~DiscordBot()
        {
            if (!_isInit)
                return;

            _client?.Dispose();
        }
    }
}
