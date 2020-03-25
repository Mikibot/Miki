namespace Miki.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Api.Leaderboards;
    using Miki.BunnyCDN;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Exceptions;
    using Miki.Framework;
    using Miki.Framework.Arguments;
    using Miki.Framework.Commands;
    using Miki.Helpers;
    using Miki.Localization;
    using Miki.Net.Http;
    using Miki.Services;
    using HttpClient = Net.Http.HttpClient;

    public static class Utils
    {
        public static string EveryonePattern => @"@(everyone|here)";

        public static string ImageRegex => "(http(s?):)([/|.|\\w|\\s|-])*\\.(?:jpg|gif|png)";

        public static string DiscordInviteRegex
            => "(http[s]://)?((discord.gg)|(discordapp.com/invite))/([A-Za-z0-9]+)";

        public static string EscapeEveryone(string text)
            => Regex.Replace(text, EveryonePattern, "@\u200b$1");

        public static T FromEnum<T>(this string argument, T defaultValue)
            where T : Enum
        {
            if (Enum.TryParse(typeof(T), argument, true, out object result))
            {
                return (T)result;
            }
            return defaultValue;
        }

        public static bool TryFromEnum<T>(this string argument, out T value)
            where T : struct
            => Enum.TryParse(argument ?? "", true, out value);

        public static string ToTimeString(this TimeSpan time, Locale instance)
            => ToTimeString(time, instance, false);

        public static string ToTimeString(this TimeSpan time, Locale instance, bool minified)
        {
            var t = new List<TimeValue>();
            if (Math.Floor(time.TotalDays) > 0)
            {
                if (Math.Floor(time.TotalDays) > 1)
                {
                    t.Add(new TimeValue(instance.GetString("time_days"), time.Days, minified));
                }
                else
                {
                    t.Add(new TimeValue(instance.GetString("time_days"), time.Days, minified));
                }
            }

            if(time.Hours > 0)
            {
                t.Add(new TimeValue(
                    instance.GetString(time.Hours > 1 ? "time_hours" : "time_hour"),
                    time.Hours, 
                    minified));
            }

            if (time.Minutes > 0)
            {
                if (time.Minutes > 1)
                {
                    t.Add(new TimeValue(instance.GetString("time_minutes"), time.Minutes, minified));
                }
                else
                {
                    t.Add(new TimeValue(instance.GetString("time_minute"), time.Minutes, minified));
                }
            }

            if (t.Count == 0 || time.Seconds > 0)
            {
                if (time.Seconds > 1)
                {
                    t.Add(new TimeValue(instance.GetString("time_seconds"), time.Seconds, minified));
                }
                else
                {
                    t.Add(new TimeValue(instance.GetString("time_second"), time.Seconds, minified));
                }
            }

            if (t.Count != 0)
            {
                var s = new List<string>();
                foreach (TimeValue v in t)
                {
                    s.Add(v.ToString());
                }

                string text = "";
                if (t.Count > 1)
                {
                    int offset = 1;
                    if (minified)
                    {
                        offset = 0;
                    }
                    text = string.Join(", ", s.ToArray(), 0, s.Count - offset);

                    if (!minified)
                    {
                        text += $", {instance.GetString("time_and")} {s[^1]}";
                    }
                }
                else if (t.Count == 1)
                {
                    text = s[0];
                }

                return text;
            }
            return "";
        }

        /// <summary>
        /// Gets the root of an exception
        /// </summary>
        public static Exception GetRootException(this Exception e)
        {
            if(e.InnerException != null)
            {
                return e.InnerException.GetRootException();
            }
            return e;
        }

        public static IDiscordUser GetAuthor(this IContext c)
            => c.GetMessage().Author;

        public static bool IsAll(string input)
            => input.ToLowerInvariant() == "all" || input == "*";

        public static EmbedBuilder ErrorEmbed(this IContext e, string message)
        {
            var locale = e.GetLocale();
            return new EmbedBuilder()
                .SetTitle(locale.GetString(
                    new IconResource(AppProps.Emoji.Disabled, "miki_error_message_generic")))
                .SetDescription(message)
                .SetColor(1.0f, 0.0f, 0.0f);
        }

        public static EmbedBuilder ErrorEmbedResource(
            this IContext e, string resourceId, params object[] args)
            => ErrorEmbed(e, e.GetLocale().GetString(resourceId, args));

        public static EmbedBuilder ErrorEmbedResource(this IContext e, IResource resource)
            => ErrorEmbed(e, resource.Get(e.GetLocale()));

        public static DateTime MinDbValue
            => new DateTime(1755, 1, 1, 0, 0, 0);

        public static DiscordEmbed SuccessEmbed(this IContext e, string message)
            => SuccessEmbed(e.GetLocale(), message);
        public static DiscordEmbed SuccessEmbed(this Locale e, string message)
            => new EmbedBuilder
            {
                Title = $"✅  {e.GetString("miki_success_message_generic")}",
                Description = message,
                Color = new Color(119, 178, 85)
            }.ToEmbed();
        public static DiscordEmbed SuccessEmbedResource(
            this IContext e, string resource, params object[] param)
            => SuccessEmbed(e, e.GetLocale().GetString(resource, param));
        public static DiscordEmbed SuccessEmbedResource(
            this Locale e, string resource, params object[] param)
            => SuccessEmbed(e, e.GetString(resource, param));


        public static ValueTask<string> RemoveMentionsAsync(this string arg, IDiscordGuild guild)
        {
            return new Regex("<@!?(\\d+)>")
                .ReplaceAsync(arg, m => ReplaceMentionAsync(guild, m));
        }

        private static async ValueTask<string> ReplaceMentionAsync(
            IDiscordGuild guild, Match match)
        {
            if(match.Groups.Count == 0)
            {
                return match.Value;
            }

            string value = match.Groups[1]?.Value;
            if(string.IsNullOrEmpty(value))
            {
                return match.Value;
            }

            if(!ulong.TryParse(value, out var entityId))
            {
                return match.Value;
            }

            var guildMember = await guild.GetMemberAsync(entityId);
            if(guildMember == null)
            {
                return match.Value;
            }

            return guildMember.Nickname ?? guildMember.Username;
        }

        public static EmbedBuilder RenderLeaderboards(
            EmbedBuilder embed, List<LeaderboardsItem> items, int offset)
        {
            for (int i = 0; i < Math.Min(items.Count, 12); i++)
            {
                embed.AddInlineField(
                    $"#{offset + i + 1}: " + items[i].Name, $"{items[i].Value:n0}");
            }
            return embed;
        }

        public static async Task SyncAvatarAsync(
            [NotNull] IDiscordUser user, 
            IExtendedCacheClient cache, 
            IUserService context, 
            AmazonS3Client s3Service)
        {
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = "miki-cdn",
                Key = $"avatars/{user.Id}.png",
                ContentType = "image/png",
                CannedACL = new S3CannedACL("public-read")
            };

            string avatarUrl = user.GetAvatarUrl();
            using (var client = new HttpClient(avatarUrl, true))
            {
                request.InputStream = await client.GetStreamAsync();
            }

            var response = await s3Service.PutObjectAsync(request);
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new AvatarSyncException();
            }

            try
            {
                await MikiApp.Instance.Services.GetService<BunnyCDNClient>()
                    .PurgeCacheAsync($"https://mikido.b-cdn.net/avatars/{user.Id}.png");
            } catch(HttpRequestException) { /* ignored */ }

            var mikiUser = await context.GetOrCreateUserAsync(user);
            await cache.HashUpsertAsync("avtr:sync", user.Id.ToString(), 1);
            mikiUser.AvatarUrl = mikiUser.Id.ToString();

            await context.UpdateUserAsync(mikiUser);
            await context.SaveAsync();
        }

        public static string TakeAll(this IArgumentPack pack)
        {
            List<string> allItems = new List<string>();
            while (pack.CanTake)
            {
                allItems.Add(pack.Take());
            }
            return string.Join(" ", allItems);
        }
    }

    public class TimeValue
    {
        public int Value { get; set; }
        public string Identifier { get; set; }

        private readonly bool minified;

        public TimeValue(string i, int v)
            : this(i, v, false)
        {
        }
        public TimeValue(string i, int v, bool minified)
        {
            Value = v;
            Identifier = minified
                ? i[0].ToString()
                : i;
            this.minified = minified;
        }

        public override string ToString()
        {
            if(minified)
            {
                return Value + Identifier;
            }
            return Value + " " + Identifier;
        }
    }
}