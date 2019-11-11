using JarvisGroupButlerModule.DB;
using JarvisModuleCore.Attributes;
using JarvisModuleCore.Classes;
using JarvisModuleCore.ML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace JarvisGroupButlerModule
{
    [JarvisModule(new string[] { "EntityFramework.dll", "EntityFramework.SqlServer.dll", "System.Data.SQLite.EF6.dll", "SQLite.CodeFirst.dll", "data.json" })]
    public class GroupButlerModule : JarvisModule
    {
        public override string Id => "jarvis.official.groupbutler";
        public override string Name => "Group butler";
        public override Version Version => Version.Parse("0.0.1");
        private const string mlDataFilePath = "Training\\data.json";
        public override TaskPredictionInput[] MLTrainingData => JsonConvert.DeserializeObject<TaskPredictionInput[]>(File.ReadAllText(mlDataFilePath));
        private readonly MemoryCache adminCache = new MemoryCache("JarvisAdminCache");
        private static readonly TimeSpan adminCachePersistenceTimeSpan = TimeSpan.FromMinutes(15);
        private static readonly string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Crazypokemondev\\JarvisGroupButlerModule\\");
        private static readonly string dbFilePath = Path.Combine(baseDirectory, "db.sqlite");
        private readonly JarvisContext db;

        #region Startup
        public GroupButlerModule()
        {
            Directory.CreateDirectory(baseDirectory);
            db = new JarvisContext(dbFilePath);
        }

        public override void Start(Jarvis jarvis)
        {
            base.Start(jarvis);
            jarvis.OnMessage += (sender, e) =>
            {
                AddOrUpdateUser(e.Message.From);
                AddOrUpdateUser(e.Message.ReplyToMessage?.From);
                AddOrUpdateUser(e.Message.ForwardFrom);
                AddOrUpdateUser(e.Message.ReplyToMessage?.ForwardFrom);
            };
        }
        #endregion

        #region Helper Methods
        private void AddOrUpdateUser(User user)
        {
            if (user == null) return;
            var dbUser = db.Users.Find(user.Id);
            if (dbUser == null) dbUser = db.Users.Add(new DbUser { Id = user.Id });
            dbUser.FirstName = user.FirstName;
            dbUser.LastName = user.LastName;
            dbUser.Username = user.Username;
            db.SaveChanges();
        }

        public async Task<bool> IsAuthorizedInChat(User user, Chat chat, Jarvis jarvis)
        {
            if (jarvis.IsGlobalAdmin(user.Id)) return true;
            int[] adminIdList;
            if (!adminCache.Contains(chat.Id.ToString()) || (adminIdList = (int[])adminCache.Get(chat.Id.ToString())).Length == 0)
            {
                var admins = await jarvis.GetChatAdministratorsAsync(chat.Id);
                adminIdList = admins.Select(x => x.User.Id).ToArray();
                adminCache.Add(chat.Id.ToString(), adminIdList, DateTimeOffset.Now + adminCachePersistenceTimeSpan);
            }
            return adminIdList.Contains(user.Id);
        }

        private int? ExtractUserIfOnlyOne(Message message)
        {
            if (message.Entities.Count(x => x.Type == MessageEntityType.TextMention || x.Type == MessageEntityType.Mention) != 1) return null;
            for (int i = 0; i < message.Entities.Length; i++)
            {
                MessageEntity entity = message.Entities[i];
                switch (entity.Type)
                {
                    case MessageEntityType.TextMention:
                        return entity.User.Id;
                    case MessageEntityType.Mention:
                        string username = message.EntityValues.ElementAt(i);
                        return db.Users.Where(x => x.Username == username).FirstOrDefault()?.Id;
                }
            }
            return null;
        }

        private int? ExtractIdIfOnlyOne(string text)
        {
            Regex maybeInt32 = new Regex("\\b\\d{1,10}\\b");
            var matches = maybeInt32.Matches(text);
            List<int> possibleNumbers = new List<int>();
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Value, out int id)) possibleNumbers.Add(id);
            }
            return possibleNumbers.Count == 1 ? possibleNumbers.First() : (int?)null;
        }
        #endregion

        #region Tasks
        #region Kick
        [JarvisTask("jarvis.official.groupbutler.kick", Command = "/kick", PossibleMessageTypes = PossibleMessageTypes.AllExceptPoll)]
        public async void Kick(Message message, Jarvis jarvis)
        {
            if (!await IsAuthorizedInChat(message.From, message.Chat, jarvis))
            {
                await jarvis.ReplyAsync(message, "I'm sorry, I don't think you are authorized to ask for that.");
                return;
            }
            var targetUserId = message.ReplyToMessage?.From.Id ?? ExtractUserIfOnlyOne(message) ?? ExtractIdIfOnlyOne(message.Text);
            if (!targetUserId.HasValue)
            {
                await jarvis.ReplyAsync(message, "I'm sorry, I can't quite tell who you are talking about...\n" +
                    "You can reply to one of their messages, teach me their username by forwarding one to me or just use their ID.");
                return;
            }
            var target = targetUserId.Value;
            await jarvis.UnbanChatMemberAsync(message.Chat.Id, target); // Why use unban to kick a user? Because KickChatMember bans them. 
                                                                        // (By the way, to unban a user use RestrictChatMember with everything set to true.
                                                                        // Obviously.)
            await jarvis.ReplyAsync(message, "As you wish.");
        }
        #endregion
        #endregion
    }
}
