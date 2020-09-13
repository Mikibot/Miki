﻿using System;
using System.Collections.Generic;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Enums;
using Miki.API.Imageboards.Interfaces;
using Miki.API.Imageboards.Objects;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.UrbanDictionary;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Miki.Attributes;
using Miki.Bot.Models;
using Miki.Exceptions;
using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Modules.Accounts.Services;
using Miki.Services.Achievements;
using Miki.Utility;
using Newtonsoft.Json;

namespace Miki.Modules
{
    [Module("nsfw"), Emoji(AppProps.Emoji.HotFace)]
	internal class NsfwModule
	{
        public NsfwModule(Config config)
        {
            ImageboardProviderPool.AddProvider<E621Post>(new ImageboardProvider(
                new ImageboardConfigurations
                {
                    QueryKey = new Uri("http://e621.net/posts.json?tags="),
                    ExplicitTag = "rating:e",
                    QuestionableTag = "rating:q",
                    SafeTag = "rating:s",
                    NetUseCredentials = true,
                    NetHeaders = new List<Tuple<string, string>>()
                    {
                        new Tuple<string, string>("User-Agent", "MikiBot"),
                    },
                    BlacklistedTags =
                    {
                        "loli",
                        "shota",
                        "gore"
                    },
                    mapper = content => MikiRandom.Of(
                        JsonConvert.DeserializeObject<E621Response>(content).Posts.Where(
                            post => post.File.Ext != "webm"))
                }));
            ImageboardProviderPool.AddProvider<DanbooruPost>(new ImageboardProvider(
                new ImageboardConfigurations
                {
                    QueryKey = new Uri("https://danbooru.donmai.us/posts.json?tags="),
                    ExplicitTag = "rating:e",
                    QuestionableTag = "rating:q",
                    SafeTag = "rating:s",
                    NetUseCredentials = true,
                    NetHeaders =
                    {
                        new Tuple<string, string>(
                            "Authorization",
                            $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(config.OptionalValues?.DanbooruApiKey ?? ""))}"),
                    },
                    BlacklistedTags =
                    {
                        "loli",
                        "shota",
                        "gore"
                    },
                    mapper = content => MikiRandom.Of(
                        JsonConvert.DeserializeObject<List<DanbooruPost>>(content))
                }));
            ImageboardProviderPool.AddProvider<GelbooruPost>(new ImageboardProvider(
                new ImageboardConfigurations
                {
                    QueryKey = new Uri(
                        "http://gelbooru.com/index.php?page=dapi&s=post&q=index&json=1&tags="),
                    BlacklistedTags =
                    {
                        "loli",
                        "shota",
                        "gore"
                    },
                    mapper = content => MikiRandom.Of(
                        JsonConvert.DeserializeObject<List<GelbooruPost>>(content))
                }));
            ImageboardProviderPool.AddProvider<SafebooruPost>(new ImageboardProvider(
                new ImageboardConfigurations
                {
                    QueryKey = new Uri(
                        "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&tags="),
                    BlacklistedTags =
                    {
                        "loli",
                        "shota",
                        "gore"
                    },
                    mapper = content => MikiRandom.Of(
                        JsonConvert.DeserializeObject<List<SafebooruPost>>(content))
                }));
            ImageboardProviderPool.AddProvider<Rule34Post>(new ImageboardProvider(
                new ImageboardConfigurations
                {
                    QueryKey = new Uri(
                        "http://rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags="),
                    BlacklistedTags =
                    {
                        "loli",
                        "shota",
                        "gore"
                    },
                    mapper = content => MikiRandom.Of(
                        JsonConvert.DeserializeObject<List<Rule34Post>>(content))
                }));
            ImageboardProviderPool.AddProvider<KonachanPost>(new ImageboardProvider(
                new ImageboardConfigurations
                {
                    QueryKey = new Uri("https://konachan.com/post.json?tags="),
                    BlacklistedTags =
                    {
                        "loli",
                        "shota",
                    },
                    mapper = content => MikiRandom.Of(
                        JsonConvert.DeserializeObject<List<KonachanPost>>(content))
                }));
            ImageboardProviderPool.AddProvider<YanderePost>(new ImageboardProvider(
                new ImageboardConfigurations
                {
                    QueryKey = new Uri("https://yande.re/post.json?api_version=2&tags="),
                    BlacklistedTags =
                    {
                        "loli",
                        "shota",
                    },
                    mapper = content => MikiRandom.Of(
                        JsonConvert.DeserializeObject<YandereResponse>(content).Posts)
                }));
        }

