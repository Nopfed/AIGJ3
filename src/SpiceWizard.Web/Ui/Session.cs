using System;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Scene;

namespace SpiceWizard.Web.Ui
{
    public enum PanelKind { None, Title, Help, Plot, Board, Market, Cauldron, Shelf, Mortar, Pantry, Crate, Door, Morning, Celebration, Pause, Options }

    /// <summary>Everything about the current sitting that is not game state: which panel is open, the toast, effects.</summary>
    public sealed class Session
    {
        public PanelKind Panel = PanelKind.Title;
        public int Index;                 // plot or jar the panel is about
        public int Scroll;                // scroll offset of the open panel's content
        public int ContentHeight;         // measured height of that content, one frame stale
        public int SelectedRecipe = 1;
        public bool ExtraPeppercorn;
        public Blend Draft = new Blend();   // the blend being pinched together at the mortar
        public bool HasSave;
        public Settings Settings = new Settings();

        public string Toast;
        public float ToastTime;
        public Color ToastColor;

        public Particles Particles;
        public Action<Point> OnWatered;
        public Action<Point, Color> OnSparkle;
        public Action RequestSleep;
        public Action RequestNewGame;
        public Action RequestContinue;
        public Action RequestQuit;
        public Action SettingsChanged;
        public Action<string> PlaySfx;

        public bool PanelOpen => Panel != PanelKind.None;
        public bool PausesClock => Panel != PanelKind.None;

        public void Open(PanelKind kind, int index = 0)
        {
            Panel = kind;
            Index = index;
            Scroll = 0;
            ContentHeight = 0;
        }

        public void Close() => Panel = PanelKind.None;

        public void Say(ActionResult result)
        {
            Toast = result.Message;
            ToastTime = 3f;
            ToastColor = result.Ok ? Art.Palette.White : Art.Palette.Yellow;
        }

        public void Say(string message, bool good = true)
        {
            Toast = message;
            ToastTime = 3f;
            ToastColor = good ? Art.Palette.White : Art.Palette.Yellow;
        }

        public void Update(float dt)
        {
            if (ToastTime > 0) ToastTime -= dt;
        }
    }
}
