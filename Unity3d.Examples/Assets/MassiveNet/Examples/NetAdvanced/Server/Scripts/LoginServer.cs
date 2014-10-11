using System.Collections.Generic;
using System.Net;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class LoginServer : MonoBehaviour {

        public delegate void LoginSuccess(NetConnection connection, Account account);

        public event LoginSuccess OnLoginSuccess;

        public class Account {
            public readonly ulong Id;
            public readonly string Username;
            public readonly string Password;

            public Account(ulong id, string username, string password) {
                Id = id;
                Username = username;
                Password = password;
            }
        }

        private NetSocket socket;
        //private StreamDatabaseServer database;

        private readonly List<Account> accounts = new List<Account>();
        private readonly Dictionary<ulong, Account> sessions = new Dictionary<ulong, Account>();
        private readonly Dictionary<IPEndPoint, ulong> sessionLookup = new Dictionary<IPEndPoint, ulong>();

        private void Awake() {
            socket = GetComponent<NetSocket>();
            //database = GetComponent<StreamDatabaseServer>();

            socket.RegisterRpcListener(this);
            socket.Events.OnClientDisconnected += EndSession;
        }

        public bool SessionValid(NetConnection connection, ulong token) {
            return sessionLookup.ContainsKey(connection.Endpoint) && sessionLookup[connection.Endpoint] == token;
        }

        public bool TryGetAccount(NetConnection connection, out Account account) {
            if (!sessionLookup.ContainsKey(connection.Endpoint)) {
                account = null;
                return false;
            }
            account = sessions[sessionLookup[connection.Endpoint]];
            return true;
        }

        [NetRPC]
        private void LoginRequest(string username, string password, NetConnection connection) {
            if (!InputValidator.IsValidEmail(username) || !InputValidator.IsValidPassword(password)) return;
            if (!InputValidator.LowercaseOnly(username)) return;
            foreach (Account account in accounts) {
                if (account.Username != username) continue;
                if (sessions.ContainsValue(account)) SendAlreadyLoggedIn(connection);
                else if (account.Password == password) SendLoginSuccess(account, connection);
                else SendBadCredentials(connection);
                return;
            }
            SendBadCredentials(connection);
        }

        [NetRPC]
        private void CreateAccountRequest(string username, string password, NetConnection connection) {
            if (!InputValidator.IsValidEmail(username) || !InputValidator.IsValidPassword(password)) return;
            if (!InputValidator.LowercaseOnly(username)) return;
            foreach (Account account in accounts) {
                if (account.Username != username) continue;
                SendEmailDuplicate(connection);
                return;
            }
            ulong randId = NetMath.RandomUlong();
            var newAcc = new Account(randId, username, password);
            accounts.Add(newAcc);
            SendLoginSuccess(newAcc, connection);
        }

        private ulong CreateSession(Account account, NetConnection connection) {
            ulong sessionToken = NetMath.RandomUlong();
            sessions.Add(sessionToken, account);
            sessionLookup.Add(connection.Endpoint, sessionToken);
            return sessionToken;
        }

        private void EndSession(NetConnection connection) {
            if (!sessionLookup.ContainsKey(connection.Endpoint)) return;
            ulong token = sessionLookup[connection.Endpoint];
            sessions.Remove(token);
            sessionLookup.Remove(connection.Endpoint);
        }

        private void SendEmailDuplicate(NetConnection connection) {
            socket.Send("EmailDuplicateResponse", connection);
        }

        private void SendLoginSuccess(Account account, NetConnection connection) {
            ulong sessionToken = CreateSession(account, connection);
            socket.Send("LoginSuccessResponse", connection, sessionToken);
            if (OnLoginSuccess != null) OnLoginSuccess(connection, account);
        }

        private void SendAlreadyLoggedIn(NetConnection connection) {
            socket.Send("AlreadyLoggedInResponse", connection);
        }

        private void SendBadCredentials(NetConnection connection) {
            socket.Send("BadCredentialsResponse", connection);
        }

    }

}