using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DayZServerControllerUI.CtrlLogic
{
    public class Logging
    {
        private DiscordBot? _discordBot;
        private TextBox _textBox;

        public bool MuteDiscordBot { get; set; } = false;
        public bool MuteTextBox { get; set; } = false;


        public Logging(TextBox textBox)
        {
            _textBox = textBox;
            _discordBot = null;
        }

        public void AttachDiscordBot(DiscordBot discordBot)
        {
            _discordBot = discordBot;

            MuteDiscordBot = _discordBot == null;
        }

        public async Task WriteLineAsync(string message, bool writeToDiscord = true)
        {
            if (!MuteTextBox)
            {
                _textBox.Dispatcher.Invoke(DispatcherPriority.Normal, 
                    new Action(() => { _textBox.AppendText(message + Environment.NewLine); }));
            }

            if (!MuteDiscordBot && writeToDiscord)
                await _discordBot.Announce(message);
        }

    }
}
