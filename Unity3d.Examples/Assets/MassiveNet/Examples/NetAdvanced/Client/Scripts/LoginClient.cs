// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using System.Net;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class LoginClient : MonoBehaviour {

        public List<string> LoginServers = new List<string> { "127.0.0.1:17000" };

        private NetSocket socket;
        private UiManager uiManager;

        private NetConnection loginServer;

        private const string LoginWindowName = "LoginScreen";
        private const string TitleWindowName = "TitleScreen";

        private const string LoginButtonName = "LoginButton";
        private const string RegisterButtonName = "RegisterButton";
        private const string UsernameInputName = "UsernameInput";
        private const string PasswordInputName = "PasswordInput";

        private void Awake() {
            socket = GetComponent<NetSocket>();
            uiManager = GetComponent<UiManager>();

            if (socket == null || uiManager == null) {
                Debug.LogError("Missing required component.");
                return;
            }

            socket.RegisterRpcListener(this);
            socket.Events.OnConnectedToServer += ConnectedToServer;
            socket.Events.OnSocketStart += ConnectToLoginServer;
            socket.Events.OnFailedToConnect += ConnectFailed;
            socket.Events.OnDisconnectedFromServer += DisconnectedFromServer;

            Button.ListenForClick(LoginButtonName, LoginClicked);
            Button.ListenForClick(RegisterButtonName, RegisterClicked);
            TextFieldInput.ListenForSubmit(UsernameInputName, Submit);
            TextFieldInput.ListenForSubmit(PasswordInputName, Submit);
        }

        private int connectIndex = 0;
        private void ConnectToLoginServer() {
            if (LoginServers.Count - 1 < connectIndex) return;
            socket.Connect(LoginServers[connectIndex]);
            connectIndex++;
        }

        private void ConnectFailed(IPEndPoint endpoint) {
            if (!LoginServers.Contains(endpoint.ToString()) || loginServer != null) return;
            ConnectToLoginServer();
        }

        private void DisconnectedFromServer(NetConnection connection) {
            if (loginServer == connection) loginServer = null;
        }

        private void ConnectedToServer(NetConnection connection) {
            if (!LoginServers.Contains(connection.Endpoint.ToString()) || loginServer != null) return;
            ConnectedToLoginServer(connection);
        }

        private void ConnectedToLoginServer(NetConnection connection) {
            connectIndex = 0;
            loginServer = connection;
            OpenLoginWindow();
        }

        private void OpenLoginWindow() {
            uiManager.OpenWindowsExclusively(LoginWindowName, TitleWindowName);
        }

        private void Submit() {
            LoginClicked();
        }

        private void LoginClicked() {
            SendCredentials(false);
        }

        private void RegisterClicked() {
            SendCredentials(true);
        }

        private void SendCredentials(bool register) {
            string username;
            string password;

            if (!TextFieldInput.TryGetText(UsernameInputName, out username)) {
                Debug.LogError("Failed to get text from username field.");
                return;
            }
            if (!TextFieldInput.TryGetText(PasswordInputName, out password)) {
                Debug.LogError("Failed to get text from password field.");
                return;
            }

            username = InputValidator.FmtAllLowercase(username);

            if (!InputValidator.IsValidEmail(username)) {
                Debug.LogError("Username must be a valid email address.");
            }
            else if (!InputValidator.IsValidPassword(password)) {
                Debug.LogError("Invalid password: Length must be between " +
                    InputValidator.MinPasswordLength + " and " + InputValidator.MaxPasswordLength + " characters long.");
            }
            else socket.Send(register ? "CreateAccountRequest" : "LoginRequest", loginServer, username, password);
        }

        [NetRPC]
        private void EmailDuplicateResponse() {
            Debug.Log("Email duplicate");
        }

        [NetRPC]
        private void AlreadyLoggedInResponse() {
            Debug.LogError("Already logged in.");
        }

        [NetRPC]
        private void BadCredentialsResponse() {
            Debug.LogError("Incorrect email or password.");
        }

        [NetRPC]
        private void LoginSuccessResponse(ulong token) {
            Debug.Log("Success! Token: " + token);
        }

    }

}
