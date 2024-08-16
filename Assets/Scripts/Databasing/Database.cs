using System;
using System.Collections.Generic;
using System.IO;
using DBreeze;
using DBreeze.Objects;
using DBreeze.Transactions;
using DBreeze.Utils;
using Hypernex.CCK;
using Hypernex.Configuration.ConfigMeta;
using Hypernex.Tools;
using Nexport;

namespace Hypernex.Databasing
{
    public class Database : IDisposable
    {
        private string server;
        private string userid;
        private DBreezeEngine engine;

        public Database(ConfigUser configUser)
        {
            server = configUser.Server;
            userid = configUser.UserId;
            string pathToDatabase = Path.Combine(Init.Instance.GetDatabaseLocation(),
                DownloadTools.GetStringHash(configUser.Server), configUser.UserId);
            engine = new DBreezeEngine(pathToDatabase);
            CustomSerializator.ByteArraySerializator = Msg.Serialize;
            CustomSerializator.ByteArrayDeSerializator = Msg.Deserialize;
        }

        public bool IsSame(ConfigUser configUser)
        {
            if (configUser == null) return false;
            return configUser.Server == server && configUser.UserId == userid;
        }

        public T Insert<T>(string table, T value) where T : IIndex
        {
            try
            {
                using Transaction t = engine.GetTransaction();
                t.ObjectInsert(table, new DBreezeObject<T>
                {
                    NewEntity = false,
                    Entity = value,
                    Indexes = new List<DBreezeIndex>
                    {
                        new (1, value.Id) { PrimaryIndex = true }
                    }
                });
                t.Commit();
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
            return value;
        }

        public T Get<T>(string table, string id)
        {
            try
            {
                using Transaction t = engine.GetTransaction();
                DBreezeObject<T> o = t.Select<byte[], byte[]>(table, 1.ToIndex(id)).ObjectGet<T>();
                return o.Entity;
            }
            catch (Exception)
            {
                // Probably missing, return null/default
                return default;
            }
        }

        public void Dispose() => engine.Dispose();
    }
}