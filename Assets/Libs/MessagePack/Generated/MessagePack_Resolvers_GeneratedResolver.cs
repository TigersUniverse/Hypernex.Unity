// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168
#pragma warning disable CS1591 // document public APIs

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Resolvers
{
    public class GeneratedResolver : global::MessagePack.IFormatterResolver
    {
        public static readonly global::MessagePack.IFormatterResolver Instance = new GeneratedResolver();

        private GeneratedResolver()
        {
        }

        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                var f = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (f != null)
                {
                    Formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
                }
            }
        }
    }

    internal static class GeneratedResolverGetFormatterHelper
    {
        private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, int> lookup;

        static GeneratedResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<global::System.Type, int>(33)
            {
                { typeof(global::Hypernex.Networking.Messages.WeightedObjectUpdate[]), 0 },
                { typeof(global::System.Collections.Generic.Dictionary<global::Nexport.ClientIdentifier, string>), 1 },
                { typeof(global::System.Collections.Generic.Dictionary<int, global::Hypernex.Networking.Messages.Data.NetworkedObject>), 2 },
                { typeof(global::System.Collections.Generic.Dictionary<string, object>), 3 },
                { typeof(global::System.Collections.Generic.List<object>), 4 },
                { typeof(global::System.Collections.Generic.List<string>), 5 },
                { typeof(global::Hypernex.CCK.NexboxLanguage), 6 },
                { typeof(global::Hypernex.Networking.Messages.Data.WorldObjectAction), 7 },
                { typeof(global::Hypernex.Networking.Messages.AddModerator), 8 },
                { typeof(global::Hypernex.Networking.Messages.BanPlayer), 9 },
                { typeof(global::Hypernex.Networking.Messages.Bulk.BulkWeightedObjectUpdate), 10 },
                { typeof(global::Hypernex.Networking.Messages.Data.float2), 11 },
                { typeof(global::Hypernex.Networking.Messages.Data.float3), 12 },
                { typeof(global::Hypernex.Networking.Messages.Data.float4), 13 },
                { typeof(global::Hypernex.Networking.Messages.Data.NetworkedObject), 14 },
                { typeof(global::Hypernex.Networking.Messages.Data.SinCos), 15 },
                { typeof(global::Hypernex.Networking.Messages.InstancePlayers), 16 },
                { typeof(global::Hypernex.Networking.Messages.JoinAuth), 17 },
                { typeof(global::Hypernex.Networking.Messages.KickPlayer), 18 },
                { typeof(global::Hypernex.Networking.Messages.NetworkedEvent), 19 },
                { typeof(global::Hypernex.Networking.Messages.PlayerDataUpdate), 20 },
                { typeof(global::Hypernex.Networking.Messages.PlayerMessage), 21 },
                { typeof(global::Hypernex.Networking.Messages.PlayerObjectUpdate), 22 },
                { typeof(global::Hypernex.Networking.Messages.PlayerUpdate), 23 },
                { typeof(global::Hypernex.Networking.Messages.PlayerVoice), 24 },
                { typeof(global::Hypernex.Networking.Messages.RemoveModerator), 25 },
                { typeof(global::Hypernex.Networking.Messages.RespondAuth), 26 },
                { typeof(global::Hypernex.Networking.Messages.ServerConsoleExecute), 27 },
                { typeof(global::Hypernex.Networking.Messages.ServerConsoleLog), 28 },
                { typeof(global::Hypernex.Networking.Messages.UnbanPlayer), 29 },
                { typeof(global::Hypernex.Networking.Messages.WarnPlayer), 30 },
                { typeof(global::Hypernex.Networking.Messages.WeightedObjectUpdate), 31 },
                { typeof(global::Hypernex.Networking.Messages.WorldObjectUpdate), 32 },
            };
        }

        internal static object GetFormatter(global::System.Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key))
            {
                return null;
            }

            switch (key)
            {
                case 0: return new global::MessagePack.Formatters.ArrayFormatter<global::Hypernex.Networking.Messages.WeightedObjectUpdate>();
                case 1: return new global::MessagePack.Formatters.DictionaryFormatter<global::Nexport.ClientIdentifier, string>();
                case 2: return new global::MessagePack.Formatters.DictionaryFormatter<int, global::Hypernex.Networking.Messages.Data.NetworkedObject>();
                case 3: return new global::MessagePack.Formatters.DictionaryFormatter<string, object>();
                case 4: return new global::MessagePack.Formatters.ListFormatter<object>();
                case 5: return new global::MessagePack.Formatters.ListFormatter<string>();
                case 6: return new MessagePack.Formatters.Hypernex.CCK.NexboxLanguageFormatter();
                case 7: return new MessagePack.Formatters.Hypernex.Networking.Messages.Data.WorldObjectActionFormatter();
                case 8: return new MessagePack.Formatters.Hypernex.Networking.Messages.AddModeratorFormatter();
                case 9: return new MessagePack.Formatters.Hypernex.Networking.Messages.BanPlayerFormatter();
                case 10: return new MessagePack.Formatters.Hypernex.Networking.Messages.Bulk.BulkWeightedObjectUpdateFormatter();
                case 11: return new MessagePack.Formatters.Hypernex.Networking.Messages.Data.float2Formatter();
                case 12: return new MessagePack.Formatters.Hypernex.Networking.Messages.Data.float3Formatter();
                case 13: return new MessagePack.Formatters.Hypernex.Networking.Messages.Data.float4Formatter();
                case 14: return new MessagePack.Formatters.Hypernex.Networking.Messages.Data.NetworkedObjectFormatter();
                case 15: return new MessagePack.Formatters.Hypernex.Networking.Messages.Data.SinCosFormatter();
                case 16: return new MessagePack.Formatters.Hypernex.Networking.Messages.InstancePlayersFormatter();
                case 17: return new MessagePack.Formatters.Hypernex.Networking.Messages.JoinAuthFormatter();
                case 18: return new MessagePack.Formatters.Hypernex.Networking.Messages.KickPlayerFormatter();
                case 19: return new MessagePack.Formatters.Hypernex.Networking.Messages.NetworkedEventFormatter();
                case 20: return new MessagePack.Formatters.Hypernex.Networking.Messages.PlayerDataUpdateFormatter();
                case 21: return new MessagePack.Formatters.Hypernex.Networking.Messages.PlayerMessageFormatter();
                case 22: return new MessagePack.Formatters.Hypernex.Networking.Messages.PlayerObjectUpdateFormatter();
                case 23: return new MessagePack.Formatters.Hypernex.Networking.Messages.PlayerUpdateFormatter();
                case 24: return new MessagePack.Formatters.Hypernex.Networking.Messages.PlayerVoiceFormatter();
                case 25: return new MessagePack.Formatters.Hypernex.Networking.Messages.RemoveModeratorFormatter();
                case 26: return new MessagePack.Formatters.Hypernex.Networking.Messages.RespondAuthFormatter();
                case 27: return new MessagePack.Formatters.Hypernex.Networking.Messages.ServerConsoleExecuteFormatter();
                case 28: return new MessagePack.Formatters.Hypernex.Networking.Messages.ServerConsoleLogFormatter();
                case 29: return new MessagePack.Formatters.Hypernex.Networking.Messages.UnbanPlayerFormatter();
                case 30: return new MessagePack.Formatters.Hypernex.Networking.Messages.WarnPlayerFormatter();
                case 31: return new MessagePack.Formatters.Hypernex.Networking.Messages.WeightedObjectUpdateFormatter();
                case 32: return new MessagePack.Formatters.Hypernex.Networking.Messages.WorldObjectUpdateFormatter();
                default: return null;
            }
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1649 // File name should match first type name
