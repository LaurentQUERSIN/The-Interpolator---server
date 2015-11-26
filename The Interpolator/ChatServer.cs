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
        ChatServerRun.ChatRoomBuilder(scene);
    }
}

public class ChatServerRun
{
    void Run(IAppBuilder builder)
    {
        builder.SceneTemplate("ChatRoom", ChatRoomBuilder);
    }

     public static void ChatRoomBuilder(ISceneHost scene)
    {
        ChatServer cs = new ChatServer(scene);
    }


}

public class ChatServer
{
    private ISceneHost _scene;

    void OnMessageReceived(Packet<IScenePeerClient> packet)
    {
        var dto = new ChatMessageDTO();
        dto.UserInfo = packet.Connection.GetUserData<ChatUserInfo>();
        dto.Message = packet.ReadObject<string>();
        _scene.Broadcast("chat", dto, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
    }

    public ChatServer(ISceneHost scene)
    {
        _scene = scene;
        _scene.AddRoute("chat", OnMessageReceived);
    }
}
