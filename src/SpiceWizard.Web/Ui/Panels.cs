using System;
using System.Linq;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;
using SpiceWizard.Web.Audio;
using SpiceWizard.Web.Scene;

namespace SpiceWizard.Web.Ui
{
    /// <summary>Every station panel, drawn immediate-mode from the game state.</summary>
    public static class Panels
    {
        static readonly Rectangle Frame = new Rectangle(16, 18, 352, 186);
        const int Left = 22;
        const int Top = 34;
        const int Row = 13;
        const int MaxChars = 56;
        /// <summary>Rightmost pixel body text may reach: the frame's bevel starts just past it.</summary>
        static int RightEdge => Frame.Right - 6;

        public static void Draw(Ui ui, GameState s, Session ss)
        {
            switch (ss.Panel)
            {
                case PanelKind.Plot: Plot(ui, s, ss); break;
                case PanelKind.Board: Board(ui, s, ss); break;
                case PanelKind.Market: Market(ui, s, ss); break;
                case PanelKind.Cauldron: Cauldron(ui, s, ss); break;
                case PanelKind.Shelf: Shelf(ui, s, ss); break;
                case PanelKind.Mortar: Mortar(ui, s, ss); break;
                case PanelKind.Pantry: Pantry(ui, s, ss); break;
                case PanelKind.Crate: Crate(ui, s, ss); break;
                case PanelKind.Door: Door(ui, s, ss); break;
                case PanelKind.Morning: Morning(ui, s, ss); break;
                case PanelKind.Help: Help(ui, s, ss); break;
            }
        }

        static bool Open(Ui ui, Session ss, string title)
        {
            if (ui.Panel(Frame, title)) { ss.Close(); return false; }
            return true;
        }

        /// <summary>Small status at the right end of the title bar, with an optional icon.</summary>
        static void Header(Ui ui, string icon, string text)
        {
            int w = PixelFont.Measure(text) + (icon != null ? 10 : 0);
            int x = Frame.Right - 22 - w;
            if (icon != null) ui.IconLabel(x, Frame.Y + 4, icon, Color.White, text, Palette.Cream);
            else ui.C.Text(text, x, Frame.Y + 5, Palette.Cream);
        }

        static void SpiceHeader(Ui ui, GameState s) => Header(ui, "ic_flame", "Spice " + s.Spice.Current + "/" + s.Spice.Max);
        static void PeppercornHeader(Ui ui, GameState s) => Header(ui, "ic_peppercorn", s.Peppercorns + " peppercorns");

        /// <summary>The panel body below the title bar (and above the header text, if any), leaving room for a
        /// fixed footer (buttons or a caption) that sits outside the scrollable area.</summary>
        static Rectangle Content(int footerHeight = 0) =>
            new Rectangle(Frame.X + 6, Top, Frame.Width - 12, Frame.Bottom - 6 - footerHeight - Top);

        /// <summary>Wraps a panel's body in a clipped, scrollable region. <paramref name="draw"/> receives the
        /// starting y (already shifted by the current scroll) and must return the y just past the last content
        /// drawn, so the true content height can be measured and used to size the scrollbar and clamp scrolling.</summary>
        static void Scroll(Ui ui, Session ss, Rectangle area, Func<int, int> draw)
        {
            int scroll = ui.BeginScroll(area, ref ss.Scroll, ss.ContentHeight);
            int startY = area.Y - scroll;
            int endY = draw(startY);
            ss.ContentHeight = endY - area.Y + scroll;
            ui.EndScroll(area, scroll, ss.ContentHeight);
        }

        /// <summary>Icon and tint for an ingredient wherever a recipe or blend lists one.</summary>
        static (string icon, Color tint) IngredientArt(Ingredient ing) => ing.Kind switch
        {
            IngredientKind.Pepper => (ItemArt.PepperIcon((PepperSpecies)ing.Index), Color.White),
            IngredientKind.Powder => ("ic_powder", ItemArt.SpeciesColor((PepperSpecies)ing.Index)),
            IngredientKind.Mash => ("ic_mash", ItemArt.SpeciesColor((PepperSpecies)ing.Index)),
            IngredientKind.Spice => ("ic_pouch", ItemArt.SpiceColor((Spice)ing.Index)),
            _ => ("ic_peppercorn", Color.White),
        };

        static string Cost(int spice) => "-" + spice;

        // ---- Garden plot ------------------------------------------------------------

