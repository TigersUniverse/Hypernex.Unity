// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168
#pragma warning disable CS1591 // document public APIs

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Hypernex.Networking.Messages
{
    public sealed class PlayerVoiceFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Hypernex.Networking.Messages.PlayerVoice>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Hypernex.Networking.Messages.PlayerVoice value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(9);
            writer.WriteNil();
            writer.WriteNil();
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Hypernex.Networking.Messages.JoinAuth>(formatterResolver).Serialize(ref writer, value.Auth, options);
            writer.Write(value.Bitrate);
            writer.Write(value.SampleRate);
            writer.Write(value.Channels);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.Encoder, options);
            writer.Write(value.Bytes);
            writer.Write(value.EncodeLength);
        }

        public global::Hypernex.Networking.Messages.PlayerVoice Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var ____result = new global::Hypernex.Networking.Messages.PlayerVoice();

            for (int i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 2:
                        ____result.Auth = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Hypernex.Networking.Messages.JoinAuth>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 3:
                        ____result.Bitrate = reader.ReadInt32();
                        break;
                    case 4:
                        ____result.SampleRate = reader.ReadInt32();
                        break;
                    case 5:
                        ____result.Channels = reader.ReadInt32();
                        break;
                    case 6:
                        ____result.Encoder = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 7:
                        ____result.Bytes = global::MessagePack.Internal.CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes());
                        break;
                    case 8:
                        ____result.EncodeLength = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.Depth--;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name
