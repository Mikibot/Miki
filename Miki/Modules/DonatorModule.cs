﻿using Miki.Accounts.Achievements;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Logging;
using Miki.Models;
using Miki.Rest;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module(Name = "Donator")]
	internal class DonatorModule
	{
        private RestClient client;

		public DonatorModule(Module m, MikiApplication b)
		{
            if(!string.IsNullOrWhiteSpace(Global.Config.ImageApiUrl) 
                || !string.IsNullOrWhiteSpace(Global.Config.MikiApiKey))
            {
                client = new RestClient(Global.Config.ImageApiUrl)
                    .AddHeader("Authorization", Global.Config.MikiApiKey);
            }
            else
            {
                m.Enabled = false;
                Log.Warning("Disabled Donator module due to missing configuration parameters for MikiAPI.");
            }
		}

		[Command(Name = "changetitle")]
		public async Task ChangeTitleAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				IsDonator donator = await context.IsDonator.FindAsync((long)e.Author.Id);
				User user = await context.Users.FindAsync((long)e.Author.Id);

				donator.AddBalance(-10);
				user.Title = e.Arguments.ToString();

				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "redeemkey")]
		public async Task RedeemKeyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				long id = (long)e.Author.Id;
				Guid guid = Guid.Parse(e.Arguments.Join().Argument);
				DonatorKey key = await context.DonatorKey.FindAsync(guid);
				IsDonator donatorStatus = await context.IsDonator.FindAsync(id);

				if (key != null)
				{
					if (donatorStatus == null)
					{
						donatorStatus = (await context.IsDonator.AddAsync(new IsDonator()
						{
							UserId = id
						})).Entity;
					}

					donatorStatus.KeysRedeemed++;

					if (donatorStatus.ValidUntil > DateTime.Now)
					{
						donatorStatus.ValidUntil += key.StatusTime;
					}
					else
					{
						donatorStatus.ValidUntil = DateTime.Now + key.StatusTime;
					}

					new EmbedBuilder()
					{
						Title = ($"🎉 Congratulations, {e.Author.Username}"),
						Color = new Color(226, 46, 68),
						Description = ($"You have successfully redeemed a donator key, I've given you **{key.StatusTime.TotalDays}** days of donator status."),
						ThumbnailUrl = ("https://i.imgur.com/OwwA5fV.png")
					}.AddInlineField("When does my status expire?", donatorStatus.ValidUntil.ToLongDateString())
					.ToEmbed().QueueToChannel(e.Channel);

					context.DonatorKey.Remove(key);
					await context.SaveChangesAsync();

					// cheap hack.

					var achievements = AchievementManager.Instance.GetContainerById("donator");

					if (donatorStatus.KeysRedeemed == 1)
					{
						await achievements.Achievements[0].UnlockAsync(e.Channel, e.Author, 0);
					}
					else if (donatorStatus.KeysRedeemed == 5)
					{
						await achievements.Achievements[1].UnlockAsync(e.Channel, e.Author, 1);
					}
					else if (donatorStatus.KeysRedeemed == 25)
					{
						await achievements.Achievements[2].UnlockAsync(e.Channel, e.Author, 2);
					}
				}
				else
				{
					e.ErrorEmbed("Your donation key is invalid!")
						.ToEmbed().QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "box")]
		public async Task BoxAsync(EventContext e)
			=> await PerformCall(e, $"/api/box?text={e.Arguments.Join().RemoveMentions(e.Guild)}&url={(await GetUrlFromMessageAsync(e))}");

		[Command(Name = "disability")]
		public async Task DisabilityAsync(EventContext e)
			=> await PerformCall(e, "/api/disability?url=" + (await GetUrlFromMessageAsync(e)));

		[Command(Name = "tohru")]
		public async Task TohruAsync(EventContext e)
			=> await PerformCall(e, "/api/tohru?text=" + e.Arguments.Join().RemoveMentions(e.Guild));

		[Command(Name = "truth")]
		public async Task TruthAsync(EventContext e)
			=> await PerformCall(e, "/api/yagami?text=" + e.Arguments.Join().RemoveMentions(e.Guild));

		[Command(Name = "trapcard")]
		public async Task YugiAsync(EventContext e)
			=> await PerformCall(e, $"/api/yugioh?url={(await GetUrlFromMessageAsync(e))}");

		private async Task<string> GetUrlFromMessageAsync(EventContext e)
		{
			string url = e.Author.GetAvatarUrl();

			if (e.message.MentionedUserIds.Count > 0)
			{
				url = (await e.Guild.GetMemberAsync(e.message.MentionedUserIds.First())).GetAvatarUrl();
			}

			//if (e.message.Attachments.Count > 0)
			//{
			//	url = e.message.Attachments.First().Url;
			//}

			return url;
		}

		private async Task PerformCall(EventContext e, string url)
		{
			using (var context = new MikiContext())
			{
				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				if (u == null)
				{
					SendNotADonatorError(e.Channel);
					return;
				}

				if (await u.IsDonatorAsync(context))
				{
					Stream s = await client.GetStreamAsync(url);
					await (e.Channel as IDiscordTextChannel).SendFileAsync(s, "meme.png");
				}
				else
				{
					SendNotADonatorError(e.Channel);
				}
			}
		}

		private void SendNotADonatorError(IDiscordChannel channel)
		{
			new EmbedBuilder()
			{
				Title = "Sorry!",
				Description = "... but you haven't donated yet, please support us with a small donation to unlock these commands!",
			}.AddField("Already donated?", "Make sure to join the Miki Support server and claim your donator status!")
			 .AddField("Where do I donate?", "You can find our patreon at https://patreon.com/mikibot")
			 .ToEmbed().QueueToChannel(channel);
		}
	}
}