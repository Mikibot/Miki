﻿using Discord;
using EFCache;
using EFCache.RedisCache;
using IA;
using IA.FileHandling;
using IA.SDK;
using Miki.Languages;
using Miki.Models;
using Miki.Modules.Gambling.Managers;
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
        private string devId;

        public async Task Start()
        {
            Locale.Load();
            timeSinceStartup = DateTime.Now;

            LoadApiKeyFromFile();

            LoadDiscord();

            await bot.ConnectAsync();
        }

        private void LoadApiKeyFromFile()
        {
            if (FileReader.FileExist("settings", "miki"))
            {
                FileReader reader = new FileReader("settings", "miki");
                Global.ApiKey = reader.ReadLine();
                devId = reader.ReadLine();
                Global.shardCount = int.Parse(reader.ReadLine());
                Global.CarbonitexKey = reader.ReadLine();
                Global.UrbanKey = reader.ReadLine();
                Global.ImgurKey = reader.ReadLine();
                Global.ImgurClientId = reader.ReadLine();
                Global.DiscordPwKey = reader.ReadLine();
                Global.DiscordBotsOrgKey = reader.ReadLine();
                Global.SharpRavenKey = reader.ReadLine();
				Global.DatadogKey = reader.ReadLine();
                reader.Finish();
            }
            else
            {
                FileWriter writer = new FileWriter("settings", "miki");
                writer.Write("", "Token");
                writer.Write("", "Developer Id");
                writer.Write("", "Shard Count");
                writer.Write("", "Carbon API Key");
                writer.Write("", "Urban API Key (Mashape)");
                writer.Write("", "Imgur API Key (Mashape)");
                writer.Write("", "Imgur Client ID (without Client-ID)");
                writer.Write("", "Discord.pw API Key");
                writer.Write("", "Discordbot.org API Key");
                writer.Write("", "RavenSharp Key");
				writer.Write("", "Datadog Key");
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
                x.Version = "0.4.5-fix";
                x.Token = Global.ApiKey;
                x.ShardCount = Global.shardCount;
                x.ConsoleLogLevel = LogLevel.ALL;
            });

            if (!string.IsNullOrWhiteSpace(Global.SharpRavenKey))
            {
                Global.ravenClient = new SharpRaven.RavenClient(Global.SharpRavenKey);
            }

			if(!string.IsNullOrWhiteSpace(Global.DatadogKey))
			{
				var dogstatsdConfig = new StatsdConfig
				{
					StatsdServerName = "37.187.131.140",
					StatsdPort = 8125,
					Prefix = "miki"
				};
				DogStatsd.Configure(dogstatsdConfig);
			}

			bot.Events.AddCommandDoneEvent(x =>
			{
				DogStatsd.Counter("commands.count", 1);
			});

            bot.Events.OnCommandError = async (ex, cmd, msg) =>
            {
				DogStatsd.Counter("commands.error", 1);
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
        }

		private async Task Client_LeftGuild(Discord.WebSocket.SocketGuild arg)
		{
			DogStatsd.Counter("guilds.left", 1);
		}

		private async Task Client_JoinedGuild(IGuild arg)
        {
            ITextChannel defaultChannel = await arg.GetDefaultChannelAsync();
            await defaultChannel.SendMessage("Hello, I am **Miki**! At your service!\nTry to use **>help** to check out what i can do! :notes:");

			// if miki patreon is present, leave again.

			DogStatsd.Increment("guilds.joined", 1);
        }	
    }
}