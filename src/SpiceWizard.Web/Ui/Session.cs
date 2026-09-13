using System;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Audio;
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
        public int Tab;                   // which tab of a tabbed panel is showing
        public int SelectedRecipe = 1;
        public bool ExtraPeppercorn;
        public Blend Draft = new Blend();   // the blend being pinched together at the mortar
        public bool HasSave;
        /// <summary>The title screen is asking whether to overwrite the saved game.</summary>
        public bool ConfirmNewGame;
        /// <summary>This game's mastery beat the best day on record (shown on the celebration).</summary>
        public bool NewRecord;
        public Settings Settings = new Settings();

        public string Toast;
        public float ToastTime;
        public Color ToastColor;

        public Particles Particles;
        public Action<Point> OnWatered;
        public Action<Point, Color> OnSparkle;
        /// <summary>A sauce of this colour just came out of the cauldron.</summary>
        public Action<Color> OnCooked;
        /// <summary>The wizard acts out what just happened (a <see cref="WizardActor"/> pose name).</summary>
        public Action<string> OnPose;
        public Action RequestSleep;
        public Action RequestNewGame;
        public Action RequestContinue;
        public Action RequestQuit;
        public Action SettingsChanged;
        public Action<string> PlaySfx;
        /// <summary>The wizard is pleased with himself: called after actions that went well.</summary>
        public Action OnAffirm;

        public bool PanelOpen => Panel != PanelKind.None;
        public bool PausesClock => Panel != PanelKind.None;

        public void Open(PanelKind kind, int index = 0)
        {
            if (kind != Panel && kind != PanelKind.Title) PlaySfx?.Invoke(Sfx.Open);
            Panel = kind;
            Index = index;
            ConfirmNewGame = false;
            Scroll = 0;
            ContentHeight = 0;
            Tab = 0;
        }

        /// <summary>Switches tab and resets the scroll so the new page starts at its top.</summary>
        public void ShowTab(int tab)
        {
            Tab = tab;
            Scroll = 0;
            ContentHeight = 0;
        }

        public void Close()
        {
            if (Panel != PanelKind.None && Panel != PanelKind.Title) PlaySfx?.Invoke(Sfx.Close);
            Panel = PanelKind.None;
        }

        /// <summary>Toasts the result; a success plays its own sound (and may earn an "a-ha"), a failure buzzes.</summary>
        public void Say(ActionResult result, string okSfx = null)
        {
            Toast = result.Message;
            ToastTime = 3f;
            ToastColor = result.Ok ? Art.Palette.White : Art.Palette.Yellow;
            if (!result.Ok) PlaySfx?.Invoke(Sfx.Denied);
            else
            {
                if (okSfx != null) PlaySfx?.Invoke(okSfx);
                OnAffirm?.Invoke();
                string pose = WizardActor.PoseFor(okSfx);
                if (pose != null) OnPose?.Invoke(pose);
            }
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
