using System;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;
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

        static void Header(Ui ui, string icon, string text)
        {
            int w = PixelFont.Measure(text) + 10;
            ui.IconLabel(Frame.Right - 22 - w, Frame.Y + 4, icon, Color.White, text, Palette.Cream);
        }

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
                    ui.Label(Left, top, "Bare soil. Plant a seed:");
                    int y = top;
                    for (int k = 0; k < Species.All.Length; k++)
                    {
                        var info = Species.All[k];
                        y = top + 16 + k * (Row + 6);
                        int seeds = s.Inventory.Seed(info.Species);
                        ui.IconLabel(Left, y + 2, ItemArt.PepperIcon(info.Species), Color.White, info.Name + " pepper", Palette.Outline);
                        ui.Label(Left + 110, y + 3, "seeds: " + seeds, seeds > 0 ? Palette.Outline : Palette.Grey);
                        ui.Label(Left, y + 11, info.CareHint, Palette.Grey);
                        string tip = info.UnlockLevel > s.Level ? "Seeds unlock at level " + info.UnlockLevel : info.GrowthPoints + " growth to ripen, yields " + info.Yield;
                        if (ui.Button(new Rectangle(Left + 200, y, 56, 13), "Plant", seeds > 0, tip))
                        {
                            var r = Actions.PlantSeed(s, i, info.Species);
                            ss.Say(r);
                            if (r.Ok) ss.OnSparkle?.Invoke(new Point(pos.X + 12, pos.Y + 4), Palette.LightGreen);
                        }
                    }
                    return y + Row + 6;
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
                ui.C.Sprite(sprite, Left + 8, top + 8);
                if (plant.IsMature && plant.Species == PepperSpecies.Ghost) ui.C.Sprite("ghost", Left + 11, top + 4);

                ui.Label(Left + 40, top, sp.Name + " pepper", Palette.DarkRed);
                ui.Label(Left + 40, top + 10, "Stage: " + plant.Stage + "   Growth " + plant.Points + "/" + sp.GrowthPoints);
                ui.Bar(new Rectangle(Left + 40, top + 20, 120, 6), plant.Points / (float)sp.GrowthPoints, Palette.LightGreen);
                ui.Label(Left + 40, top + 30, plant.Mood(), Palette.Purple);
                ui.Label(Left, top + 46, sp.CareHint, Palette.Grey);
                ui.Label(Left, top + 58, "Watered today: " + (plant.WateredToday ? "yes" : "no") + "   yesterday: " + (plant.WateredYesterday ? "yes" : "no"));
                ui.Label(Left, top + 68, "Pep talk today: " + (plant.PepTalkedToday ? "yes" : "no") + "   Tonight: +" + plant.GrowthTonight() + " growth");

                int by = top + 88;
                bool canWater = !plant.IsMature && !plant.WateredToday;
                if (ui.Button(new Rectangle(Left, by, 80, 16), "Water (" + s.Garden.BucketWater + "/" + Garden.BucketCapacity + ")", canWater && s.Garden.BucketWater > 0,
                    s.Garden.BucketWater == 0 ? "Bucket is empty: click the well" : "Water the plant"))
                {
                    var r = Actions.Water(s, i);
                    ss.Say(r);
                    if (r.Ok) ss.OnWatered?.Invoke(new Point(pos.X + 12, pos.Y + 4));
                }
                if (ui.Button(new Rectangle(Left + 86, by, 80, 16), "Pep talk (1)", !plant.IsMature && !plant.PepTalkedToday && s.Spice.CanSpend(Balance.PepTalkSpiceCost),
                    "Costs 1 spice. Every pepper reacts differently"))
                {
                    var r = Actions.PepTalk(s, i);
                    ss.Say(r);
                    if (r.Ok) ss.OnSparkle?.Invoke(new Point(pos.X + 12, pos.Y), Palette.Pink);
                }
                if (ui.Button(new Rectangle(Left + 172, by, 80, 16), "Harvest", plant.IsMature, "Yields " + sp.Yield))
                {
                    var r = Actions.Harvest(s, i);
                    ss.Say(r);
                    if (r.Ok) { ss.OnSparkle?.Invoke(new Point(pos.X + 12, pos.Y), Palette.Yellow); ss.Close(); }
                }
                return by + 16;
            });
        }

        // ---- Notice board -------------------------------------------------------------

        static void Board(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Notice board - week " + s.Clock.Week)) return;
            var q = s.Quota;
            Scroll(ui, ss, Content(), top =>
            {
                int y = top;
                ui.Label(Left, y, "The town council requests, by the end of day " + s.Clock.Week * 7 + ":", Palette.DarkRed);
                y += 14;
                if (q == null) { ui.Label(Left, y, "Nothing posted."); return y + Row; }
                for (int i = 0; i < q.Lines.Count; i++)
                {
                    var line = q.Lines[i];
                    var r = RecipeBook.Get(line.RecipeId);
                    ui.IconLabel(Left + 6, y, ItemArt.SauceIcon(r), ItemArt.SauceColor(r.Id), line.Required + "x " + r.Name, Palette.Outline);
                    ui.Label(Left + 150, y + 1, line.Sold + " / " + line.Required, line.IsMet ? Palette.Green : Palette.DarkRed);
                    if (line.IsMet) ui.C.Sprite("ic_check", Left + 190, y);
                    y += Row + 2;
                }
                y += 8;
                ui.Label(Left, y, "Today is day " + s.Clock.DayOfWeek + " of 7."); y += 12;
                ui.Label(Left, y, "Reward: " + Balance.QuotaBonusPeppercorns(q.Week) + " peppercorns, " + Balance.QuotaBonusXp(q.Week) + " fame and a rare spice."); y += 12;
                ui.Label(Left, y, "Sauces on the list earn an extra star when sold.", Palette.Grey); y += 12;
                ui.Label(Left, y, "The town tires of the same sauce twice in three days.", Palette.Grey); y += 12;
                if (q.IsMet) { y += 4; ui.Label(Left, y, "Quota met! The bonus arrives at the start of next week.", Palette.Green); y += 12; }
                return y;
            });
        }

        // ---- Merchant --------------------------------------------------------------------

        static void Market(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Travelling merchant")) return;
            Header(ui, "ic_peppercorn", s.Peppercorns + " peppercorns");
            Scroll(ui, ss, Content(), top =>
            {
                int y1 = top;
                ui.Label(Left, y1, "Seeds", Palette.DarkRed);
                y1 += 12;
                for (int k = 0; k < Species.All.Length; k++, y1 += Row)
                {
                    var info = Species.All[k];
                    bool unlocked = info.UnlockLevel <= s.Level;
                    ui.IconLabel(Left, y1, ItemArt.PepperIcon(info.Species), unlocked ? Color.White : Palette.Grey, info.Name, unlocked ? Palette.Outline : Palette.Grey);
                    ui.Label(Left + 56, y1 + 1, info.SeedCost + "pc", Palette.Outline);
                    if (!unlocked) ui.Label(Left + 86, y1 + 1, "Lv" + info.UnlockLevel, Palette.Grey);
                    else if (ui.Button(new Rectangle(Left + 84, y1 - 1, 30, 11), "Buy", s.Peppercorns >= info.SeedCost, info.CareHint))
                        ss.Say(Actions.BuySeed(s, info.Species));
                    ui.Label(Left + 120, y1 + 1, "x" + s.Inventory.Seed(info.Species), Palette.Grey);
                }

                int sx = Left + 156;
                int y2 = top;
                ui.Label(sx, y2, "Spices", Palette.DarkRed);
                y2 += 12;
                for (int k = 0; k < Inventory.SpiceCount; k++, y2 += Row)
                {
                    var spice = (Spice)k;
                    bool unlocked = SpiceInfo.UnlockLevel(spice) <= s.Level;
                    ui.IconLabel(sx, y2, "ic_pouch", unlocked ? ItemArt.SpiceColor(spice) : Palette.Grey, SpiceInfo.Name(spice), unlocked ? Palette.Outline : Palette.Grey);
                    ui.Label(sx + 84, y2 + 1, SpiceInfo.Price(spice) + "pc", Palette.Outline);
                    if (!unlocked) ui.Label(sx + 112, y2 + 1, "Lv" + SpiceInfo.UnlockLevel(spice), Palette.Grey);
                    else if (ui.Button(new Rectangle(sx + 110, y2 - 1, 30, 11), "Buy", s.Peppercorns >= SpiceInfo.Price(spice)))
                        ss.Say(Actions.BuySpice(s, spice));
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
            Header(ui, "ic_flame", "Spice " + s.Spice.Current + "/" + s.Spice.Max);
            Scroll(ui, ss, Content(), top =>
            {
                int y1 = top;
                for (int k = 0; k < RecipeBook.All.Length; k++, y1 += 12)
                {
                    var r = RecipeBook.All[k];
                    bool unlocked = r.UnlockLevel <= s.Level;
                    var rowRect = new Rectangle(Left, y1 - 1, 162, 11);
                    bool selected = ss.SelectedRecipe == r.Id;
                    if (selected) ui.C.Rect(rowRect, Palette.Yellow * 0.5f);
                    ui.IconLabel(Left + 2, y1, ItemArt.SauceIcon(r), unlocked ? ItemArt.SauceColor(r.Id) : Palette.Grey, r.Name, unlocked ? Palette.Outline : Palette.Grey);
                    ui.Label(Left + 100, y1 + 1, unlocked ? r.BaseValue + "pc" : "Lv" + r.UnlockLevel, unlocked ? Palette.Outline : Palette.Grey);
                    if (ui.Take(rowRect)) ss.SelectedRecipe = r.Id;
                    string blocker = Actions.CookBlocker(s, r, ss.ExtraPeppercorn);
                    if (unlocked && ui.Button(new Rectangle(Left + 130, y1 - 1, 34, 11), "Cook", blocker.Length == 0, blocker.Length == 0 ? "Costs " + Balance.CookSpiceCost + " spice" : blocker))
                    {
                        var res = Actions.Cook(s, r.Id, ss.ExtraPeppercorn);
                        ss.Say(res);
                        if (res.Ok) ss.OnSparkle?.Invoke(new Point(Layout.Cauldron.X + 12, Layout.Cauldron.Y + 2), ItemArt.SauceColor(r.Id));
                    }
                }

                var sel = RecipeBook.Get(ss.SelectedRecipe);
                int dx = Left + 178, dy = top;
                ui.Label(dx, dy, sel.Name, Palette.DarkRed);
                ui.Label(dx, dy + 10, (sel.Type == SauceType.Hot ? "Hot sauce" : "Curry sauce") + ", tier " + sel.Tier, Palette.Grey);
                ui.Label(dx, dy + 22, "Needs:");
                for (int k = 0; k < sel.Ingredients.Length; k++)
                {
                    var ing = sel.Ingredients[k];
                    int have = Actions.Have(s, ing);
                    bool ok = have >= ing.Count;
                    ui.Label(dx + 4, dy + 32 + k * 10, ing.Count + " " + ing.Name + " (" + have + ")", ok ? Palette.Green : Palette.DarkRed);
                }
                int ey = dy + 32 + sel.Ingredients.Length * 10 + 6;
                var box = new Rectangle(dx, ey, 9, 9);
                ui.C.Rect(box, Palette.Outline);
                ui.C.Rect(dx + 1, ey + 1, 7, 7, ss.ExtraPeppercorn ? Palette.Yellow : Palette.Cream);
                ui.Label(dx + 12, ey + 1, "Extra peppercorn (+1 star)");
                if (ui.Take(new Rectangle(dx, ey, 140, 10))) ss.ExtraPeppercorn = !ss.ExtraPeppercorn;
                ui.Label(dx, ey + 14, "Aged mash also adds a star.", Palette.Grey);
                ui.Label(dx, ey + 24, "Unfamiliar recipes lose one.", Palette.Grey);
                int y2 = ey + 34;

                return Math.Max(y1, y2);
            });
        }

        // ---- Fermenting jars ------------------------------------------------------------------

        static void Shelf(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Fermenting shelf")) return;
            Scroll(ui, ss, Content(), top =>
            {
                ui.Label(Left, top, "Pack " + Jar.PeppersPerJar + " peppers of one kind: mash in " + Jar.NightsToFerment + " nights, aged in " + Jar.NightsToAge + ".", Palette.Grey);
                int y = top + 14;
                for (int j = 0; j < FermentShelf.MaxJars; j++, y += 34)
                {
                    var jar = s.Shelf.Jars[j];
                    bool unlocked = j < s.UnlockedJars;
                    ui.C.Sprite(unlocked ? (jar.IsEmpty ? "jar_empty" : "jar_full") : "jar_lock", Left, y, unlocked && !jar.IsEmpty ? ItemArt.SpeciesColor(jar.Species.Value) : Color.White);
                    if (!unlocked)
                    {
                        ui.Label(Left + 16, y + 3, "Unlocks at level " + (j == 2 ? 5 : 10), Palette.Grey);
                        continue;
                    }
                    ui.Label(Left + 16, y + 3, jar.Status(), jar.IsReady ? Palette.Green : Palette.Outline);
                    if (jar.IsEmpty)
                    {
                        for (int k = 0; k < Species.All.Length; k++)
                        {
                            var sp = (PepperSpecies)k;
                            int have = s.Inventory.Pepper(sp);
                            if (ui.Button(new Rectangle(Left + 16 + k * 60, y + 15, 56, 12), Species.NameOf(sp) + " " + have, have >= Jar.PeppersPerJar, "Pack " + Jar.PeppersPerJar + " " + Species.NameOf(sp) + " peppers"))
                                ss.Say(Actions.FillJar(s, j, sp));
                        }
                    }
                    else if (jar.IsReady)
                    {
                        if (ui.Button(new Rectangle(Left + 16, y + 15, 84, 12), "Collect mash", true, jar.IsAged ? "Aged mash: +1 star when cooked" : "Leave it " + (Jar.NightsToAge - jar.Nights) + " more nights to age"))
                            ss.Say(Actions.EmptyJar(s, j));
                    }
                    else ui.Label(Left + 16, y + 17, "Bubbling away...", Palette.Grey);
                }
                return y;
            });
        }

        // ---- Mortar: grinding and blending -----------------------------------------------------

        static void Mortar(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Mortar and pestle")) return;
            Header(ui, "ic_flame", "Spice " + s.Spice.Current + "/" + s.Spice.Max);
            var draft = ss.Draft;
            bool unlocked = s.Level >= Balance.BlendUnlockLevel;
            bool room = draft.Pinches < Balance.MaxBlendPinches;
            int rx = Left + 160;

            Scroll(ui, ss, Content(unlocked ? 26 : 0), top =>
            {
                ui.Label(Left, top, "Grind, then pinch a blend:", Palette.Grey);
                int y = top + 12;
                for (int k = 0; k < Species.All.Length; k++, y += 11)
                {
                    var sp = (PepperSpecies)k;
                    var ing = Ingredient.Powder(sp);
                    ui.IconLabel(Left, y, ItemArt.PepperIcon(sp), Color.White, Species.NameOf(sp) + " " + s.Inventory.Pepper(sp), Palette.Outline);
                    ui.IconLabel(Left + 66, y, "ic_powder", ItemArt.SpeciesColor(sp), "x" + s.Inventory.PowderOf(sp), Palette.Outline);
                    if (ui.Button(new Rectangle(Left + 100, y - 1, 34, 11), "Grind", s.Inventory.Pepper(sp) > 0 && s.Spice.CanSpend(Balance.GrindSpiceCost), "One pepper to one powder for " + Balance.GrindSpiceCost + " spice"))
                        ss.Say(Actions.Grind(s, sp));
                    PinchButton(ui, s, draft, ing, Left + 138, y, unlocked && room);
                }
                y += 2;
                for (int k = 0; k < Inventory.SpiceCount; k++, y += 11)
                {
                    var spice = (Spice)k;
                    var ing = Ingredient.Of(spice);
                    int have = s.Inventory.SpiceOf(spice);
                    ui.IconLabel(Left, y, "ic_pouch", ItemArt.SpiceColor(spice), SpiceInfo.Name(spice) + " x" + have, have > 0 ? Palette.Outline : Palette.Grey);
                    PinchButton(ui, s, draft, ing, Left + 138, y, unlocked && room);
                }
                ui.IconLabel(Left, y, "ic_peppercorn", Color.White, "Peppercorn x" + s.Peppercorns, Palette.Outline);
                PinchButton(ui, s, draft, Ingredient.Peppercorns(1), Left + 138, y, unlocked && room);
                int y1 = y + 11;

                ui.Label(rx, top, "Your blend", Palette.DarkRed);
                ui.Label(rx + 72, top, draft.Pinches + "/" + Balance.MaxBlendPinches + " pinches", Palette.Grey);
                int y2;
                if (!unlocked)
                {
                    y2 = ui.Paragraph(rx, top + 14, 30, "Blending unlocks at level " + Balance.BlendUnlockLevel + ". Until then the mortar only grinds.", Palette.Grey);
                }
                else
                {
                    int py = top + 12;
                    if (draft.Pinches == 0) py = ui.Paragraph(rx, py, 30, "Click + to add pinches. Two to five make a blend the town has never tasted.", Palette.Grey);
                    foreach (var ing in draft.Ingredients)
                    {
                        if (ui.Button(new Rectangle(rx, py - 1, 12, 11), "-", true, "Take one pinch out")) draft.Adjust(ing, -1);
                        string icon = ing.Kind == IngredientKind.Powder ? "ic_powder" : ing.Kind == IngredientKind.Spice ? "ic_pouch" : "ic_peppercorn";
                        var tint = ing.Kind == IngredientKind.Powder ? ItemArt.SpeciesColor((PepperSpecies)ing.Index) : ing.Kind == IngredientKind.Spice ? ItemArt.SpiceColor((Spice)ing.Index) : Color.White;
                        ui.IconLabel(rx + 16, py, icon, tint, ing.Count + " " + ing.Name, Palette.Outline);
                        py += 11;
                    }

                    y2 = py;
                    if (draft.Pinches > 0)
                    {
                        int dy = top + 12 + 5 * 11 + 4;
                        ui.Label(rx, dy, draft.Name, Palette.Purple);
                        ui.Label(rx + 96, dy, "heat " + draft.Heat, Palette.Grey);
                        ui.Label(rx, dy + 11, "Worth " + draft.Value + "pc, tier " + draft.Tier);
                        ui.Stars(rx + 120, dy + 10, draft.Quality);
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
                }

                return Math.Max(y1, y2);
            });

            if (unlocked)
            {
                string blocker = Actions.BlendBlocker(s, draft);
                if (ui.Button(new Rectangle(rx, Frame.Bottom - 22, 72, 16), "Blend (" + Balance.BlendSpiceCost + ")", blocker.Length == 0, blocker.Length == 0 ? "Costs " + Balance.BlendSpiceCost + " spice" : blocker))
                {
                    var res = Actions.MakeBlend(s, draft);
                    ss.Say(res);
                    if (res.Ok) ss.OnSparkle?.Invoke(new Point(Layout.Mortar.X + 8, Layout.Mortar.Y), ItemArt.BlendColor(draft));
                }
                if (ui.Button(new Rectangle(rx + 78, Frame.Bottom - 22, 44, 16), "Clear", draft.Pinches > 0)) draft.Clear();
            }
        }

        /// <summary>The little + that drops one pinch of an ingredient into the draft, if the pantry has a spare one.</summary>
        static void PinchButton(Ui ui, GameState s, Blend draft, Ingredient ing, int x, int y, bool enabled)
        {
            int spare = Actions.Have(s, ing) - draft.CountOf(ing);
            if (ui.Button(new Rectangle(x, y - 1, 12, 11), "+", enabled && spare > 0, spare > 0 ? "Add a pinch" : "None spare in the pantry"))
                draft.Adjust(ing, 1);
        }

        // ---- Pantry (inventory) -----------------------------------------------------------------

        static void Pantry(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Pantry")) return;
            var inv = s.Inventory;
            Header(ui, "ic_peppercorn", s.Peppercorns + " peppercorns");
            Scroll(ui, ss, Content(), top =>
            {
                int c1 = Left, c2 = Left + 120, c3 = Left + 244;

                ui.Label(c1, top, "Peppers", Palette.DarkRed);
                for (int k = 0; k < Species.All.Length; k++)
                {
                    var sp = (PepperSpecies)k;
                    int y = top + 12 + k * Row;
                    ui.IconLabel(c1, y, ItemArt.PepperIcon(sp), Color.White, Species.NameOf(sp) + " x" + inv.Pepper(sp), Palette.Outline);
                    if (ui.Button(new Rectangle(c1 + 66, y - 1, 44, 11), "Eat +" + Species.Get(sp).Heat, inv.Pepper(sp) > 0 && s.Spice.Current < s.Spice.Max, "Eat one to restore spice"))
                        ss.Say(Actions.Eat(s, sp));
                }
                int seedsY = top + 12 + 4 * Row + 4;
                ui.Label(c1, seedsY, "Seeds", Palette.DarkRed);
                for (int k = 0; k < Species.All.Length; k++)
                {
                    var sp = (PepperSpecies)k;
                    ui.IconLabel(c1, seedsY + 12 + k * 10, "ic_seed", ItemArt.SpeciesColor(sp), Species.NameOf(sp) + " x" + inv.Seed(sp), Palette.Outline);
                }
                int y1 = seedsY + 12 + Species.All.Length * 10;

                ui.Label(c2, top, "Powder / mash", Palette.DarkRed);
                for (int k = 0; k < Species.All.Length; k++)
                {
                    var sp = (PepperSpecies)k;
                    int y = top + 12 + k * Row;
                    ui.IconLabel(c2, y, "ic_powder", ItemArt.SpeciesColor(sp), "x" + inv.PowderOf(sp), Palette.Outline);
                    string mash = "x" + inv.Mash[k] + (inv.AgedMash[k] > 0 ? " +" + inv.AgedMash[k] + " aged" : "");
                    ui.IconLabel(c2 + 36, y, "ic_mash", ItemArt.SpeciesColor(sp), mash, Palette.Outline);
                }
                ui.Label(c2, seedsY, "Sauces", Palette.DarkRed);
                if (inv.Sauces.Count == 0) ui.Label(c2, seedsY + 12, "none bottled", Palette.Grey);
                for (int k = 0; k < inv.Sauces.Count && k < 6; k++)
                {
                    var sauce = inv.Sauces[k];
                    ui.IconLabel(c2, seedsY + 12 + k * 10, ItemArt.ProductIcon(sauce), ItemArt.ProductColor(sauce), sauce.Name + " " + sauce.Quality + "*", Palette.Outline);
                }
                int y2 = seedsY + 12 + Math.Min(inv.Sauces.Count, 6) * 10;
                if (inv.Sauces.Count > 6) { ui.Label(c2, y2, "+" + (inv.Sauces.Count - 6) + " more", Palette.Grey); y2 += 10; }

                ui.Label(c3, top, "Spices", Palette.DarkRed);
                for (int k = 0; k < Inventory.SpiceCount; k++)
                {
                    var spice = (Spice)k;
                    int y = top + 12 + k * 11;
                    ui.IconLabel(c3, y, "ic_pouch", ItemArt.SpiceColor(spice), SpiceInfo.Name(spice) + " " + inv.SpiceOf(spice), inv.SpiceOf(spice) > 0 ? Palette.Outline : Palette.Grey);
                }
                int y3 = top + 12 + Inventory.SpiceCount * 11;

                return Math.Max(y1, Math.Max(y2, y3));
            });
        }

        // ---- Shipping crate ------------------------------------------------------------------------

        static void Crate(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Shipping crate")) return;
            Scroll(ui, ss, Content(20), top =>
            {
                int y1 = top;
                ui.Label(Left, y1, "Bottled (" + s.Inventory.Sauces.Count + ")", Palette.DarkRed);
                y1 += 12;
                if (s.Inventory.Sauces.Count == 0) { ui.Label(Left, y1, "Nothing to ship. Cook first!", Palette.Grey); y1 += Row; }
                for (int k = 0; k < s.Inventory.Sauces.Count && k < 10; k++, y1 += Row)
                {
                    var sauce = s.Inventory.Sauces[k];
                    ui.IconLabel(Left, y1, ItemArt.ProductIcon(sauce), ItemArt.ProductColor(sauce), sauce.Name, Palette.Outline);
                    ui.Stars(Left + 96, y1, sauce.Quality);
                    if (ui.Button(new Rectangle(Left + 134, y1 - 1, 12, 11), ">", !s.Crate.IsFull, "Ship to town"))
                        ss.Say(Actions.Ship(s, k));
                }

                int cx = Left + 160;
                int y2 = top;
                ui.Label(cx, y2, "In the crate (" + s.Crate.Sauces.Count + "/" + Balance.CrateCapacity + ")", Palette.DarkRed);
                y2 += 12;
                if (s.Crate.Sauces.Count == 0) { ui.Label(cx, y2, "Empty. The cart leaves at nightfall.", Palette.Grey); y2 += Row; }
                for (int k = 0; k < s.Crate.Sauces.Count; k++, y2 += Row)
                {
                    var sauce = s.Crate.Sauces[k];
                    if (ui.Button(new Rectangle(cx, y2 - 1, 12, 11), "<", true, "Take back")) ss.Say(Actions.Unship(s, k));
                    ui.IconLabel(cx + 16, y2, ItemArt.ProductIcon(sauce), ItemArt.ProductColor(sauce), sauce.Name, Palette.Outline);
                    ui.Stars(cx + 112, y2, sauce.Quality);
                }

                return Math.Max(y1, y2);
            });
            ui.Paragraph(Left, Frame.Bottom - 24, MaxChars, "Rated overnight. Quota sauces and new blends gain a star; repeats within three days lose one.", Palette.Grey);
        }

        // ---- Tower door: sleep ---------------------------------------------------------------------

        static void Door(Ui ui, GameState s, Session ss)
        {
            if (!Open(ui, ss, "Tower door")) return;
            ui.Label(Left, Top, "It is " + s.Clock.TimeText() + " on day " + s.Clock.Day + ". Turn in for the night?");
            int growing = 0, ready = 0;
            foreach (var p in s.Garden.Plots) if (p.Plant != null && !p.Plant.IsMature) { if (p.Plant.GrowthTonight() > 0) growing++; else ready++; }
            int fermenting = 0;
            foreach (var j in s.Shelf.Jars) if (!j.IsEmpty && !j.IsReady) fermenting++;
            ui.Label(Left, Top + 16, "Tonight " + growing + " plants will grow and " + ready + " will not.", ready > 0 ? Palette.DarkRed : Palette.Outline);
            ui.Label(Left, Top + 28, "Jars fermenting: " + fermenting + ".   Sauces in the crate: " + s.Crate.Sauces.Count + ".");
            ui.Label(Left, Top + 40, "Sleeping refills your spice to " + s.Spice.Max + ".", Palette.Grey);
            if (s.Clock.DayOfWeek == 7) ui.Label(Left, Top + 52, "The week ends tonight! The quota is judged at dawn.", Palette.DarkRed);
            if (ui.Button(new Rectangle(Left, Top + 70, 90, 16), "Sleep", true)) { ss.Close(); ss.RequestSleep?.Invoke(); }
            if (ui.Button(new Rectangle(Left + 100, Top + 70, 90, 16), "Not yet", true)) ss.Close();
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
                    ui.Label(Left + 130, y + 1, "+" + sale.Peppercorns + "pc +" + sale.Xp + "xp", Palette.Green);
                    ui.Label(Left + 8, y + 9, sale.Remark, Palette.Grey);
                    y += 19;
                }
                ui.Label(Left, y, "Earned " + r.PeppercornsEarned + " peppercorns and " + r.XpEarned + " fame.", Palette.DarkRed);
                y += 12;
                if (r.QuotaEvaluated)
                {
                    if (r.QuotaMet)
                        ui.Label(Left, y, "Quota met! +" + r.QuotaBonusPeppercorns + "pc, +" + r.QuotaBonusXp + "xp and " + SpiceInfo.Name(r.QuotaBonusSpice.Value) + ".", Palette.Green);
                    else ui.Label(Left, y, "The council sighs: last week's quota went unmet.", Palette.DarkRed);
                    y += 12;
                }
                if (r.NewQuotaPosted) { ui.Label(Left, y, "A new request is pinned to the notice board.", Palette.Purple); y += 12; }
                if (r.LevelsGained > 0)
                {
                    ui.Label(Left, y, "Level up! Now a level " + r.NewLevel + " " + Progression.Title(r.NewLevel) + ".", Palette.Purple);
                    y += 10;
                    string unlock = Progression.UnlockAt(r.NewLevel);
                    if (unlock.Length > 0) y = ui.Paragraph(Left + 8, y, MaxChars - 2, "Unlocked: " + unlock, Palette.Purple);
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
            string[] lines =
            {
                "Click anything in the yard to use it. A day lasts eight minutes; at 22:00 you sleep and the night moves everything on.",
                "GROW  Plant, water (refill at the well) and pep-talk your peppers. Each kind has its own temperament.",
                "FERMENT  Two peppers in a jar become mash after two nights.",
                "GRIND  The mortar turns peppers into powder for curries, or mixes powder, spices and peppercorns into blends of your own.",
                "COOK  The cauldron brews hot sauces and curries for spice.",
                "SELL  Bottles in the crate are rated at dawn and paid for in peppercorns, which are also an ingredient.",
                "QUOTA  Fill the notice board request each week for bonuses.",
                "Spice is your cooking energy: eat a pepper or sleep. Reach level 20 to become the Master Spice Wizard.",
            };
            Scroll(ui, ss, Content(), top =>
            {
                int y = top;
                foreach (var line in lines)
                {
                    bool heading = char.IsUpper(line[0]) && char.IsUpper(line[1]);
                    y = ui.Paragraph(Left, y, MaxChars, line, heading ? Palette.DarkRed : Palette.Outline) + 2;
                }
                return y;
            });
        }
    }
}
