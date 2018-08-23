﻿using Miki.Common;
using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Models;

namespace Miki.Accounts.Achievements.Objects
{
    public class BasePacket
    {
        public IDiscordUser discordUser;
        public IDiscordChannel discordChannel;
    }

    public class AchievementPacket : BasePacket
    {
        public BaseAchievement achievement;
        public int count;
    }

    public class MessageEventPacket : BasePacket
    {
        public IDiscordMessage message;
    }

    public class UserUpdatePacket : BasePacket
    {
        public IDiscordUser userNew;
    }

    public class TransactionPacket : BasePacket
    {
        public User receiver;
        public User giver;
        public int amount;
    }

    public class CommandPacket : BasePacket
    {
        public IDiscordMessage message;
        public CommandEvent command;
        public bool success;
    }

    public class LevelPacket : BasePacket
    {
        public User account;
        public int level;
    }
}