        static void Plot(Ui ui, GameState s, Session ss)
        {
            int i = ss.Index;
            var plot = s.Garden.Plots[i];
            if (!Open(ui, ss, "Garden plot " + (i + 1))) return;
            var pos = Layout.PlotPos(i);

            if (plot.IsEmpty)
            {
                Scroll(ui, ss, Content(), top =>
                {
                    ui.Heading(Left, top, "Bare soil. Plant a seed:");
                    int y = top;
                    for (int k = 0; k < Species.All.Length; k++)
                    {
                        var info = Species.All[k];
                        y = top + 16 + k * (Row + 12);
                        int seeds = s.Inventory.Seed(info.Species);
                        bool unlocked = info.UnlockLevel <= s.Level;
                        ui.IconLabel(Left, y + 2, ItemArt.PepperIcon(info.Species), unlocked ? Color.White : Palette.Grey, info.Name + " pepper", unlocked ? Palette.Outline : Palette.Grey);
                        ui.IconLabel(Left + 110, y + 2, "ic_seed", unlocked ? ItemArt.SpeciesColor(info.Species) : Palette.Grey, "x" + seeds, seeds > 0 ? Palette.Outline : Palette.Grey);
                        ui.Label(Left, y + 14, unlocked ? info.CareHint : "Seeds unlock at level " + info.UnlockLevel, Palette.Grey);
                        string tip = info.GrowthPoints + " growth to ripen, yields " + info.Yield + " peppers";
                        if (ui.Button(new Rectangle(Left + 160, y - 1, 56, 13), "Plant", seeds > 0, tip))
                        {
                            var r = Actions.PlantSeed(s, i, info.Species);
                            ss.Say(r, Sfx.Plant);
                            if (r.Ok) ss.OnSparkle?.Invoke(new Point(pos.X + 12, pos.Y + 4), Palette.LightGreen);
                        }
                    }
                    return y + Row + 12;
                });
                return;
            }

            var plant = plot.Plant;
            var sp = plant.Info;
            Scroll(ui, ss, Content(), top =>
            {
                string sprite = plant.Stage switch
                {
                    PlantStage.Seed => "seed",
                    PlantStage.Sprout => "sprout",
                    PlantStage.Budding => "bud",
                    _ => ItemArt.MatureSprite(plant.Species),
                };
                ui.C.Rect(Left, top, 32, 32, Palette.Soil);
                ui.C.Border(new Rectangle(Left, top, 32, 32), Palette.DeepSoil);
                ui.C.Sprite(sprite, Left + 8, top + 8);
                if (plant.IsMature && plant.Species == PepperSpecies.Ghost) ui.C.Sprite("ghost", Left + 11, top + 4);

                int tx = Left + 40;
                ui.Heading(tx, top, sp.Name + " pepper");
                ui.Label(tx + 96, top, plant.Stage.ToString(), Palette.Grey);
                ui.Bar(new Rectangle(tx, top + 11, 120, 7), plant.Points / (float)sp.GrowthPoints, Palette.LightGreen);
                ui.Label(tx + 126, top + 11, plant.Points + "/" + sp.GrowthPoints + " growth");
                ui.Label(tx, top + 22, plant.Mood(), Palette.Purple);
                ui.Label(tx, top + 32, sp.CareHint, Palette.Grey);

                int by = top + 48;
                if (!plant.IsMature)
                {
                    ui.Flag(Left, by, plant.WateredToday, "Watered today");
                    ui.Flag(Left + 96, by, plant.WateredYesterday, "Watered yesterday");
                    ui.Flag(Left + 216, by, plant.PepTalkedToday, "Pep talk today");
                    by += 14;
                    int g = plant.GrowthTonight();
                    ui.Label(Left, by, "Tonight: +" + g + " growth", g > 0 ? Palette.Green : Palette.DarkRed);
                    by += 18;
                }

                bool canWater = !plant.IsMature && !plant.WateredToday;
                if (ui.Button(new Rectangle(Left, by, 86, 16), "Water " + s.Garden.BucketWater + "/" + Garden.BucketCapacity, canWater && s.Garden.BucketWater > 0,
                    s.Garden.BucketWater == 0 ? "Bucket is empty: click the well" : "Water the plant", icon: "ic_water"))
                {
                    var r = Actions.Water(s, i);
                    ss.Say(r);
                    if (r.Ok) ss.OnWatered?.Invoke(new Point(pos.X + 12, pos.Y + 4));
                }
                if (ui.Button(new Rectangle(Left + 92, by, 86, 16), "Pep talk", !plant.IsMature && !plant.PepTalkedToday && s.Spice.CanSpend(Balance.PepTalkSpiceCost),
                    "Costs " + Balance.PepTalkSpiceCost + " spice. Every pepper reacts differently", badge: Cost(Balance.PepTalkSpiceCost)))
                {
                    var r = Actions.PepTalk(s, i);
                    ss.Say(r);
                    if (r.Ok) { ss.OnSparkle?.Invoke(new Point(pos.X + 12, pos.Y), Palette.Pink); ss.OnPose?.Invoke(WizardActor.PoseCheer); }
                }
                if (ui.Button(new Rectangle(Left + 184, by, 86, 16), "Harvest", plant.IsMature, "Yields " + sp.Yield + " peppers", icon: ItemArt.PepperIcon(plant.Species)))
                {
                    var r = Actions.Harvest(s, i);
                    ss.Say(r, Sfx.Harvest);
                    if (r.Ok) { ss.OnSparkle?.Invoke(new Point(pos.X + 12, pos.Y), Palette.Yellow); ss.Close(); }
                }
                by += 20;
                HastenButton(ui, ss, new Rectangle(Left, by, 178, 16), s, !plant.IsMature, "Grows " + Balance.HastenNights + " nights at once",
                    () => Actions.HastenPlant(s, i), new Point(pos.X + 12, pos.Y));
                return by + 16;
            });
        }

        /// <summary>The hasten spell button: one shared look, one shared set of reasons it is greyed out.</summary>
        static void HastenButton(Ui ui, Session ss, Rectangle rect, GameState s, bool targetOk, string tip, Func<ActionResult> cast, Point sparkleAt)
        {
            string blocker = Actions.HastenBlocker(s);
            bool enabled = targetOk && blocker.Length == 0;
            bool spent = s.HastensLeft == 0;
            string label = spent ? "Hasten (used today)" : s.HastenCasts > 1 ? "Hasten spell (" + s.HastensLeft + " left)" : "Hasten spell";
            string perDay = s.HastenCasts == 1 ? "Once a day" : s.HastenCasts + " casts a day";
            if (!ui.Button(rect, label, enabled, blocker.Length > 0 ? blocker : perDay + ", costs " + Balance.HastenSpiceCost + " spice. " + tip,
                badge: spent ? null : Cost(Balance.HastenSpiceCost))) return;
            var r = cast();
            ss.Say(r, Sfx.Chime);
            if (r.Ok) ss.OnSparkle?.Invoke(sparkleAt, Palette.Purple);
        }

        // ---- Notice board -------------------------------------------------------------

