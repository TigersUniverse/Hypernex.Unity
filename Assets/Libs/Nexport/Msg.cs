#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePack;
using MessagePack.Resolvers;

namespace Nexport
{
    public class Msg : MessagePackObjectAttribute
    {
        public static bool UseCompression { get; set; } = true;
        public static readonly Dictionary<string, Type> RegisteredMessages = new Dictionary<string, Type>();

        private static void msgCheck(Type type)
        {
            bool foundMessageId = false;
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                MsgKey[] attributes = (MsgKey[]) GetCustomAttributes(propertyInfo, typeof(MsgKey));
                if (attributes.Length > 0)
                {
                    if (attributes.Length > 1)
                        throw new Exception("Cannot have multiple MsgKeys on one property!");
                    MsgKey target = attributes[0];
                    if (propertyInfo.Name == "MessageId")
                    {
                        if (target.Identifier != 1)
                            throw new Exception("MessageId must have a MsgKey Identifier of 1!");
                        foundMessageId = true;
                    }
                    else
                    {
                        if (target.Identifier == 1)
                            throw new Exception("MessageId must have a MsgKey Identifier of 1!");
                    }
                }
            }
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                MsgKey[] attributes = (MsgKey[]) GetCustomAttributes(fieldInfo, typeof(MsgKey));
                if (attributes.Length > 0)
                {
                    if (attributes.Length > 1)
                        throw new Exception("Cannot have multiple MsgKeys on one property!");
                    MsgKey target = attributes[0];
                    if (fieldInfo.Name == "MessageId")
                    {
                        if (target.Identifier != 1)
                            throw new Exception("MessageId must have a MsgKey Identifier of 1!");
                        foundMessageId = true;
                    }
                    else
                    {
                        if (target.Identifier == 1)
                            throw new Exception("MessageId must have a MsgKey Identifier of 1!");
                    }
                }
            }
            if (!foundMessageId)
                throw new Exception("Your Msg must contain a MessageId with an Identifier of 1!");
        }

        private static void loopMessages(Assembly? assembly)
        {
            if (assembly == null)
                return;
            IEnumerable<Type> types;
            try
            {
                types = from type in assembly.GetTypes() where IsDefined(type, typeof(Msg)) select type;
            }
            catch (ReflectionTypeLoadException e)
            {
                types = new List<Type>();
                IEnumerable<Type?> t = e.Types.Where(t => t != null);
                foreach (Type? type in t)
                    if(type != null && IsDefined(type, typeof(Msg)))
                        ((List<Type>) types).Add(type);
            }
            foreach (Type type in types)
            {
                if (type.FullName != null)
                {
                    msgCheck(type);
                    if(!RegisteredMessages.ContainsKey(type.FullName))
                        RegisteredMessages.Add(type.FullName ?? throw new Exception("Failed to get FullName of type " + type), type);
                }
            }
        }

        public static void RefreshMessageTypes()
        {
            RegisteredMessages.Clear();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                loopMessages(assembly);
            loopMessages(Assembly.GetExecutingAssembly());
            loopMessages(Assembly.GetEntryAssembly());
        }

        public static void LoadCustomAssembly(Assembly? assembly) => loopMessages(assembly);

        static Msg()
        {
            if(RegisteredMessages.Count <= 0)
                try
                {
                    RefreshMessageTypes();
                }
                catch(Exception){}
        }

        public static byte[] Serialize<T>(T obj)
        {
            if (UseCompression)
                return MessagePackSerializer.Serialize(obj,
                    MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
            return MessagePackSerializer.Serialize(obj);
        }

        public static T Deserialize<T>(byte[] data)
        {
            if (UseCompression)
            {
                var options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
                return MessagePackSerializer.Deserialize<T>(data, options);
            }
            return MessagePackSerializer.Deserialize<T>(data);
        }

        public static object? Deserialize(Type targetType, byte[] data)
        {
            if (UseCompression)
            {
                var options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
                return MessagePackSerializer.Deserialize(targetType, data, options);
            }
            return MessagePackSerializer.Deserialize(targetType, data);
        }

        public static MsgMeta? GetMeta(byte[] data)
        {
            try
            {
                dynamic dynamicModel;
                if (UseCompression)
                    dynamicModel = MessagePackSerializer.Deserialize<dynamic>(data,
                        ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray));
                else
                    dynamicModel = MessagePackSerializer.Deserialize<dynamic>(data, ContractlessStandardResolver.Options);
                string msgid = dynamicModel[1];
                if (!RegisteredMessages.ContainsKey(msgid))
                    return null;
                Type t = RegisteredMessages[msgid];
                object? d = Deserialize(t, data);
                return d != null ? new MsgMeta(data, d, msgid, t) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
        
    public class MsgKey : KeyAttribute
    {
        public int Identifier { get; }
        public MsgKey(int id) : base(id) => Identifier = id;
    }

    public class MsgMeta
    {
        public byte[] RawData { get; }
        public string DataId { get; }
        public object Data { get; }
        public Type TypeOfData { get; }

        public MsgMeta(byte[] rawData, object data, string dataId, Type typeOfData)
        {
            RawData = rawData;
            DataId = dataId;
            Data = data;
            TypeOfData = typeOfData;
        }
    }
}