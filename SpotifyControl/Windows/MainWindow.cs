using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using Newtonsoft.Json.Serialization;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;


namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin plugin;

    private Exception? e;


    private Tab currentTab = Tab.General;
    private enum Tab
    {
        General,
        Authentication,
        Debug
    }

    public MainWindow(Plugin plugin) : base(
        "My Amazing Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose()
    {
        
    }

    public override void Draw()
    {
        this.DrawTabs();
        switch (this.currentTab)
        {
            case Tab.General:
                this.DrawGeneral();
                break;
            case Tab.Authentication:
                this.DrawAuthentication();
                break;
            case Tab.Debug:
                this.DrawDebug();
                break;
            default:
                this.DrawGeneral();
                break;
        }

        ImGui.Spacing();

    }

    private async void DrawAuthentication()
    {
        ImGui.Text("Please register an application first at https://developer.spotify.com/documentation/web-api/concepts/apps");
        ImGui.Text("And set the Redirect URI to http://localhost:5543/callback");
        if (ImGui.Button("Copy URI"))
        {
            ImGui.SetClipboardText("http://localhost:5543/callback");
        }
        if (ImGui.Button("Open site"))
        {
            BrowserUtil.Open(new Uri("https://developer.spotify.com/documentation/web-api/concepts/apps"));
        }
        ImGui.Text("One registered, fill in your clientId below");
        var clientId = this.plugin.Configuration.clientId;
        if (ImGui.InputText(
           "###ClientId_Text",
           ref clientId,
           2000
        ))
        {
            this.plugin.Configuration.clientId = clientId;
            this.plugin.SaveConfig();
        }

        ImGui.Spacing();
        if (ImGui.Button("Authenticate"))
        {
            if (this.plugin.Configuration.clientId == string.Empty)
            {
                ImGui.Text("Please set client id first");
            }
            else
            {
                await this.plugin.StartAuthentication();
            }

        }
    }




    private void DrawGeneral()
    {
        ImGui.Text("General config");
        if (ImGui.Button("Test spotify"))
        {
            if (this.plugin.spotify != null)
            {
                hell();
            }
            else
            {

                ImGui.Text("pain");
            }
        }
        if (ImGui.Button("Clear (DEBUG)"))
        {
            this.plugin.Configuration.clientId = string.Empty;
            this.plugin.Configuration.token = null;
            this.plugin.spotify = null;
        }

    }

    private async void hell()
    {
        if (this.plugin.spotify != null)
        {
            try
            {
                await this.plugin.spotify.Player.PausePlayback();
            }
            catch (Exception e)
            {
                this.e = e;
            }
        }
        else
        {
            ImGui.Text("pain");
        }
    }

    private void DrawTabs()
    {
        if (ImGui.BeginTabBar("spcontrolSettingsTabBar", ImGuiTabBarFlags.NoTooltip))
        {
            if (ImGui.BeginTabItem("General" + "###General_Tab"))
            {
                this.currentTab = Tab.General;
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Authentication" + "###Authentication_Tab"))
            {
                this.currentTab = Tab.Authentication;
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Debug" + "###Debug_Tab"))
            {
                this.currentTab = Tab.Debug;
                ImGui.EndTabItem();
            }
        }
    }
    private void DrawDebug()
    {
        if (this.plugin.Configuration.clientId != null)
        {
            ImGui.Text($"ClientId: {this.plugin.Configuration.clientId}");
        }
        if (this.plugin.Configuration.token != null)
        {
            ImGui.Text($"token: {this.plugin.Configuration.token}");
        }
        if (this.plugin.spotify != null)
        {
            ImGui.Text($"spotify: {this.plugin.spotify}");
        }
        if (this.e != null)
        {
            ImGui.Text($"last error: {this.e.Message}");
        }
    }
}