        [Command("gelbooru", "gel")]
        [NsfwOnly]
		public Task RunGelbooru(IContext e)
            => RunNsfwAsync<GelbooruPost>(e);

        [Command("danbooru", "dan")]
        [NsfwOnly]
        public Task DanbooruAsync(IContext e)
            => RunNsfwAsync<DanbooruPost>(e);

        [Command("rule34", "r34")]
        [NsfwOnly]
        public Task RunRule34(IContext e)
            => RunNsfwAsync<Rule34Post>(e);

        [Command("e621")]
        [NsfwOnly]
        public Task RunE621(IContext e)
            => RunNsfwAsync<E621Post>(e);

        [Command("urban")]
        [NsfwOnly]
        public async Task UrbanAsync(IContext e)
        {
            if(!e.GetArgumentPack().Pack.CanTake)
            {
                return;
            }

            var api = e.GetService<UrbanDictionaryApi>();

            var query = e.GetArgumentPack().Pack.TakeAll();
            var searchResult = await api.SearchTermAsync(query);

            if(searchResult == null)
            {
                // TODO (Veld): Something went wrong/No results found.
                return;
            }

            var entry = searchResult.List.FirstOrDefault();

            if(entry == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_term_invalid"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            string desc = Regex.Replace(entry.Definition, "\\[(.*?)\\]",
                (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionUrl(x.Groups[1].Value)})"
            );

            string example = Regex.Replace(entry.Example, "\\[(.*?)\\]",
                (x) => $"[{x.Groups[1].Value}]({api.GetUserDefinitionUrl(x.Groups[1].Value)})"
            );

            await new EmbedBuilder()
                .SetAuthor($"📚 {entry.Term}", null,
                    "http://www.urbandictionary.com/define.php?term=" + query)
                .SetDescription(e.GetLocale()
                    .GetString("miki_module_general_urban_author", entry.Author))
                .AddField(
                    e.GetLocale().GetString("miki_module_general_urban_definition"), desc, true)
                .AddField(
                    e.GetLocale().GetString("miki_module_general_urban_example"), example, true)
                .SetFooter($"👍 { entry.ThumbsUp:N0} 👎 { entry.ThumbsDown:N0} - Powered by UrbanDictionary")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("yandere")]
        [NsfwOnly]
        public Task RunYandere(IContext e) 
            => RunNsfwAsync<YanderePost>(e);

        private async Task RunNsfwAsync<T>(IContext e)
            where T : BooruPost, ILinkable
        {
            try
            {
                ILinkable s = await ImageboardProviderPool.GetProvider<T>()
                    .GetPostAsync(e.GetArgumentPack().Pack.TakeAll(), ImageRating.EXPLICIT);
                if(!IsValid(s))
                {
                    throw new DataNotFoundException();
                }

                await CreateEmbed(s)
                    .QueueAsync(e, e.GetChannel());
            }
            catch(ArgumentOutOfRangeException)
            {
                throw new DataNotFoundException();
            }
            catch(Exception ex)
            {
                if(!(ex is LocalizedException))
                {
                    await e.ErrorEmbed("Too many tags for this system. sorry :(")
                        .ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                }
                throw;
            }

            await UnlockLewdAchievementAsync(e, e.GetService<AchievementService>());
        }

        private ValueTask UnlockLewdAchievementAsync(IContext e, AchievementService service)
        {
            if(MikiRandom.Next(100) == 50)
            {
                var lewdAchievement = service.GetAchievementOrDefault(AchievementIds.LewdId);
                return new ValueTask(service.UnlockAsync(lewdAchievement, e.GetAuthor().Id));
            }
            return default;
        }

        private DiscordEmbed CreateEmbed(ILinkable s)
            => new EmbedBuilder()
                .SetColor(216, 88, 140)
                .SetAuthor(s.Provider, "https://i.imgur.com/FeRu6Pw.png", "https://miki.ai")
                .AddInlineField("🗒 Tags", FormatTags(s.Tags))
                .AddInlineField("⬆ Score", s.Score)
                .AddInlineField("🔗 Source", $"[click here]({s.Url})")
                .SetImage(s.Url).ToEmbed();

        private static string FormatTags(string tags)
            => string.Join(", ", tags.Split(' ').Select(x => $"`{x}`"));

        private static bool IsValid(ILinkable s)
	        => (s != null) && (!string.IsNullOrWhiteSpace(s.Url));
	}
}