        static void Board(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Notice board - week " + s.Clock.Week)) return;
            Header(ui, null, "Day " + s.Clock.DayOfWeek + " of 7");
            var q = s.Quota;
            Scroll(ui, ss, Content(), top =>
            {
                int y = top;
                ui.Heading(Left, y, "The town council requests, by the end of day " + s.Clock.Week * 7 + ":");
                y += 14;
                if (q == null) { ui.Label(Left, y, "Nothing posted."); return y + Row; }
                for (int i = 0; i < q.Lines.Count; i++)
                {
                    var line = q.Lines[i];
                    var r = RecipeBook.Get(line.RecipeId);
                    var color = line.IsMet ? Palette.Green : Palette.Outline;
                    ui.IconLabel(Left + 6, y, ItemArt.SauceIcon(r), ItemArt.SauceColor(r.Id), line.Required + "x " + r.Name, color);
                    ui.Label(Left + 150, y + 1, line.Sold + " / " + line.Required, line.IsMet ? Palette.Green : Palette.DarkRed);
                    if (line.IsMet) ui.C.Sprite("ic_check", Left + 190, y);
                    y += Row + 2;
                }
                y += 8;
                ui.Label(Left, y + 1, "Reward:", Palette.Grey);
                ui.IconLabel(Left + 48, y, "ic_peppercorn", Color.White, Balance.QuotaBonusPeppercorns(q.Week) + " peppercorns", Palette.Outline);
                ui.IconLabel(Left + 152, y, "ic_hat", Color.White, Balance.QuotaBonusXp(q.Week) + " fame", Palette.Outline);
                ui.IconLabel(Left + 208, y, "ic_pouch", Palette.Tan, "a rare spice", Palette.Outline);
                y += 14;
                ui.Label(Left, y, "Sauces on the list earn an extra star when sold.", Palette.Grey); y += 10;
                ui.Label(Left, y, "The same sauce " + RepeatWord(s) + " in three days tires the town.", Palette.Grey); y += 12;
                if (q.CravedType is SauceType craved)
                {
                    ui.IconLabel(Left, y, craved == SauceType.Hot ? "ic_hot" : "ic_curry", craved == SauceType.Hot ? Palette.Red : Palette.Yellow,
                        Quota.CravingText(craved), Palette.Orange);
                    y += 10;
                    ui.Label(Left + 10, y, "Every bottle of that kind earns an extra star.", Palette.Orange); y += 12;
                }
                if (q.IsMet) { y += 4; ui.IconLabel(Left, y, "ic_check", Color.White, "Quota met! The bonus arrives at the start of next week.", Palette.Green); y += 12; }
                y += 4;
                y = RushBlock(ui, s, y);
                return y;
            });
        }

        /// <summary>The rush order pinned under the quota: what, how many, when, and how it is going.</summary>
        static int RushBlock(Ui ui, GameState s, int y)
        {
            var rush = s.Rush;
            if (rush == null)
            {
                ui.IconLabel(Left, y, "ic_envelope", Palette.Grey, "No rush orders today. Someone may run up with one at dawn.", Palette.Grey);
                return y + Row;
            }
            var r = RecipeBook.Get(rush.RecipeId);
            bool dueTonight = rush.DaysLeft(s.Clock.Day) <= 0;
            ui.IconLabel(Left, y, "ic_envelope", Color.White, "Rush order, " + rush.DueText(s.Clock.Day) + ":", dueTonight ? Palette.DarkRed : Palette.Purple);
            y += Row;
            ui.IconLabel(Left + 6, y, ItemArt.SauceIcon(r), ItemArt.SauceColor(r.Id), rush.Count + "x " + r.Name, rush.IsMet ? Palette.Green : Palette.Outline);
            ui.Label(Left + 150, y + 1, rush.Delivered + " / " + rush.Count, rush.IsMet ? Palette.Green : Palette.DarkRed);
            if (rush.IsMet) ui.C.Sprite("ic_check", Left + 190, y);
            y += Row + 2;
            return ui.Paragraph(Left, y, MaxChars, "Those bottles pay " + rush.PayMultiplier + "x; filling the order earns " + Balance.RushXp + " fame. No penalty if it lapses.", Palette.Grey);
        }

        /// <summary>"twice" or "three times": how many repeats the town sits through before it tires of a sauce.</summary>
        static string RepeatWord(GameState s) => Balance.BoredomThresholdAt(s.Level) switch { 2 => "twice", 3 => "three times", int n => n + " times" };

        // ---- Merchant --------------------------------------------------------------------

        static void Market(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Travelling merchant")) return;
            PeppercornHeader(ui, s);
            Scroll(ui, ss, Content(), top =>
            {
                int y1 = top;
                ui.Heading(Left, y1, "Seeds");
                ui.Label(Left + 120, y1, "owned", Palette.Grey);
                y1 += 12;
                for (int k = 0; k < Species.All.Length; k++, y1 += Row)
                {
                    var info = Species.All[k];
                    bool unlocked = info.UnlockLevel <= s.Level;
                    ui.IconLabel(Left, y1, ItemArt.PepperIcon(info.Species), unlocked ? Color.White : Palette.Grey, info.Name, unlocked ? Palette.Outline : Palette.Grey);
                    ui.Label(Left + 56, y1 + 1, info.SeedCost + "pc", Palette.Outline);
                    if (!unlocked) ui.IconLabel(Left + 84, y1, "ic_lock", Color.White, "Lv" + info.UnlockLevel, Palette.Grey);
                    else if (ui.Button(new Rectangle(Left + 84, y1 - 1, 30, 11), "Buy", s.Peppercorns >= info.SeedCost, info.CareHint))
                        ss.Say(Actions.BuySeed(s, info.Species), Sfx.Coin);
                    ui.Label(Left + 120, y1 + 1, "x" + s.Inventory.Seed(info.Species), Palette.Grey);
                }

                int sx = Left + 156;
                int y2 = top;
                ui.Heading(sx, y2, "Spices");
                ui.Label(sx + 146, y2, "owned", Palette.Grey);
                y2 += 12;
                for (int k = 0; k < Inventory.SpiceCount; k++, y2 += Row)
                {
                    var spice = (Spice)k;
                    bool unlocked = SpiceInfo.UnlockLevel(spice) <= s.Level;
                    ui.IconLabel(sx, y2, "ic_pouch", unlocked ? ItemArt.SpiceColor(spice) : Palette.Grey, SpiceInfo.Name(spice), unlocked ? Palette.Outline : Palette.Grey);
                    ui.Label(sx + 84, y2 + 1, SpiceInfo.Price(spice) + "pc", Palette.Outline);
                    if (!unlocked) ui.IconLabel(sx + 110, y2, "ic_lock", Color.White, "Lv" + SpiceInfo.UnlockLevel(spice), Palette.Grey);
                    else if (ui.Button(new Rectangle(sx + 110, y2 - 1, 30, 11), "Buy", s.Peppercorns >= SpiceInfo.Price(spice)))
                        ss.Say(Actions.BuySpice(s, spice), Sfx.Coin);
                    ui.Label(sx + 146, y2 + 1, "x" + s.Inventory.SpiceOf(spice), Palette.Grey);
                }

                int y = Math.Max(y1, y2) + 8;
                y = ui.Paragraph(Left, y, 24, "\"Peppercorns are money AND an ingredient. Grind your own pepper powder at the mortar.\"", Palette.Grey);
                return y;
            });
        }

