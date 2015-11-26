using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer.Server;

public class ChatUserInfo
{
    public string User;
}

public struct ChatMessageDTO
{
    public ChatUserInfo UserInfo;
    public string Message;
}

public static class ChatServerExtensions
{
    public static void AddChat(this ISceneHost scene)
    {
        ChatServer.ChatRoomBuilder(scene);
    }
}

public class ChatServer
{
    void Run(IAppBuilder builder)
    {
        builder.SceneTemplate("ChatRoom", ChatRoomBuilder);
    }

     public static void ChatRoomBuilder(ISceneHost scene)
    {
        scene.AddRoute("chat", p => OnMessageReceived(scene, p));
    }

    static void OnMessageReceived(ISceneHost scene, Packet<IScenePeerClient> packet)
    {
        var dto = new ChatMessageDTO();
        dto.UserInfo = packet.Connection.GetUserData<ChatUserInfo>();
        dto.Message = packet.ReadObject<string>();
        scene.Broadcast("chat", dto, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
    }
}
