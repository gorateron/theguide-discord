﻿using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TheGuide
{
    public class CommandHandler
    {
        public const int cooldownDelay = 2500;
        public const int deleteDelay = 5000;
        public const char prefixChar = '?';
        private CommandService service;
        private DiscordSocketClient client;
        private IDependencyMap map;
        private Dictionary<ulong, DateTime> cooldowns;

        public async Task Install(IDependencyMap _map)
        {
            service = new CommandService();

            client = _map.Get<DiscordSocketClient>();
            cooldowns = _map.Get<Dictionary<ulong, DateTime>>();
            _map.Add(service);

            map = _map;

            await service.AddModules(Assembly.GetEntryAssembly()).ConfigureAwait(false);

            client.MessageReceived += HandleCommand;
        }

        public async Task HandleCommand(SocketMessage parameterMessage)
        {

            var message = parameterMessage as SocketUserMessage;
            var context = new CommandContext(client, message);

            if (Program.devMode && context.Guild.Id != 216276491544166401)
                return;

            int argPos = 0;

            var cleanmsg = new string(message.Content.Where(x => !char.IsWhiteSpace(x)).ToArray());
            if (message == null || cleanmsg.Length <= 1 || (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix(prefixChar, ref argPos))))
                return;

            var cooldownTime = cooldowns.FirstOrDefault(x => x.Key == message.Author.Id);
            if (cooldownTime.Key != default(ulong))
            {
                if (cooldownTime.Value > DateTime.Now)
                {
                    await message?.DeleteAsync();
                    return;
                }
                else
                    cooldowns.Remove(cooldownTime.Key);
            }

            var result = await service.Execute(context, argPos, map);

            if (!result.IsSuccess)
            {
                //var channel = await context.User?.CreateDMChannelAsync();
                await context.Channel.SendMessageAsync($"**Error** (on command <{message.Content}>): {result.ToString()}");
            }
            else
            {
                cooldowns.Add(message.Author.Id, DateTime.Now.AddMilliseconds(cooldownDelay));
                string[] opt = SplitOpt(message.ToString());
                if (opt.Any(x => x[0] == 'd'))
                {
                    await message?.DeleteAsync();
                }
            }
        }

        public static string[] SplitOpt(string opt)
        {
            string[] opts = opt.Split('-');
            opts = opts.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            return opts;
        }
    }
}