        // ---- Cauldron ----------------------------------------------------------------------

        static void Cauldron(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Cauldron")) return;
            SpiceHeader(ui, s);
            Scroll(ui, ss, Content(), top =>
            {
                int y1 = top;
                ui.Heading(Left, y1, "Recipes");
                ui.Label(Left + 68, y1, "sells for, pc", Palette.Grey);
                y1 += 12;
                for (int k = 0; k < RecipeBook.All.Length; k++, y1 += 12)
                {
                    var r = RecipeBook.All[k];
                    bool unlocked = r.UnlockLevel <= s.Level;
                    var rowRect = new Rectangle(Left, y1 - 1, 128, 11); // stops short of the Cook button so the row does not swallow its click
                    bool selected = ss.SelectedRecipe == r.Id;
                    if (selected) ui.C.Rect(rowRect, Palette.Yellow * 0.5f);
                    else if (ui.Hot(rowRect)) ui.C.Rect(rowRect, Palette.Yellow * 0.2f);
                    ui.IconLabel(Left + 2, y1, ItemArt.SauceIcon(r), unlocked ? ItemArt.SauceColor(r.Id) : Palette.Grey, r.Name, unlocked ? Palette.Outline : Palette.Grey);
                    // Right-aligned against the row so three-digit prices and long names both fit.
                    string price = r.BaseValue.ToString();
                    if (unlocked) ui.Label(rowRect.Right - PixelFont.Measure(price), y1 + 1, price, Palette.Outline);
                    else ui.IconLabel(Left + 104, y1, "ic_lock", Color.White, "Lv" + r.UnlockLevel, Palette.Grey);
                    if (ui.Take(rowRect)) ss.SelectedRecipe = r.Id;
                    string blocker = Actions.CookBlocker(s, r, ss.ExtraPeppercorn);
                    if (unlocked && ui.Button(new Rectangle(Left + 130, y1 - 1, 34, 11), "Cook", blocker.Length == 0, blocker.Length == 0 ? "Costs " + Balance.CookSpiceCost + " spice" : blocker))
                    {
                        var res = Actions.Cook(s, r.Id, ss.ExtraPeppercorn);
                        ss.Say(res, Sfx.Cook);
                        if (res.Ok) ss.OnCooked?.Invoke(ItemArt.SauceColor(r.Id));
                    }
                }

                var sel = RecipeBook.Get(ss.SelectedRecipe);
                int dx = Left + 172, dy = top;
                ui.Heading(dx, dy, sel.Name);
                ui.Label(dx, dy + 10, (sel.Type == SauceType.Hot ? "Hot sauce" : "Curry sauce") + ", tier " + sel.Tier + ".", Palette.Grey);
                ui.IconLabel(dx + 110, dy + 9, "ic_flame", Color.White, Cost(Balance.CookSpiceCost), Palette.DarkRed);
                ui.Label(dx, dy + 22, "Needs (you have):");
                for (int k = 0; k < sel.Ingredients.Length; k++)
                {
                    var ing = sel.Ingredients[k];
                    int have = Actions.Have(s, ing);
                    bool ok = have >= ing.Count;
                    var (icon, tint) = IngredientArt(ing);
                    ui.IconLabel(dx + 4, dy + 32 + k * 10, icon, ok ? tint : Palette.Grey, ing.Count + "x " + ing.Name + " (" + have + ")", ok ? Palette.Green : Palette.DarkRed);
                }
                int ey = dy + 32 + sel.Ingredients.Length * 10 + 6;
                ui.Checkbox(dx, ey, "Extra peppercorn: +1 star", ref ss.ExtraPeppercorn);
                int y2 = ui.Paragraph(dx, ey + 14, 27, "Aged mash also adds a star. A recipe you have never cooked loses one.", Palette.Grey);

                return Math.Max(y1, y2);
            });
        }

        // ---- Fermenting jars ------------------------------------------------------------------

