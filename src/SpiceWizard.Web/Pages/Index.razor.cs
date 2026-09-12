using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace SpiceWizard.Web.Pages
{
    public partial class Index
    {
        const string SaveKey = "spicewizard.save";

        Game _game;
        string _savedJson;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (!firstRender) return;

            try { _savedJson = await JsRuntime.InvokeAsync<string>("localStorage.getItem", SaveKey); }
            catch (Exception) { _savedJson = null; }

            await JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public void TickDotNet()
        {
            if (_game == null)
            {
                string demo = Nav.Uri.Contains("demo=master") ? "master" : Nav.Uri.Contains("demo") ? "mid" : null;
                _game = new SpiceWizardGame(_savedJson, Save, ClearSave, PollClicks, demo);
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

        void ClearSave()
        {
            _ = JsRuntime.InvokeVoidAsync("localStorage.removeItem", SaveKey);
        }
    }
}
