	#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Discord;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Builders;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.API.Leaderboards;
using Miki.Languages;
using Miki.Models;
using Miki.Modules.Accounts.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.AccountsModule
{
	[Module("Accounts")]
	public class AccountsModule
	{
		// TODO: install services automatically.
		public AccountsModule(RuntimeModule module)
		{
			new AchievementsService()
				.Install(module);

			new ExperienceTrackerService()
				.Install(module);
		}

		[Command(Name = "achievements")]
		public async Task AchievementsAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				long id = (long)e.Author.Id;

				ArgObject arg = e.Arguments.FirstOrDefault();

				if (arg != null)
				{
					IDiscordUser user = await arg.TakeUntilEnd()
						.GetUserAsync(e.Guild);

					if (user != null)
					{
						id = (long)user.Id;
					}
				}

				IDiscordUser discordUser = await e.Guild.GetUserAsync(id.FromDbLong());
				User u = await User.GetAsync(context, discordUser);

				List<Achievement> achievements = await context.Achievements
					.Where(x => x.Id == id)
					.ToListAsync();

				IDiscordEmbed embed = Utils.Embed
					.SetColor(255, 255, 255)
					.SetAuthor($"{u.Name} | " + "Achievements", discordUser.AvatarUrl, "https://miki.ai/profiles/ID/achievements");

				StringBuilder leftBuilder = new StringBuilder();

				int totalScore = 0;

				foreach (var a in achievements)
				{
					BaseAchievement metadata = AchievementManager.Instance.GetContainerById(a.Name).Achievements[a.Rank];
					leftBuilder.AppendLine(metadata.Icon + " | `" + metadata.Name.PadRight(15) + $"{metadata.Points.ToString().PadLeft(3)} pts` | 📅 {a.UnlockedAt.ToShortDateString()}");
					totalScore += metadata.Points;
				}

				if (string.IsNullOrEmpty(leftBuilder.ToString()))
				{
					embed.AddInlineField("Total Pts: " + totalScore, "None, yet.");
				}
				else
				{
					embed.AddInlineField("Total Pts: " + totalScore, leftBuilder.ToString());
				}

				embed.QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "leaderboards", Aliases = new[] { "lb", "leaderboard", "top" })]
		public async Task LeaderboardsAsync(EventContext e)
		{
			LeaderboardsOptions options = new LeaderboardsOptions();

			ArgObject argument = e.Arguments.FirstOrDefault();

			switch (argument?.Argument.ToLower() ?? "")
			{
				case "commands":
				case "cmds":
				{
					options.type = LeaderboardsType.COMMANDS;
					argument = argument?.Next();
				}
				break;

				case "currency":
				case "mekos":
				case "money":
				case "bal":
				{
					options.type = LeaderboardsType.CURRENCY;
					argument = argument?.Next();
				}
				break;

				case "rep":
				case "reputation":
				{
					options.type = LeaderboardsType.REPUTATION;
					argument = argument?.Next();
				}
				break;

				case "pasta":
				case "pastas":
				{
					options.type = LeaderboardsType.PASTA;
					argument = argument?.Next();
				}
				break;

				case "experience":
				case "exp":
				{
					options.type = LeaderboardsType.EXPERIENCE;
					argument = argument?.Next();
				}
				break;

				default:
				{
					options.type = LeaderboardsType.EXPERIENCE;
				}
				break;
			}

			if (argument?.Argument.ToLower() == "local")
			{
				if (options.type != LeaderboardsType.PASTA)
				{
					options.guildId = e.Guild.Id;
				}
				argument = argument.Next();
			}

			// Null-conditional operators do not apply on async methods.
			if (argument != null)
			{
				IDiscordUser user = (await argument.GetUserAsync(e.Guild));
				if (user != null)
				{
					options.mentionedUserId = user.Id;
					argument = argument.Next();
				}
			}

			if ((argument?.AsInt(0) ?? 0) != 0)
			{
				options.pageNumber = argument.AsInt();
				argument = argument?.Next();
			}

			await ShowLeaderboardsAsync(e.message, options);
		}

		[Command(Name = "profile")]
		public async Task ProfileAsync(EventContext e)
		{
			Stopwatch sw = new Stopwatch();

			sw.Start();

			using (var context = new MikiContext())
			{
				long id = 0;
				ulong uid = 0;

				var arg = e.Arguments.FirstOrDefault();

				if (arg != null)
				{
					uid = (await arg.GetUserAsync(e.Guild)).Id;
					id = uid.ToDbLong();
				}
				else
				{
					uid = e.message.Author.Id;
					id = uid.ToDbLong();
				}

				Locale locale = new Locale(e.Channel.Id.ToDbLong());
				IDiscordUser discordUser = await e.Guild.GetUserAsync(uid);
				User account = await User.GetAsync(context, discordUser);

				EmojiBarSet onBarSet = new EmojiBarSet(
					"<:mbarlefton:391971424442646534>", 
					"<:mbarmidon:391971424920797185>", 
					"<:mbarrighton:391971424488783875>");

				EmojiBarSet offBarSet = new EmojiBarSet(
					"<:mbarleftoff:391971424824459265>", 
					"<:mbarmidoff:391971424824197123>", 
					"<:mbarrightoff:391971424862208000>");

				string icon = "";

				if(await account.IsDonatorAsync(context))
				{
					icon = "https://cdn.discordapp.com/emojis/421969679561785354.png";
				}

				if (account != null)
				{
					IDiscordEmbed embed = Utils.Embed
						.SetDescription(account.Title)
						.SetAuthor(locale.GetString("miki_global_profile_user_header", account.Name), icon, "https://patreon.com/mikibot")
						.SetThumbnailUrl(discordUser.AvatarUrl);

					long serverid = e.Guild.Id.ToDbLong();

					LocalExperience localExp = account.LocalExperience.FirstOrDefault(x => x.ServerId == e.Guild.Id.ToDbLong());
					if(localExp == null)
					{
						localExp = await LocalExperience.CreateAsync(context, serverid, id);
					}

					int rank = await localExp.GetRank(context);
					int localLevel = User.CalculateLevel(localExp.Experience);
					int maxLocalExp = User.CalculateLevelExperience(localLevel);
					int minLocalExp = User.CalculateLevelExperience(localLevel - 1);
					EmojiBar expBar = new EmojiBar(maxLocalExp - minLocalExp, onBarSet, offBarSet, 6);

					string infoValue = new MessageBuilder()
						.AppendText(locale.GetString("miki_module_accounts_information_level", localLevel, localExp.Experience, maxLocalExp))
						.AppendText(await expBar.Print(localExp.Experience - minLocalExp, e.Channel))
						.AppendText(locale.GetString("miki_module_accounts_information_rank", rank))
						.AppendText("Reputation: " + account.Reputation, MessageFormatting.Plain, false)
						.Build();
					embed.AddInlineField(locale.GetString("miki_generic_information"), infoValue);

					int globalLevel = User.CalculateLevel(account.Total_Experience);
					int maxGlobalExp = User.CalculateLevelExperience(globalLevel);
					int minGlobalExp = User.CalculateLevelExperience(globalLevel -1);

					int globalRank = await account.GetGlobalRankAsync();

					EmojiBar globalExpBar = new EmojiBar(maxGlobalExp - minGlobalExp, onBarSet, offBarSet, 6);

					string globalInfoValue = new MessageBuilder()
						.AppendText(locale.GetString("miki_module_accounts_information_level", globalLevel, account.Total_Experience, maxGlobalExp))
						.AppendText(await globalExpBar.Print(account.Total_Experience - minGlobalExp, e.Channel))
						.AppendText(locale.GetString("miki_module_accounts_information_rank", globalRank), MessageFormatting.Plain, false)
						.Build();

					embed.AddInlineField(locale.GetString("miki_generic_global_information"), globalInfoValue);
					embed.AddInlineField(locale.GetString("miki_generic_mekos"), account.Currency + "<:mekos:421972155484471296>");

					List<Marriage> Marriages = account.Marriages?
						.Select(x => x.Marriage)
						.Where(x => !x.IsProposing)
						.OrderBy(mar => mar.TimeOfMarriage)
						.ToList();

					List<User> users = new List<User>();

					int maxCount = Marriages?.Count ?? 0;

					for (int i = 0; i < maxCount; i++)
					{
						users.Add(await context.Users.FindAsync(Marriages[i].GetOther(id)));
					}

					if (Marriages?.Count > 0)
					{
						List<string> MarriageStrings = new List<string>();

						for (int i = 0; i < maxCount; i++)
						{
							if (Marriages[i].GetOther(id) != 0)
							{
								MarriageStrings.Add($"💕 {users[i].Name} (_{Marriages[i].TimeOfMarriage.ToShortDateString()}_)");
							}
						}

						embed.AddInlineField(
							locale.GetString("miki_module_accounts_profile_marriedto"),
							string.Join("\n", MarriageStrings));
					}

					Random r = new Random((int)id - 3);

					embed.Color = new Miki.Common.Color((float)r.NextDouble(), (float)r.NextDouble(),
						(float)r.NextDouble());

					CommandUsage favouriteCommand = account.CommandsUsed?
						.OrderByDescending(c => c.Amount)
						.FirstOrDefault();

					string favCommand = $"{favouriteCommand?.Name ?? locale.GetString("miki_placeholder_null")} ({ favouriteCommand?.Amount ?? 0 })";

					embed.AddInlineField(locale.GetString("miki_module_accounts_profile_favourite_command"),
						favCommand);

					if (account.Achievements != null)
					{
						string achievements = AchievementManager.Instance.PrintAchievements(account.Achievements);

					embed.AddInlineField(
						locale.GetString("miki_generic_achievements"),
						achievements != "" ? achievements : locale.GetString("miki_placeholder_null"));
					}

					embed.SetFooter(
						locale.GetString("miki_module_accounts_profile_footer", account.DateCreated.ToShortDateString(),
							sw.ElapsedMilliseconds), "");

					sw.Stop();

					embed.QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed(locale.GetString("error_account_null"))
						.QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "rep")]
		public async Task GiveReputationAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				Locale locale = new Locale(e.Channel.Id.ToDbLong());
				ArgObject arg = e.Arguments.FirstOrDefault();

				if (arg == null)
					return;

				User giver = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				Dictionary<IDiscordUser, int> usersMentioned = new Dictionary<IDiscordUser, int>();

				int totalAmountGiven = 0;
				bool mentionedSelf = false;

				var repObject = Global.redisClient.Get<ReputationObject>($"user:{giver.Id}:rep");

				if (repObject == null)
				{
					repObject = new ReputationObject()
					{
						LastReputationGiven = DateTime.Now,
						ReputationPointsLeft = 3
					};
				}

				while (true || totalAmountGiven <= repObject.ReputationPointsLeft)
				{
					if (arg == null)
						break;

					IDiscordUser u = await arg.GetUserAsync(e.Guild);
					int amount = 1;

					if (u == null)
						break;

					arg = arg?.Next();

					if ((arg?.AsInt(-1) ?? -1) != -1)
					{
						amount = arg.AsInt();
						arg = arg.Next();
					}
					else if (Utils.IsAll(arg, locale))
					{
						amount = repObject.ReputationPointsLeft;
						arg = arg.Next();
					}

					if (u.Id == e.Author.Id)
					{
						mentionedSelf = true;
						continue;
					}

					totalAmountGiven += amount;

					if (usersMentioned.Keys.Where(x => x.Id == u.Id).Count() > 0)
					{
						usersMentioned[usersMentioned.Keys.Where(x => x.Id == u.Id).First()] += amount;
					}
					else
					{
						usersMentioned.Add(u, amount);
					}
				}

				IDiscordEmbed embed = Utils.Embed;

				if(mentionedSelf)
				{
					embed.SetFooter(e.GetResource("warning_mention_self"), "");
				}

				if (usersMentioned.Count == 0)
				{
					TimeSpan pointReset = (DateTime.Now.AddDays(1).Date - DateTime.Now);

					embed.SetTitle(locale.GetString("miki_module_accounts_rep_header"))
						.SetDescription(locale.GetString("miki_module_accounts_rep_description"))
						.AddInlineField(locale.GetString("miki_module_accounts_rep_total_received"), giver.Reputation.ToString())
						.AddInlineField(locale.GetString("miki_module_accounts_rep_reset"), pointReset.ToTimeString(e.Channel.GetLocale()))
						.AddInlineField(locale.GetString("miki_module_accounts_rep_remaining"), repObject.ReputationPointsLeft)
						.QueueToChannel(e.Channel);
					return;
				}
				else
				{
					if (totalAmountGiven <= 0)
					{
						e.ErrorEmbedResource("miki_module_accounts_rep_error_zero")
							.QueueToChannel(e.Channel);
						return;
					}

					if(usersMentioned.Sum(x => x.Value) > repObject.ReputationPointsLeft)
					{
						e.ErrorEmbedResource("error_rep_limit", usersMentioned.Count, usersMentioned.Sum(x => x.Value), repObject.ReputationPointsLeft)
							.QueueToChannel(e.Channel);
						return;
					}
				}

				embed.SetTitle(locale.GetString("miki_module_accounts_rep_header"))
					.SetDescription(locale.GetString("rep_success"));

				foreach (var user in usersMentioned)
				{
					User receiver = await User.GetAsync(context, user.Key);

					receiver.Reputation += user.Value;

					embed.AddInlineField(receiver.Name, string.Format("{0} => {1} (+{2})", receiver.Reputation - user.Value, receiver.Reputation, user.Value));
				}

				repObject.ReputationPointsLeft -= (short)(usersMentioned.Sum(x => x.Value));

				await Global.redisClient.AddAsync($"user:{giver.Id}:rep", repObject);

				embed.AddInlineField(locale.GetString("miki_module_accounts_rep_points_left"), repObject.ReputationPointsLeft)
					.QueueToChannel(e.Channel);

				await context.SaveChangesAsync();
			}
		}

		// TODO: rework into miki api
		//[Command(Name = "syncavatar")]
		//public async Task SyncAvatarAsync(EventContext e)
		//{
		//	string localFilename = @"c:\inetpub\miki.veld.one\assets\img\user\" + e.Author.Id + ".png";

		//	HttpWebRequest request = (HttpWebRequest)WebRequest.Create(e.Author.GetAvatarUrl());
		//	HttpWebResponse response = (HttpWebResponse)request.GetResponse();

		//	// Check that the remote file was found. The ContentType
		//	// check is performed since a request for a non-existent
		//	// image file might be redirected to a 404-page, which would
		//	// yield the StatusCode "OK", even though the image was not
		//	// found.
		//	if ((response.StatusCode == HttpStatusCode.OK ||
		//		 response.StatusCode == HttpStatusCode.Moved ||
		//		 response.StatusCode == HttpStatusCode.Redirect) &&
		//		response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
		//	{
		//		// if the remote file was found, download oit
		//		using (Stream inputStream = response.GetResponseStream())
		//		using (Stream outputStream = File.OpenWrite(localFilename))
		//		{
		//			byte[] buffer = new byte[4096];
		//			int bytesRead;
		//			do
		//			{
		//				bytesRead = inputStream.Read(buffer, 0, buffer.Length);
		//				outputStream.Write(buffer, 0, bytesRead);
		//			} while (bytesRead != 0);
		//		}
		//	}

		//	using (var context = new MikiContext())
		//	{
		//		User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
		//		if (user == null)
		//		{
		//			return;
		//		}
		//		user.AvatarUrl = e.Author.Id.ToString();
		//		await context.SaveChangesAsync();
		//	}

		//	IDiscordEmbed embed = Utils.Embed;
		//	embed.Title = "👌 OKAY";
		//	embed.Description = e.GetResource("sync_success", e.GetResource("term_avatar"));
		//	embed.QueueToChannel(e.Channel);
		//}

		[Command(Name = "syncname")]
		public async Task SyncNameAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				if (user == null)
				{
					return;
				}

				user.Name = e.Author.Username;
				await context.SaveChangesAsync();
			}

			IDiscordEmbed embed = Utils.Embed;
			embed.Title = "👌 OKAY";
			embed.Description = e.GetResource("sync_success", "name");	
			embed.QueueToChannel(e.Channel);
		}

		[Command(Name = "mekos", Aliases = new string[] { "bal", "meko" })]
		public async Task ShowMekosAsync(EventContext e)
		{
			ulong targetId = e.message.MentionedUserIds.Count > 0 ? e.message.MentionedUserIds.First() : 0;

			if (e.message.MentionedUserIds.Count > 0)
			{
				if (targetId == 0)
				{
					e.ErrorEmbedResource("miki_module_accounts_mekos_no_user")
						.QueueToChannel(e.Channel);
					return;
				}
				IDiscordUser userCheck = await e.Guild.GetUserAsync(targetId);
				if (userCheck.IsBot)
				{
					e.ErrorEmbedResource("miki_module_accounts_mekos_bot")
						.QueueToChannel(e.Channel);
					return;
				}
			}

			using (var context = new MikiContext())
			{
				User user = await User.GetAsync(context, await e.Guild.GetUserAsync(targetId != 0 ? targetId : e.Author.Id));

				IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());
				embed.Title = "🔸 Mekos";
				embed.Description = e.GetResource("miki_user_mekos", user.Name, user.Currency);
				embed.Color = new Common.Color(1f, 0.5f, 0.7f);

				embed.QueueToChannel(e.Channel);
				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "give")]
		public async Task GiveMekosAsync(EventContext e)
		{
			Locale locale = new Locale(e.Guild.Id);

			if (e.Arguments.Count < 2)
			{
				e.ErrorEmbedResource("give_error_no_arg")
					.QueueToChannel(e.Channel);
				return;
			}

			ArgObject arg = e.Arguments.FirstOrDefault();

			IDiscordUser user = null;

			if (arg != null)
			{
				user = await arg.GetUserAsync(e.Guild);
			}

			if (user == null)
			{
				e.ErrorEmbedResource("give_error_no_mention")
					.QueueToChannel(e.Channel);
				return;
			}

			arg = arg.Next();

			int? amount = arg?.AsInt() ?? null;

			if (amount == null)
			{
				e.ErrorEmbedResource("give_error_amount_unparsable")
					.QueueToChannel(e.Channel);
				return;
			}

			if (amount > 999999)
			{
				e.ErrorEmbedResource("give_error_max_mekos")
					.QueueToChannel(e.Channel);
				return;
			}

			if (amount <= 0)
			{
				e.ErrorEmbedResource("give_error_min_mekos")
					.QueueToChannel(e.Channel);
				return;
			}

			using (MikiContext context = new MikiContext())
			{
				User sender = await User.GetAsync(context, e.Author);
				User receiver = await User.GetAsync(context, user);

				if (amount.Value <= sender.Currency)
				{
					await sender.AddCurrencyAsync(-amount.Value, e.Channel, sender);

					IDiscordEmbed em = Utils.Embed;
					em.Title = "🔸 transaction";
					em.Description = e.GetResource("give_description", sender.Name, receiver.Name, amount.Value);

					em.Color = new Miki.Common.Color(255, 140, 0);

					em.QueueToChannel(e.Channel);
					await context.SaveChangesAsync();
				}
				else
				{
					e.ErrorEmbedResource("user_error_insufficient_mekos")
						.QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "daily")]
		public async Task GetDailyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				Locale locale = new Locale(e.Channel.Id);

				User u = await User.GetAsync(context, e.Author);

				if (u == null)
				{
					e.ErrorEmbed(e.GetResource("user_error_no_account"))
						.QueueToChannel(e.Channel);
					return;
				}

				int dailyAmount = 100;
				int dailyStreakAmount = 20;

				if (await u.IsDonatorAsync(context))
				{
					dailyAmount *= 2;
					dailyStreakAmount *= 2;
				}

				if (u.LastDailyTime.AddHours(23) >= DateTime.Now)
				{
					e.ErrorEmbed(
						$"You already claimed your daily today! Please wait another `{(u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString(e.Channel.GetLocale())}` before using it again.").QueueToChannel(e.Channel);
					return;
				}

				int streak = 1;
				string redisKey = $"user:{e.Author.Id}:daily";

				if (await Global.redisClient.ExistsAsync(redisKey))
				{
					streak = await Global.redisClient.GetAsync<int>(redisKey);
					streak++;
				}

				int amount = dailyAmount + (dailyStreakAmount * Math.Min(100, streak));

				await u.AddCurrencyAsync(amount, e.Channel);
				u.LastDailyTime = DateTime.Now;


				Utils.Embed.SetTitle("💰 Daily")
					.SetDescription($"Received **{amount}** Mekos! You now have `{u.Currency}` Mekos")
					.SetColor(253, 216, 136)
					.AddInlineField("Streak!", $"You're on a {streak} day daily streak!")
					.QueueToChannel(e.Channel);

				await Global.redisClient.AddAsync(redisKey, streak, new TimeSpan(48, 0, 0));
				await context.SaveChangesAsync();
			}
		}

		/*[Command(Name = "setrolelevel")]
		public async Task SetRoleLevelAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				Locale locale = new Locale(e.Channel.Id.ToDbLong());

				List<string> allArgs = new List<string>();
				allArgs.AddRange(e.arguments.Split(' '));
				if (allArgs.Count >= 2)
				{
					int levelrequirement = int.Parse(allArgs[allArgs.Count - 1]);
					allArgs.RemoveAt(allArgs.Count - 1);
					IDiscordRole role = e.Guild.Roles
						.Find(r => r.Name.ToLower() == string.Join(" ", allArgs).TrimEnd(' ').TrimStart(' ').ToLower());

					if (role == null)
					{
						e.ErrorEmbed(e.GetResource("error_role_not_found"))
							.QueueToChannel(e.Channel);
						return;
					}

					LevelRole lr = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
					if (lr == null)
					{
						lr = context.LevelRoles.Add(new LevelRole()
						{
							GuildId = e.Guild.Id.ToDbLong(),
							RoleId = role.Id.ToDbLong(),
							RequiredLevel = levelrequirement
						}).Entity;

						IDiscordEmbed embed = Utils.Embed;
						embed.Title = "Added Role!";
						embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";

						IDiscordUser currentUser = await e.GetCurrentUserAsync();

						if (!currentUser.HasPermissions(e.Channel, DiscordGuildPermission.ManageRoles))
						{
							embed.AddInlineField(e.GetResource("miki_warning"), e.GetResource("setrolelevel_error_no_permissions", $"`{e.GetResource("permission_manage_roles")}`"));
						}

						embed.QueueToChannel(e.Channel);
					}
					else
					{
						lr.RequiredLevel = levelrequirement;

						IDiscordEmbed embed = Utils.Embed;
						embed.Title = "Updated Role!";
						embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";
						embed.QueueToChannel(e.Channel);
					}
					await context.SaveChangesAsync();
				}
				else
				{
					e.ErrorEmbed("Make sure to fill out both the role and the level when creating this!")
						.QueueToChannel(e.Channel);
				}
			}
		}*/

		//[Command(Name = "mybadges")]
		//public async Task MyBadgesAsync(EventContext e)
		//{
		//	int page = 0;
		//	using (var context = new MikiContext())
		//	{
		//		User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

		//		string output = string.Join<long>(" ", u.BadgesOwned.Select(x => x.Id).ToList());

		//		await e.Channel.SendMessage(output.DefaultIfEmpty("none, yet!"));
		//	}
		//}

		public async Task ShowLeaderboardsAsync(IDiscordMessage mContext, LeaderboardsOptions leaderboardOptions)
		{
			using (var context = new MikiContext())
			{
				Locale locale = new Locale(mContext.Channel.Id);

				int p = Math.Max(leaderboardOptions.pageNumber - 1, 0);

				if (Global.MikiApi == null)
				{
					IDiscordEmbed embed = Utils.Embed
						.SetColor(1.0f, 0.6f, 0.4f);

					switch (leaderboardOptions.type)
					{
						case LeaderboardsType.COMMANDS:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_commands_header");
							if (leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();

								var mentionedUser = await context.Users.FindAsync(mentionedId);
								p = (int)Math.Ceiling((double)(((await mentionedUser.GetGlobalCommandsRankAsync()) - 1) / 12));
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Total_Commands)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for (int i = 0; i < output.Count; i++)
							{
								string nameToOutput = leaderboardOptions.mentionedUserId != 0 ? string.Join("", output[i].Name.Take(16)) : "~" + string.Join("", output[i].Name.Take(16)) + "~";
								embed.AddInlineField($"#{i + (12 * p) + 1}: {nameToOutput}", $"{output[i].Total_Commands} commands used!");
							}
						}
						break;

						case LeaderboardsType.CURRENCY:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_mekos_header");
							if (leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								var mentionedUser = await context.Users.FindAsync(mentionedId);
								p = (int)Math.Ceiling((double)(((await mentionedUser.GetGlobalMekosRankAsync()) - 1) / 12));
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Currency)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for (int i = 0; i < output.Count; i++)
							{
								embed.AddInlineField($"#{i + (12 * p) + 1}: {string.Join("", output[i].Name.Take(16))}",
									$"{output[i].Currency} mekos!");
							}
						}
						break;

						case LeaderboardsType.EXPERIENCE:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_header");
							if (leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								var mentionedUser = await context.Users.FindAsync(mentionedId);
								p = (int)Math.Ceiling((double)(((await mentionedUser.GetGlobalRankAsync()) - 1) / 12));
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Total_Experience)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for (int i = 0; i < output.Count; i++)
							{
								embed.AddInlineField($"#{i + (12 * p) + 1}: {string.Join("", output[i].Name.Take(16))}",
									$"{output[i].Total_Experience} experience!");
							}
						}
						break;

						case LeaderboardsType.REPUTATION:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_reputation_header");
							if (leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								var mentionedUser = await context.Users.FindAsync(mentionedId);
								p = (int)Math.Ceiling((double)(((await mentionedUser.GetGlobalReputationRankAsync()) - 1) / 12));
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Reputation)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for (int i = 0; i < output.Count; i++)
							{
								embed.AddInlineField($"#{i + (12 * p) + 1}: {string.Join("", output[i].Name.Take(16))}",
									$"{output[i].Reputation} reputation!");
							}
						}
						break;

						case LeaderboardsType.PASTA:
						{
							List<GlobalPasta> leaderboards = await context.Pastas
								.OrderByDescending(x => x.Score)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							embed.SetTitle(locale.GetString("toppasta_title"));

							foreach (GlobalPasta t in leaderboards)
							{
								int amount = t.Score;
								embed.AddInlineField(t.Id, (t == leaderboards.First() ? "💖 " + amount : (amount < 0 ? "💔 " : "❤ ") + amount));
							}
						}
						break;
					}


					embed.SetFooter(locale.GetString("page_index", p + 1, Math.Ceiling(context.Users.Count() / 12f)), "")
						.QueueToChannel(mContext.Channel);
				}
				else
				{
					LeaderboardsObject obj = await Global.MikiApi.GetPagedLeaderboardsAsync(leaderboardOptions);

					Utils.RenderLeaderboards(Utils.Embed, obj.items, obj.currentPage * 10)
						.SetFooter(locale.GetString("page_index", p + 1, Math.Ceiling((double)obj.totalItems / 10)), "")
						.SetTitle($"Leaderboards: {leaderboardOptions.type.ToString()}")
						.QueueToChannel(mContext.Channel);
				}
			}
		}
		
		[Command(Name = "vote")]
		public async Task VoteAsync(EventContext e)
		{
			Utils.Embed
				.SetTitle("Support Miki on Discord Bots")
				.SetDescription("Show your support for Miki by voting for her every 24 hours on [DiscordBots](https://discordbots.org/bot/160105994217586689/vote)! The 200 mekos doesn't hurt, either. ;)")
				.QueueToChannel(e.Channel);

			await Task.Yield();
		}		
	}
}
