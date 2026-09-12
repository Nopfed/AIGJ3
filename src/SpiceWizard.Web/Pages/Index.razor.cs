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
                string demo = Nav.Uri.Contains("demo=master") ? "master" : Nav.Uri.Contains("demo=night") ? "night" : Nav.Uri.Contains("demo") ? "mid" : null;
                _game = new SpiceWizardGame(_savedJson, Save, ClearSave, PollClicks, demo, _savedSettings, SaveSettings);
                _game.Run();
            }

            _game.Tick();
        }

        int[] PollClicks()
        {
            try { return ((IJSInProcessRuntime)JsRuntime).Invoke<int[]>("swPollClicks"); }
            catch (Exception) { return null; }
        }

        void Save(string json)
        {
            _ = JsRuntime.InvokeVoidAsync("localStorage.setItem", SaveKey, json);
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
