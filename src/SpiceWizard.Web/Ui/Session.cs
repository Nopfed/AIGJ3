using System;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Scene;

namespace SpiceWizard.Web.Ui
{
    public enum PanelKind { None, Title, Help, Plot, Board, Market, Cauldron, Shelf, Mortar, Pantry, Crate, Door, Morning, Celebration }

    /// <summary>Everything about the current sitting that is not game state: which panel is open, the toast, effects.</summary>
    public sealed class Session
    {
        public PanelKind Panel = PanelKind.Title;
        public int Index;                 // plot or jar the panel is about
        public int SelectedRecipe = 1;
        public bool ExtraPeppercorn;
        public bool HasSave;

        public string Toast;
        public float ToastTime;
        public Color ToastColor;

        public Particles Particles;
        public Action<Point> OnWatered;
        public Action<Point, Color> OnSparkle;
        public Action RequestSleep;
        public Action RequestNewGame;
        public Action RequestContinue;

        public bool PanelOpen => Panel != PanelKind.None;
        public bool PausesClock => Panel != PanelKind.None;

        public void Open(PanelKind kind, int index = 0)
        {
            Panel = kind;
            Index = index;
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