        static void Shelf(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Fermenting shelf")) return;
            Scroll(ui, ss, Content(), top =>
            {
                ui.Label(Left, top, "Pack " + Jar.PeppersPerJar + " peppers of one kind: mash in " + Jar.NightsToFerment + " nights, aged in " + Balance.NightsToAgeAt(s.Level) + ".", Palette.Grey);
                int y = top + 14;
                for (int j = 0; j < FermentShelf.MaxJars; j++, y += 34)
                {
                    var jar = s.Shelf.Jars[j];
                    bool unlocked = j < s.UnlockedJars;
                    if (!unlocked)
                    {
                        ui.C.Sprite("jar_lock", Left, y);
                        ui.Label(Left + 16, y + 3, "Unlocks at level " + FermentShelf.JarUnlockLevels[j], Palette.Grey);
                        continue;
                    }
                    if (!jar.IsEmpty)
                    {
                        var color = ItemArt.SpeciesColor(jar.Species.Value);
                        ui.C.Sprite("jar_fill", Left, y, jar.IsReady ? color : Color.Lerp(color, Palette.Grey, 0.5f));
                    }
                    ui.C.Sprite("jar", Left, y);
                    ui.Label(Left + 16, y + 3, "Jar " + (j + 1) + ": " + jar.Status(), jar.IsReady ? Palette.Green : Palette.Outline);
                    if (jar.IsEmpty)
                    {
                        for (int k = 0; k < Species.All.Length; k++)
                        {
                            var sp = (PepperSpecies)k;
                            int have = s.Inventory.Pepper(sp);
                            if (ui.Button(new Rectangle(Left + 16 + k * 66, y + 15, 62, 12), Species.NameOf(sp) + " " + have, have >= Jar.PeppersPerJar,
                                "Pack " + Jar.PeppersPerJar + " " + Species.NameOf(sp) + " peppers", icon: ItemArt.PepperIcon(sp)))
                                ss.Say(Actions.FillJar(s, j, sp), Sfx.Jar);
                        }
                    }
                    else if (jar.IsReady)
                    {
                        if (ui.Button(new Rectangle(Left + 16, y + 14, 96, 14), "Collect mash", true, jar.IsAged ? "Aged mash: +1 star when cooked" : "Leave it " + (jar.AgeNights - jar.Nights) + " more nights to age",
                            icon: "ic_mash", tint: ItemArt.SpeciesColor(jar.Species.Value)))
                            ss.Say(Actions.EmptyJar(s, j), Sfx.Jar);
                    }
                    else ui.Label(Left + 16, y + 17, "Bubbling away...", Palette.Grey);
                    if (!jar.IsEmpty && !jar.IsAged)
                        HastenButton(ui, ss, new Rectangle(Left + 120, y + 14, 110, 14), s, true, "Ferments " + Balance.HastenNights + " nights at once",
                            () => Actions.HastenJar(s, j), new Point(Layout.Shelf.X + 8 + j * 8, Layout.Shelf.Y));
                }
                return y;
            });
        }

        // ---- Mortar: grinding and blending -----------------------------------------------------

        static void Mortar(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Mortar and pestle")) return;
            SpiceHeader(ui, s);
            var draft = ss.Draft;
            bool unlocked = s.Level >= Balance.BlendUnlockLevel;
            bool room = draft.Pinches < Balance.MaxBlendPinches;
            const int Pitch = 11;
            int rx = Left + 172;
            var area = Content();

            Scroll(ui, ss, area, top =>
            {
                ui.Heading(Left, top, "Ingredients");
                int y = top + 12;
                for (int k = 0; k < Species.All.Length; k++, y += Pitch)
                {
                    var sp = (PepperSpecies)k;
                    var ing = Ingredient.Powder(sp);
                    ui.IconLabel(Left, y, ItemArt.PepperIcon(sp), Color.White, Species.NameOf(sp) + " " + s.Inventory.Pepper(sp), Palette.Outline);
                    ui.IconLabel(Left + 66, y, "ic_powder", ItemArt.SpeciesColor(sp), "x" + s.Inventory.PowderOf(sp), Palette.Outline);
                    if (ui.Button(new Rectangle(Left + 92, y - 1, 58, 11), "Grind", s.Inventory.Pepper(sp) > 0 && s.Spice.CanSpend(Balance.GrindSpiceCost),
                        "One pepper to one powder for " + Balance.GrindSpiceCost + " spice", badge: Cost(Balance.GrindSpiceCost)))
                        ss.Say(Actions.Grind(s, sp), Sfx.Grind);
                    PinchButton(ui, ss, s, draft, ing, Left + 154, y, unlocked && room);
                }
                y += 2;
                for (int k = 0; k < Inventory.SpiceCount; k++, y += Pitch)
                {
                    var spice = (Spice)k;
                    var ing = Ingredient.Of(spice);
                    int have = s.Inventory.SpiceOf(spice);
                    ui.IconLabel(Left, y, "ic_pouch", ItemArt.SpiceColor(spice), SpiceInfo.Name(spice) + " x" + have, have > 0 ? Palette.Outline : Palette.Grey);
                    PinchButton(ui, ss, s, draft, ing, Left + 154, y, unlocked && room);
                }
                ui.IconLabel(Left, y, "ic_peppercorn", Color.White, "Peppercorn x" + s.Peppercorns, Palette.Outline);
                PinchButton(ui, ss, s, draft, Ingredient.Peppercorns(1), Left + 154, y, unlocked && room);
                int y1 = y + Pitch;

                ui.Heading(rx, top, "Your blend");
                if (unlocked) ui.C.TextRight(draft.Pinches + "/" + Balance.MaxBlendPinches + " pinches", RightEdge, top, Palette.Grey);
                int y2;
                if (!unlocked)
                {
                    y2 = ui.Paragraph(rx, top + 14, 28, "Blending unlocks at level " + Balance.BlendUnlockLevel + ". Until then the mortar only grinds.", Palette.Grey);
                }
                else
                {
                    int py = top + 12;
                    if (draft.Pinches == 0) py = ui.Paragraph(rx, py, 28, "Click + to add pinches. Two to five make a blend the town has never tasted.", Palette.Grey);
                    foreach (var ing in draft.Ingredients)
                    {
                        if (ui.Button(new Rectangle(rx, py - 1, 12, 11), "-", true, "Take one pinch out")) { draft.Adjust(ing, -1); ss.PlaySfx?.Invoke(Sfx.Pinch); }
                        var (icon, tint) = IngredientArt(ing);
                        ui.IconLabel(rx + 16, py, icon, tint, ing.Count + "x " + ing.Name, Palette.Outline);
                        py += 11;
                    }

                    y2 = py;
                    if (draft.Pinches > 0)
                    {
                        int dy = top + 12 + 5 * 11 + 4;
                        ui.C.Rect(rx, dy - 3, RightEdge - rx, 1, Palette.Tan);
                        ui.Label(rx, dy, draft.Name, Palette.Purple);
                        ui.C.TextRight("heat " + draft.Heat, RightEdge, dy, Palette.Grey);
                        ui.Label(rx, dy + 11, "Worth " + draft.Value + "pc, tier " + draft.Tier);
                        ui.Stars(RightEdge - 34, dy + 10, draft.Quality);
                        int ny = dy + 23;
                        ui.Label(rx, ny, "Plain mix: " + Balance.BlendBaseQuality + " stars", Palette.Grey);
                        ny += 10;
                        foreach (var note in draft.Notes())
                        {
                            ui.Label(rx, ny, (note.Delta > 0 ? "+" : "") + note.Delta + " " + note.Text, note.Delta > 0 ? Palette.Green : Palette.DarkRed);
                            ny += 10;
                        }
                        if (s.Town.HasTasted(draft)) ui.Label(rx, ny, "The town has tasted this one.", Palette.Grey);
                        else ui.Label(rx, ny, "+1 New to the town", Palette.Green);
                        ny += 10;
                        y2 = Math.Max(y2, ny);
                    }

                    // The ingredient list fills the body exactly, so nothing ever scrolls and these stay put at the bottom.
                    int by = top + area.Height - 14;
                    string blocker = Actions.BlendBlocker(s, draft);
                    if (ui.Button(new Rectangle(rx, by, 72, 14), "Blend", blocker.Length == 0, blocker.Length == 0 ? "Costs " + Balance.BlendSpiceCost + " spice" : blocker, badge: Cost(Balance.BlendSpiceCost)))
                    {
                        var res = Actions.MakeBlend(s, draft);
                        ss.Say(res, Sfx.Blend);
                        if (res.Ok) ss.OnSparkle?.Invoke(new Point(Layout.Mortar.X + 8, Layout.Mortar.Y), ItemArt.BlendColor(draft));
                    }
                    if (ui.Button(new Rectangle(rx + 78, by, 44, 14), "Clear", draft.Pinches > 0)) draft.Clear();
                }

                return Math.Max(y1, y2);
            });
        }

