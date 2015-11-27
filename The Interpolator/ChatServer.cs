﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
    public static List<ChatServer> scenes = new List<ChatServer>();

    void Run(IAppBuilder builder)
    {
        builder.SceneTemplate("ChatRoom", ChatRoomBuilder);
    }

     public static void ChatRoomBuilder(ISceneHost scene)
    {
        ChatServer cs = new ChatServer(scene);
        scenes.Add(cs);
    }
}

public class ChatServer
{
    private ISceneHost _scene;
    private ConcurrentDictionary<long, ChatUserInfo> UsersInfos = new ConcurrentDictionary<long, ChatUserInfo>();

    void OnMessageReceived(Packet<IScenePeerClient> packet)
    {
        var dto = new ChatMessageDTO();
        ChatUserInfo temp;

        UsersInfos.TryGetValue(packet.Connection.Id, out temp);
        dto.UserInfo = temp;
        dto.Message = packet.ReadObject<string>();
        _scene.Broadcast("chat", dto, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
    }

    void OnUpdateInfo(Packet<IScenePeerClient> packet)
    {
        var info = packet.ReadObject<ChatUserInfo>();

        if (UsersInfos.ContainsKey(packet.Connection.Id) == true)
        {
            ChatUserInfo trash;
            UsersInfos.TryRemove(packet.Connection.Id, out trash);
        }
        UsersInfos.TryAdd(packet.Connection.Id, info);
    }

    public Task OnShutDown(ShutdownArgs args)
    {
        ChatServerRun.scenes.Remove(this);

        return Task.FromResult(true);
    }

    public ChatServer(ISceneHost scene)
    {
        _scene = scene;
        _scene.AddRoute("UpdateInfo", OnUpdateInfo);
        _scene.AddRoute("chat", OnMessageReceived);
        _scene.Shuttingdown.Add(OnShutDown);
    }
}
