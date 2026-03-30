using MessagePack;
using MessagePack.Resolvers;
using Nexport;
using UnityEngine;

namespace Libs.MessagePack
{
    public class Startup
    {
        static bool serializerRegistered = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (!serializerRegistered)
            {
                StaticCompositeResolver.Instance.Register(
                    global::MessagePack.Resolvers.GeneratedResolver.Instance,
                    global::MessagePack.Resolvers.StandardResolver.Instance
                );
                var options = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
                MessagePackSerializer.DefaultOptions = options;
                Msg.SerializerOptions = options;
            }
        }
    }
}