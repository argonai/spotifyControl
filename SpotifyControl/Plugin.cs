using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using SamplePlugin.Windows;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using SpotifyAPI.Web;
using Dalamud.Game.Gui;
using System.Threading.Tasks;
using SpotifyAPI.Web.Auth;
using static SpotifyAPI.Web.Scopes;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Spotify control";
        private const string CommandName = "/spconfig";
        private const string ControlCommand = "/spcontrol";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }

        public WindowSystem WindowSystem = new("Spotify control");

        public SpotifyClient? spotify;

        private static readonly EmbedIOAuthServer Server = new(new Uri("http://localhost:5543/callback"), 5543);

        private MainWindow MainWindow { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            MainWindow = new MainWindow(this);

            WindowSystem.AddWindow(MainWindow);
            // Hier nieuwe commands toevoegen
            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Configure SpotifyControl"
            });
            this.CommandManager.AddHandler(ControlCommand, new CommandInfo(OnControl)
            {
                HelpMessage = "Control spotify"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }



        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();

            MainWindow.Dispose();

            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            MainWindow.IsOpen = true;
            // in response to the slash command, just display our main ui
            
        }
        private async void OnControl(String command, string args)
        {
            // TODO: args voor control pane en command control, unified control klasse om te besturen, setting om image te tonen in pane
            // CLEAR ALL DATA BUTTON VOOR TESTING
            if(spotify!=null){
                switch (args)
            {
                case "next":
                    await spotify.Player.SkipNext();
                    break;
                case "play":
                    await spotify.Player.ResumePlayback();
                    break;
                case "pause":
                    await spotify.Player.PausePlayback();
                    break;
                case "playback":
                    if(await spotify.Player.GetCurrentPlayback()!=null){
                        await spotify.Player.ResumePlayback();
                    } else{
                        await spotify.Player.PausePlayback();
                    }
                    break;
                default:
                    MainWindow.IsOpen = true;
                    break;
            }
            }
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            MainWindow.IsOpen = true;
        }
        public void SaveConfig()
        {
            PluginInterface.SavePluginConfig(this.Configuration);
        }

        public void startClient()
        {
            // TODO: starts spotify client and checks for PKCSE token
            if (this.Configuration.token == null)
            {
                throw new NullReferenceException("Please set clientId first");
            }
            var authenticator = new PKCEAuthenticator(this.Configuration.clientId!, this.Configuration.token!);
            authenticator.TokenRefreshed += (sender, token) => this.Configuration.token = token;

            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);

            this.spotify = new SpotifyClient(config);
            Server.Stop();
            Server.Dispose();
        }

        public async Task StartAuthentication()
        {

            var (verifier, challenge) = PKCEUtil.GenerateCodes();
            
                await Server.Start();
                Server.AuthorizationCodeReceived += async (sender, response) =>
                {
                    var token = await new OAuthClient().RequestToken(
                    new PKCETokenRequest(this.Configuration.clientId!, response.Code, Server.BaseUri, verifier)
                  );
                    this.Configuration.token = token;
                    // await Start();
                    this.startClient();
                };

                var request = new LoginRequest(Server.BaseUri, this.Configuration.clientId!, LoginRequest.ResponseType.Code)
                {
                    CodeChallenge = challenge,
                    CodeChallengeMethod = "S256",
                    Scope = new List<string> { UserReadPrivate, PlaylistReadPrivate, PlaylistReadCollaborative, AppRemoteControl, UserModifyPlaybackState, UserReadPlaybackState  }
                };

                var uri = request.ToUri();
                try
                {
                    BrowserUtil.Open(uri);
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to open URL, manually open: {0}", uri);
                }
            }

        }
    }