        /// <summary>The little + that drops one pinch of an ingredient into the draft, if the pantry has a spare one.</summary>
        static void PinchButton(Ui ui, Session ss, GameState s, Blend draft, Ingredient ing, int x, int y, bool enabled)
        {
            int spare = Actions.Have(s, ing) - draft.CountOf(ing);
            if (ui.Button(new Rectangle(x, y - 1, 12, 11), "+", enabled && spare > 0, spare > 0 ? "Add a pinch" : "None spare in the pantry"))
            { draft.Adjust(ing, 1); ss.PlaySfx?.Invoke(Sfx.Pinch); }
        }

        // ---- Pantry (inventory) -----------------------------------------------------------------

        static void Pantry(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Pantry")) return;
            var inv = s.Inventory;
            PeppercornHeader(ui, s);
            Scroll(ui, ss, Content(), top =>
            {
                int c1 = Left, c2 = Left + 116, c3 = Left + 246;

                ui.Heading(c1, top, "Peppers");
                for (int k = 0; k < Species.All.Length; k++)
                {
                    var sp = (PepperSpecies)k;
                    int y = top + 12 + k * Row;
                    ui.IconLabel(c1, y, ItemArt.PepperIcon(sp), Color.White, Species.NameOf(sp) + " x" + inv.Pepper(sp), Palette.Outline);
                    if (ui.Button(new Rectangle(c1 + 64, y - 1, 48, 11), "Eat", inv.Pepper(sp) > 0 && s.Spice.Current < s.Spice.Max, "Eat one to restore " + Species.Get(sp).Heat + " spice", badge: "+" + Species.Get(sp).Heat))
                        ss.Say(Actions.Eat(s, sp), Sfx.Eat);
                }
                int seedsY = top + 12 + 4 * Row + 4;
                ui.Heading(c1, seedsY, "Seeds");
                for (int k = 0; k < Species.All.Length; k++)
                {
                    var sp = (PepperSpecies)k;
                    ui.IconLabel(c1, seedsY + 12 + k * 10, "ic_seed", ItemArt.SpeciesColor(sp), Species.NameOf(sp) + " x" + inv.Seed(sp), Palette.Outline);
                }
                int y1 = seedsY + 12 + Species.All.Length * 10;

                ui.Heading(c2, top, "Powder");
                ui.Heading(c2 + 44, top, "Mash");
                for (int k = 0; k < Species.All.Length; k++)
                {
                    var sp = (PepperSpecies)k;
                    int y = top + 12 + k * Row;
                    ui.IconLabel(c2, y, "ic_powder", ItemArt.SpeciesColor(sp), "x" + inv.PowderOf(sp), Palette.Outline);
                    string mash = "x" + inv.Mash[k] + (inv.AgedMash[k] > 0 ? " +" + inv.AgedMash[k] + " aged" : "");
                    ui.IconLabel(c2 + 44, y, "ic_mash", ItemArt.SpeciesColor(sp), mash, Palette.Outline);
                }
                ui.Heading(c2, seedsY, "Sauces");
                if (inv.Sauces.Count == 0) ui.Label(c2, seedsY + 12, "none bottled", Palette.Grey);
                for (int k = 0; k < inv.Sauces.Count && k < 6; k++)
                {
                    var sauce = inv.Sauces[k];
                    ui.IconLabel(c2, seedsY + 12 + k * 10, ItemArt.ProductIcon(sauce), ItemArt.ProductColor(sauce), sauce.Name, Palette.Outline);
                    ui.Stars(c2 + 92, seedsY + 12 + k * 10, sauce.Quality);
                }
                int y2 = seedsY + 12 + Math.Min(inv.Sauces.Count, 6) * 10;
                if (inv.Sauces.Count > 6) { ui.Label(c2, y2, "+" + (inv.Sauces.Count - 6) + " more", Palette.Grey); y2 += 10; }

                ui.Heading(c3, top, "Spices");
                for (int k = 0; k < Inventory.SpiceCount; k++)
                {
                    var spice = (Spice)k;
                    int y = top + 12 + k * 11;
                    var ink = inv.SpiceOf(spice) > 0 ? Palette.Outline : Palette.Grey;
                    ui.IconLabel(c3, y, "ic_pouch", ItemArt.SpiceColor(spice), SpiceInfo.Name(spice), ink);
                    ui.C.TextRight("x" + inv.SpiceOf(spice), RightEdge, y + 1, ink);
                }
                int y3 = top + 12 + Inventory.SpiceCount * 11;

                return Math.Max(y1, Math.Max(y2, y3));
            });
        }

