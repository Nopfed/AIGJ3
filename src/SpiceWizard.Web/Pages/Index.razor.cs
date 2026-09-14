using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace SpiceWizard.Web.Pages
{
    public partial class Index
    {
        const string SaveKey = "spicewizard.save";
        const string SettingsKey = "spicewizard.settings";

        Game _game;
        string _savedJson;
        string _savedSettings;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (!firstRender) return;

            try { _savedJson = await JsRuntime.InvokeAsync<string>("localStorage.getItem", SaveKey); }
            catch (Exception) { _savedJson = null; }
            try { _savedSettings = await JsRuntime.InvokeAsync<string>("localStorage.getItem", SettingsKey); }
            catch (Exception) { _savedSettings = null; }

            await JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public void TickDotNet()
        {
            if (_game == null)
            {
                string demo = null;
                foreach (var flag in new[] { "master", "night", "morning", "evening", "rain", "windy", "chores", "rush", "levelup" })
                    if (Nav.Uri.Contains("demo=" + flag)) demo = flag;
                if (demo == null && Nav.Uri.Contains("demo")) demo = "mid";
                var panel = System.Text.RegularExpressions.Regex.Match(Nav.Uri, "panel=([A-Za-z]+)");
                _game = new SpiceWizardGame(_savedJson, Save, ClearSave, PollClicks, demo, _savedSettings, SaveSettings, panel.Success ? panel.Groups[1].Value : null,
                    int.TryParse(System.Text.RegularExpressions.Regex.Match(Nav.Uri, "index=([0-9]+)").Groups[1].Value, out var idx) ? idx : 0, Fullscreen);
                _game.Run();
            }

            _game.Tick();
        }

        /// <summary>The page is being hidden or closed: write the save before the browser lets go of us.</summary>
        [JSInvokable]
        public void SaveNow() => (_game as SpiceWizardGame)?.SaveNow();

        void Fullscreen()
        {
            try { ((IJSInProcessRuntime)JsRuntime).InvokeVoid("swFullscreen"); }
            catch (Exception) { /* not every browser allows it */ }
        }

        int[] PollClicks()
        {
            try { return ((IJSInProcessRuntime)JsRuntime).Invoke<int[]>("swPollClicks"); }
            catch (Exception) { return null; }
        }

        void Save(string json)
        {
            // Synchronous so a save on pagehide lands before the page is gone.
            try { ((IJSInProcessRuntime)JsRuntime).InvokeVoid("localStorage.setItem", SaveKey, json); }
            catch (Exception) { _ = JsRuntime.InvokeVoidAsync("localStorage.setItem", SaveKey, json); }
        }

        void SaveSettings(string text)
        {
            _ = JsRuntime.InvokeVoidAsync("localStorage.setItem", SettingsKey, text);
        }

        void ClearSave()
        {
            _ = JsRuntime.InvokeVoidAsync("localStorage.removeItem", SaveKey);
        }
    }
}
