﻿using Discord;
using EFCache;
using EFCache.RedisCache;
using IA;
using IA.FileHandling;
using IA.SDK;
using Miki.Languages;
using Miki.Models;
using Miki.Modules.Gambling.Managers;
using Miki.Tests;
using Newtonsoft.Json;
using Nito.AsyncEx;
using StackExchange.Redis;
using StatsdClient;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Miki
{
    public class Program
    {
        private static void Main(string[] args)
        {
			AsyncContext.Run(() => new Program().Start());
        }

        public static Bot bot;
        public static DateTime timeSinceStartup;

		public async Task Start()
        {
            Locale.Load();
            timeSinceStartup = DateTime.Now;

			LoadConfig();
            LoadDiscord();

			// Run this only when in debug mode.
			if (Debugger.IsAttached)
			{
				TestCase.Run();
			}

            await bot.ConnectAsync();
        }

        private void LoadConfig()
        {
            if (FileReader.FileExist("settings.json", "miki"))
            {
                FileReader reader = new FileReader("settings.json", "miki");
				Global.config = JsonConvert.DeserializeObject<Config>(reader.ReadAll());			
                reader.Finish();
            }
            else
            {
                FileWriter writer = new FileWriter("settings.json", "miki");
                writer.Write(JsonConvert.SerializeObject(Global.config, Formatting.Indented));
                writer.Finish();
            }
        }

        /// <summary>
        /// The program runs all discord services and loads all the data here.
        /// </summary>
        public void LoadDiscord()
        {
            bot = new Bot(x =>
            {
                x.Name = "Miki";
                x.Version = "0.4.6";
                x.Token = Global.config.Token;
                x.ShardCount = Global.shardCount;
                x.ConsoleLogLevel = LogLevel.ALL;
            });

            if (!string.IsNullOrWhiteSpace(Global.config.SharpRavenKey))
            {
                Global.ravenClient = new SharpRaven.RavenClient(Global.config.SharpRavenKey);
            }

			if(!string.IsNullOrWhiteSpace(Global.DatadogKey))
			{
				var dogstatsdConfig = new StatsdConfig
				{
					StatsdServerName = Global.DatadogHost,
					StatsdPort = 8125,
					Prefix = "miki"
				};
				DogStatsd.Configure(dogstatsdConfig);
			}

			bot.Events.AddCommandDoneEvent(x =>
			{
				x.Name = "datadog-command-done";
				x.processEvent = async (msg, cmd, success, t) =>
				{
					if (!success)
					{
						DogStatsd.Counter("commands.error.rate", 1);
					}
					DogStatsd.Counter("commands.count", 1);
					DogStatsd.Histogram("commands.time", t, 0.1);
				};
			});

			bot.MessageReceived += Bot_MessageReceived;

            bot.Events.OnCommandError = async (ex, cmd, msg) =>
            {
				/*RuntimeEmbed e = new RuntimeEmbed();
                //e.Title = Locale.GetEntity(0).GetString(Locale.ErrorMessageGeneric);
                //e.Color = new IA.SDK.Color(1, 0.4f, 0.6f);

                //if (Notification.CanSendNotification(msg.Author.Id, DatabaseEntityType.USER, DatabaseSettingId.ERRORMESSAGE))
                //{
                //    e.Description = "Miki has encountered a problem in her code with your request. We will send you a log and instructions through PM.";

                //    await e.SendToChannel(msg.Channel);

                //    e.Title = $"You used the '{cmd.Name}' and it crashed!";
                //    e.Description = "Please screenshot this message and send it to the miki issue page (https://github.com/velddev/miki/issues)";
                //    e.AddField(f =>
                //    {
                //        f.Name = "Error Message";
                //        f.Value = ex.Message;
                //        f.IsInline = true;
                //    });

                //    e.AddField(f =>
                //    {
                //        f.Name = "Error Log"; 
                //        f.Value = "```" + ex.StackTrace + "```";
                //        f.IsInline = true;
                //    });

                //    e.CreateFooter();
                //    e.Footer.Text = "Did you not want this message? use `>toggleerrors` to disable it!";

                //    await msg.Author.SendMessage(e);
                //    return;
                //}
                //e.Description = "... but you've disabled error messages, so we won't send you a PM :)";
                //await e.SendToChannel(msg.Channel);
                */
			};
            bot.OnError = async (ex) => Log.Message(ex.ToString());
            bot.AddDeveloper(121919449996460033);

            if (!string.IsNullOrEmpty(devId))
            {
                bot.AddDeveloper(ulong.Parse(devId));
            }

            bot.Client.JoinedGuild += Client_JoinedGuild;
			bot.Client.LeftGuild += Client_LeftGuild;

			bot.OnShardConnect += Bot_OnShardConnect;
			bot.OnShardDisconnect += Bot_OnShardDisconnect;
        }

		private async Task Bot_MessageReceived(IA.SDK.Interfaces.IDiscordMessage arg)
		{
			DogStatsd.Counter("messages.received", 1);
		}

		private async Task Client_LeftGuild(Discord.WebSocket.SocketGuild arg)
		{
			DogStatsd.Counter("guilds.left", 1);
			DogStatsd.Set("guilds", Bot.instance.Client.Guilds.Count);
		}

		private async Task Client_JoinedGuild(IGuild arg)
        {
            ITextChannel defaultChannel = await arg.GetDefaultChannelAsync();
            await defaultChannel.SendMessage("Hello, I am **Miki**! At your service!\nTry to use **>help** to check out what i can do! :notes:");

			// if miki patreon is present, leave again.

			DogStatsd.Increment("guilds.joined", 1);
			DogStatsd.Set("guilds", Bot.instance.Client.Guilds.Count);
		}

		private async Task Bot_OnShardConnect(int shardId)
		{
			DogStatsd.Event("shard.connect", $"shard {shardId.ToString()} has connected!");
			DogStatsd.ServiceCheck($"shard.{shardId.ToString()}", Status.OK);
		}

		private async Task Bot_OnShardDisconnect(Exception e, int shardId)
		{
			DogStatsd.Event("shard.disconnect", $"shard {shardId.ToString()} has disconnected!");
			DogStatsd.ServiceCheck($"shard.{shardId.ToString()}", Status.CRITICAL);
		}

	}
}