        // ---- Shipping crate ------------------------------------------------------------------------

        static void Crate(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Shipping crate")) return;
            const int ColChars = 26; // each column is ~160px wide; 26 glyphs at a 6px advance
            Scroll(ui, ss, Content(20), top =>
            {
                int y1 = top;
                ui.Heading(Left, y1, "Bottled (" + s.Inventory.Sauces.Count + ")");
                y1 += 12;
                if (s.Inventory.Sauces.Count == 0) y1 = ui.Paragraph(Left, y1, ColChars, "Nothing to ship. Cook first!", Palette.Grey);
                for (int k = 0; k < s.Inventory.Sauces.Count && k < 10; k++, y1 += Row)
                {
                    var sauce = s.Inventory.Sauces[k];
                    ui.IconLabel(Left, y1, ItemArt.ProductIcon(sauce), ItemArt.ProductColor(sauce), sauce.Name, Palette.Outline);
                    ui.Stars(Left + 94, y1, sauce.Quality);
                    bool full = s.Crate.IsFull(s.Level);
                    if (ui.Button(new Rectangle(Left + 132, y1 - 1, 34, 11), "Ship", !full, full ? "The crate is full" : "Put it in the crate"))
                        ss.Say(Actions.Ship(s, k), Sfx.Ship);
                }

                int cx = Left + 172;
                int y2 = top;
                ui.Heading(cx, y2, "In the crate (" + s.Crate.Sauces.Count + "/" + s.CrateCapacity + ")");
                y2 += 12;
                if (s.Crate.Sauces.Count == 0) y2 = ui.Paragraph(cx, y2, ColChars, "Empty. The cart leaves at nightfall.", Palette.Grey);
                for (int k = 0; k < s.Crate.Sauces.Count; k++, y2 += Row)
                {
                    var sauce = s.Crate.Sauces[k];
                    if (ui.Button(new Rectangle(cx, y2 - 1, 34, 11), "Take", true, "Back to the pantry")) ss.Say(Actions.Unship(s, k), Sfx.Unship);
                    ui.IconLabel(cx + 38, y2, ItemArt.ProductIcon(sauce), ItemArt.ProductColor(sauce), sauce.Name, Palette.Outline);
                    ui.Stars(cx + 132, y2, sauce.Quality);
                }

                return Math.Max(y1, y2);
            });
            ui.Paragraph(Left, Frame.Bottom - 24, MaxChars, "Rated overnight. Quota sauces" + (s.Quota?.CravedType is SauceType ct ? ", " + (ct == SauceType.Hot ? "hot sauces" : "curries") : "") + " and new blends gain a star; repeats within three days lose one.", Palette.Grey);
        }

        // ---- Tower door: sleep ---------------------------------------------------------------------

        static void Door(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Tower door")) return;
            ui.Heading(Left, Top, "It is " + s.Clock.TimeText() + " on day " + s.Clock.Day + ". Turn in for the night?");
            int growing = 0, ready = 0;
            foreach (var p in s.Garden.Plots) if (p.Plant != null && !p.Plant.IsMature) { if (p.Plant.GrowthTonight() > 0) growing++; else ready++; }
            int fermenting = 0;
            foreach (var j in s.Shelf.Jars) if (!j.IsEmpty && !j.IsReady) fermenting++;
            int y = Top + 16;
            ui.IconLabel(Left, y, "ic_water", Color.White, growing + " plants will grow tonight, " + ready + " will not.", ready > 0 ? Palette.DarkRed : Palette.Outline); y += 12;
            ui.IconLabel(Left, y, "ic_mash", Palette.Tan, fermenting + " jars fermenting.", Palette.Outline); y += 12;
            ui.IconLabel(Left, y, "ic_hot", Palette.Red, s.Crate.Sauces.Count + " sauces in the crate for the cart.", s.Crate.Sauces.Count > 0 ? Palette.Outline : Palette.Grey); y += 12;
            ui.IconLabel(Left, y, "ic_flame", Color.White, "Sleeping refills your spice to " + s.Spice.Max + ".", Palette.Grey); y += 12;
            if (s.Clock.DayOfWeek == 7) { ui.Label(Left, y, "The week ends tonight! The quota is judged at dawn.", Palette.DarkRed); y += 12; }
            if (s.Rush is RushOrder rush && !rush.IsMet)
            {
                var r = RecipeBook.Get(rush.RecipeId);
                int inCrate = s.Crate.Sauces.Count(x => x.RecipeId == rush.RecipeId);
                bool dueTonight = rush.DaysLeft(s.Clock.Day) <= 0;
                int owed = rush.Count - rush.Delivered;
                // The envelope says "rush order"; the words go on the deadline so the longest recipe name still fits.
                string due = rush.DueText(s.Clock.Day);
                string text = char.ToUpper(due[0]) + due.Substring(1) + ": " + owed + "x " + r.Name + ", " + inCrate + " in the crate" + (dueTonight ? "!" : ".");
                ui.IconLabel(Left, y, "ic_envelope", Color.White, text, dueTonight && inCrate < owed ? Palette.DarkRed : Palette.Purple); y += 12;
            }
            y += 8;
            if (ui.Button(new Rectangle(Left, y, 90, 16), "Sleep", true)) { ss.Close(); ss.PlaySfx?.Invoke(Sfx.Yawn); ss.RequestSleep?.Invoke(); }
            if (ui.Button(new Rectangle(Left + 100, y, 90, 16), "Not yet", true)) ss.Close();
        }

        // ---- Morning report --------------------------------------------------------------------------

