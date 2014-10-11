// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using Massive.Examples.NetAdvanced;
using MassiveNet;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour {

    private NetSocket socket;
    private StreamDatabaseServer database;
    private LoginServer loginServer;
    private NetViewManager viewManager;

    void Awake() {
        socket = GetComponent<NetSocket>();
        database = GetComponent<StreamDatabaseServer>();
        loginServer = GetComponent<LoginServer>();
        viewManager = GetComponent<NetViewManager>();

        loginServer.OnLoginSuccess += LoggedIn;

        socket.RegisterRpcListener(this);
        socket.Events.OnClientDisconnected += ClientDisconnected;
    }

    void LoggedIn(NetConnection connection, LoginServer.Account account) {
        if (!database.HasKey(account.Id + "_chars")) socket.Send("OpenCreator", connection);
        else {
            string[] charNames;
            if (!TryGetCharacterNames(account.Id, out charNames)) {
                Debug.Log("Get char names failed.");
                return;
            }
            socket.Send("OpenCharacterList", connection, (string[])charNames, true);
        }
    }

    [NetRPC]
    void CreateCharacter(string playerName, NetConnection connection) {
        if (!InputValidator.LowercaseOnly(playerName)) return;
        LoginServer.Account account;
        if (!loginServer.TryGetAccount(connection, out account)) return;
        if (database.HasKey(playerName)) {
            socket.Send("NameTakenResponse", connection);
            return;
        }

        NetStream playerData = NetStream.New();
        playerData.WriteString(playerName);
        playerData.WriteInt(100);
        playerData.WriteVector3(Vector3.zero);

        ulong key;
        if (!database.TryAdd(playerName, playerData, out key)) return;
        if (!AddCharacterNameKey(key, account.Id)) {
            database.TryDelete(key);
            playerData.Release();
            return;
        }
        playerData.Position = 0;
        viewManager.CreateView(connection, "Player", playerData);
    }

    readonly List<string> cachedList = new List<string>();
    bool TryGetCharacterNames(ulong accountId, out string[] charNames) {
        string charnamesKey = accountId + "_chars";
        NetStream namesStream;
        charNames = null;
        if (!database.TryGet(charnamesKey, out namesStream)) return false;
        cachedList.Clear();
        for (int i = 0; i < 4; i++) {
            try {
                string foundName;
                if (database.TryGetKey(namesStream.ReadULong(), out foundName)) cachedList.Add(foundName);
                else break;
            } catch {
                charNames = cachedList.ToArray();
                return true;
            }
        }
        if (cachedList.Count == 0) return false;
        charNames = cachedList.ToArray();
        return true;
    }

    bool HasCharacter(string playerName, ulong accountId) {
        if (!database.HasKey(playerName)) return false;
        string charnamesKey = accountId + "_chars";
        NetStream namesStream;
        if (!database.TryGet(charnamesKey, out namesStream)) return false;
        for (int i = 0; i < 4; i++) {
            try {
                string foundName;
                if (database.TryGetKey(namesStream.ReadULong(), out foundName) && foundName == playerName) return true;
            } catch {
                return false;
            }
        }
        return false;
    }

    bool AddCharacterNameKey(ulong characterKey, ulong accountId) {
        string charnamesKey = accountId + "_chars";
        NetStream namesStream;
        if (database.TryGet(charnamesKey, out namesStream)) {
            for (int i = 0; i < 4; i++) {
                try {
                    if (namesStream.ReadULong() == 0) {
                        namesStream.Position = namesStream.Position - 64;
                        break;
                    }
                    if (i == 3) return false;
                } catch {
                    if (i == 3) return false; // Already has 4 characters
                    break;
                }
            }
            namesStream.WriteULong(characterKey);
            return database.TryUpdate(charnamesKey, namesStream);
        } else {
            namesStream = NetStream.New();
            namesStream.WriteULong(characterKey);
            ulong namesKey;
            return database.TryAdd(charnamesKey, namesStream, out namesKey);
        }
    }

    [NetRPC]
    void SpawnCharacter(string playerName, NetConnection connection) {
        if (!InputValidator.LowercaseOnly(playerName)) return;
        LoginServer.Account account;
        if (!loginServer.TryGetAccount(connection, out account)) return;
        if (!HasCharacter(playerName, account.Id)) return;
        NetStream playerData;
        database.TryGet(playerName, out playerData);
        viewManager.CreateView(connection, "Player", playerData);
    }

    void ClientDisconnected(NetConnection connection) {
        if (!connection.HasView || connection.View.CurrentRelation != NetView.Relation.Creator || connection.View.PrefabRoot != "Player") return;

        NetStream playerData;
        if (!connection.View.TryGetCreatorData(out playerData)) {
            viewManager.DestroyAuthorizedViews(connection);
            return;
        }

        string playerName = connection.View.GetComponent<PlayerCreator>().PlayerName;

        viewManager.DestroyAuthorizedViews(connection);

        //LoginServer.Account account;
        //if (!loginServer.TryGetAccount(connection, out account)) return;

        //if (!HasCharacter(playerName, account.Id)) return;

        database.TryUpdate(playerName, playerData);
    }

}