        static void Morning(Ui ui, GameState s, Session ss)
        {
            var r = s.LastReport;
            if (r == null) { ss.Close(); return; }
            if (!Open(ui, ss, "Morning of day " + r.Day + " - week " + s.Clock.Week)) return;
            Scroll(ui, ss, Content(22), top =>
            {
                int y = top;
                if (r.Sales.Count == 0)
                {
                    ui.Label(Left, y, "The cart left empty. The town went hungry.", Palette.Grey);
                    y += 12;
                }
                foreach (var sale in r.Sales)
                {
                    ui.IconLabel(Left, y, ItemArt.ProductIcon(sale.Sauce), ItemArt.ProductColor(sale.Sauce), sale.Sauce.Name, Palette.Outline);
                    ui.Stars(Left + 90, y, sale.Stars);
                    ui.IconLabel(Left + 132, y, "ic_peppercorn", Color.White, "+" + sale.Peppercorns, Palette.Green);
                    ui.IconLabel(Left + 172, y, "ic_hat", Color.White, "+" + sale.Xp + " fame", Palette.Green);
                    ui.Label(Left + 8, y + 9, sale.Remark, Palette.Grey);
                    y += 19;
                }
                y += 3;
                ui.Heading(Left, y, "Earned " + r.PeppercornsEarned + " peppercorns and " + r.XpEarned + " fame.");
                y += 12;
                if (r.QuotaEvaluated)
                {
                    if (r.QuotaMet)
                        ui.IconLabel(Left, y, "ic_check", Color.White, "Quota met! +" + r.QuotaBonusPeppercorns + "pc, +" + r.QuotaBonusXp + " fame and " + SpiceInfo.Name(r.QuotaBonusSpice.Value) + ".", Palette.Green);
                    else ui.IconLabel(Left, y, "ic_cross", Color.White, "The council sighs: last week's quota went unmet.", Palette.DarkRed);
                    y += 12;
                }
                if (r.NewQuotaPosted)
                {
                    ui.Label(Left, y, "A new request is pinned to the notice board.", Palette.Purple); y += 12;
                    if (s.Quota?.CravedType is SauceType craved) { ui.Label(Left + 10, y, Quota.CravingText(craved), Palette.Orange); y += 12; }
                }
                if (r.RushCompleted)
                {
                    ui.IconLabel(Left, y, "ic_envelope", Color.White, "Rush order filled! " + RecipeBook.Get(r.RushRecipeId).Name + " was in time: +" + r.RushBonusXp + " fame.", Palette.Green); y += 12;
                }
                else if (r.RushExpired)
                {
                    ui.IconLabel(Left, y, "ic_envelope", Palette.Grey, "The rush order for " + RecipeBook.Get(r.RushRecipeId).Name + " lapsed. No harm done.", Palette.Grey); y += 12;
                }
                if (r.RushPosted && s.Rush is RushOrder rush)
                {
                    ui.IconLabel(Left, y, "ic_envelope", Color.White, "A rush order! " + rush.Count + "x " + RecipeBook.Get(rush.RecipeId).Name + " by the cart " + (rush.DaysLeft(s.Clock.Day) == 1 ? "tomorrow night" : "in " + rush.DaysLeft(s.Clock.Day) + " nights") + ", paid double.", Palette.Purple); y += 12;
                }
                if (r.Weather != Weather.Clear)
                {
                    ui.IconLabel(Left, y, r.Weather == Weather.Rain ? "ic_rain" : "ic_wind", r.Weather == Weather.Rain ? Color.White : Palette.Grey, WeatherInfo.Describe(r.Weather), r.Weather == Weather.Rain ? Palette.Blue : Palette.Grey);
                    y += 12;
                }
                if (r.LevelsGained > 0)
                {
                    ui.IconLabel(Left, y, "ic_hat", Color.White, "Level up! Now a level " + r.NewLevel + " " + Progression.Title(r.NewLevel) + ".", Palette.Purple);
                    y += 10;
                    string unlock = Progression.UnlockAt(r.NewLevel);
                    if (unlock.Length > 0) y = ui.Paragraph(Left + 10, y, MaxChars - 2, "Unlocked: " + unlock, Palette.Purple);
                    y += 2;
                }
                ui.Label(Left, y, "Ready to harvest: " + r.PlantsReady + ".  Jars ready: " + r.JarsReady + ".", Palette.Grey);
                return y + PixelFont.LineHeight;
            });
            if (ui.Button(new Rectangle(Frame.Right - 100, Frame.Bottom - 22, 90, 16), "Rise and shine", true)) ss.Close();
        }

        // ---- Help --------------------------------------------------------------------------------------

        static void Help(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "How to be a Spice Wizard")) return;
            // "KEYWORD|body": the keyword is drawn in the heading colour, the body in plain ink.
            string[] lines =
            {
                "|Click anything in the yard to use it. A day lasts six minutes; at 22:00 you sleep, or click the door or the moon to turn in early.",
                "GROW|Plant, water (refill at the well) and pep-talk your peppers. Each kind has its own temperament. Rainy days do the watering for you.",
                "FERMENT|Two peppers in a jar become mash after two nights.",
                "GRIND|The mortar turns peppers into powder for curries, or mixes powder, spices and peppercorns into blends of your own.",
                "COOK|The cauldron brews hot sauces and curries for spice.",
                "SELL|Bottles in the crate are rated at dawn and paid for in peppercorns, which are also an ingredient.",
                "QUOTA|Fill the notice board request each week for bonuses.",
                "RUSH|Some mornings bring a rush order: a sauce wanted within two nights, paid double. The envelope by the day shows one is open.",
                "|Spice is your cooking energy: eat a pepper or sleep. Reach level 20 to become the Master Spice Wizard.",
            };
            Scroll(ui, ss, Content(), top =>
            {
                int y = top;
                foreach (var line in lines)
                {
                    int bar = line.IndexOf('|');
                    string key = line.Substring(0, bar), body = line.Substring(bar + 1);
                    if (key.Length == 0) { y = ui.Paragraph(Left, y, MaxChars, body, Palette.Outline) + 3; continue; }
                    // The keyword sits in the margin of the first line; the body wraps beside it.
                    ui.Heading(Left, y, key);
                    y = ui.Paragraph(Left + 50, y, MaxChars - 9, body, Palette.Outline) + 3;
                }
                return y;
            });
        }
    }
}
