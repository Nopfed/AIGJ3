using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpiceWizard.Web.Art;

namespace SpiceWizard.Web.Scene
{
    /// <summary>
    /// A few cats that live around the yard. They are pure decoration: they stroll about, nap in the
    /// sun, scratch the trees, hide in the bushes, sit on whatever is sittable, and nothing in the game
    /// takes any notice of them. Each cat is a grey-scale sprite tinted with its coat colour, with the
    /// eyes drawn on top afterwards so they keep their own colour.
    /// </summary>
    public sealed class Cats
    {
        enum Mood { Walk, Sit, Sleep, Roll, Scratch, Stretch, Hide, Perch, Jump }

        sealed class Cat
        {
            public Color Coat, Eyes;
            public Vector2 Feet;
            public bool FacingLeft;
            public Mood Mood = Mood.Sit;
            public float Timer;                 // time left in the current mood
            public float Anim;                  // running clock for frames, blinks and tail flicks
            public float Speed;
            public Vector2 Target;              // where it is walking to
            public Action OnArrive;             // what it does when it gets there
            public Vector2 JumpFrom, JumpTo;
            public float JumpTime, JumpLength;
            public Action OnLand;
            public Point[] HiddenEyes;          // eye pixels peeking out of the bush it is in
            public bool Napping;                // asleep on its perch rather than loafing
            public int Rolls;                   // rolls left before it gets up
        }

        /// <summary>Somewhere to stand and scratch: the trunk is on the side the cat faces.</summary>
        struct Post { public Point Stand; public bool FaceLeft; }
        /// <summary>Somewhere to sit up on: feet go on <see cref="Top"/>, the jump starts and ends at <see cref="Ground"/>.</summary>
        struct Perch { public Point Top, Ground; }
        /// <summary>A bush to vanish into: walk to <see cref="Stand"/>, then only the eyes show.</summary>
        struct Hide { public Point Stand; public Point[] Eyes; }

        readonly List<Cat> _cats = new List<Cat>();
        readonly Random _rng = new Random(13);
        readonly List<Rectangle> _blockers = new List<Rectangle>();
        readonly List<Post> _posts = new List<Post>();
        readonly List<Perch> _perches = new List<Perch>();
        readonly List<Hide> _hides = new List<Hide>();
        Rectangle _spotsFor;
        Particles _particles;

        /// <summary>Sound hooks: a meow when a cat settles or stretches, a purr when it rolls or curls up. Only cats on screen make a noise.</summary>
        public Action OnMeow, OnPurr;

        // Where the eye pixels sit in each pose, for a cat facing right; flipped with the sprite.
        static readonly Dictionary<string, Point[]> EyeSpots = new Dictionary<string, Point[]>
        {
            ["cat_walk0"] = new[] { new Point(11, 2) },
            ["cat_walk1"] = new[] { new Point(11, 2) },
            ["cat_sit"] = new[] { new Point(2, 3), new Point(4, 3) },
            ["cat_loaf"] = new[] { new Point(3, 3) },
            ["cat_sleep"] = new Point[0],
            ["cat_roll0"] = new[] { new Point(2, 4) },
            ["cat_roll1"] = new[] { new Point(2, 4) },
            ["cat_scratch0"] = new[] { new Point(5, 2) },
            ["cat_scratch1"] = new[] { new Point(5, 2) },
            ["cat_stretch"] = new[] { new Point(14, 4) },
            ["cat_jump"] = new[] { new Point(11, 2) },
        };

        public Cats()
        {
            // A ginger tabby with green eyes, a grey with amber eyes and a black cat with yellow ones.
            // They start out hiding in the yard's three bushes and wander off one by one.
            Add(new Color(232, 140, 60), Palette.LightGreen, Layout.Bushes[1], 9);
            Add(Palette.LightGrey, Palette.Gold, Layout.Bushes[0], -1);   // this bush backs onto the fence
            Add(new Color(84, 78, 96), Palette.Yellow, Layout.Bushes[2], 9);
        }

        void Add(Color coat, Color eyes, Point bush, int standBelow)
        {
            _cats.Add(new Cat
            {
                Coat = coat,
                Eyes = eyes,
                Feet = new Vector2(bush.X + 6, bush.Y + standBelow),
                Mood = Mood.Hide,
                HiddenEyes = BushEyes(bush.X, bush.Y),
                Timer = 3f + _cats.Count * 5f + (float)_rng.NextDouble() * 6f,
                Anim = (float)_rng.NextDouble() * 10f,
            });
        }

        static Point[] BushEyes(int x, int y) => new[] { new Point(x + 4, y + 4), new Point(x + 7, y + 4) };

        // ---- Spots -----------------------------------------------------------------------------

        /// <summary>Works out, for the current window, where cats can and cannot go and what there is to
        /// climb, scratch and hide in. The yard is fixed; the meadow outside it changes with the window.</summary>
        void BuildSpots(SceneRenderer scene)
        {
            _spotsFor = Camera.View;
            _blockers.Clear(); _posts.Clear(); _perches.Clear(); _hides.Clear();

            _blockers.Add(new Rectangle(296, 48, 56, 124));  // the tower, down to its doorstep
            _blockers.Add(new Rectangle(10, 126, 110, 66));  // fence and garden beds
            _blockers.Add(new Rectangle(124, 138, 34, 30));  // well and bucket
            _blockers.Add(new Rectangle(164, 104, 24, 28));  // notice board
            _blockers.Add(new Rectangle(96, 78, 60, 28));    // the merchant's cart
            _blockers.Add(new Rectangle(234, 148, 22, 17));  // shipping crate
            _blockers.Add(new Rectangle(256, 164, 34, 42));  // cauldron and its fire
            _blockers.Add(new Rectangle(352, 150, 24, 18));  // pantry
            _blockers.Add(new Rectangle(352, 176, 26, 22));  // stump and mortar

            foreach (var t in Layout.Trees) AddTree("tree", t.X, t.Y);
            foreach (var b in Layout.Bushes) AddBush(b.X, b.Y);
            foreach (var p in scene.Props)
            {
                if (p.Sprite == "tree" || p.Sprite == "tree_big") AddTree(p.Sprite, p.X, p.Y);
                else if (p.Sprite == "bush") AddBush(p.X, p.Y);
                else if (p.Sprite == "rock")
                {
                    _blockers.Add(new Rectangle(p.X, p.Y, 8, 5));
                    _perches.Add(new Perch { Top = new Point(p.X + 4, p.Y + 1), Ground = new Point(p.X + 4, p.Y + 10) });
                }
            }

            // The crate lid, the pantry lid, the rim of the well and the top rail of the fence.
            _perches.Add(new Perch { Top = new Point(Layout.Crate.X + 9, Layout.Crate.Y + 1), Ground = new Point(Layout.Crate.X - 7, Layout.Crate.Y + 13) });
            _perches.Add(new Perch { Top = new Point(Layout.Pantry.X + 9, Layout.Pantry.Y + 1), Ground = new Point(Layout.Pantry.X + 25, Layout.Pantry.Y + 14) });
            _perches.Add(new Perch { Top = new Point(Layout.Well.X + 10, Layout.Well.Y + 13), Ground = new Point(Layout.Well.X + 10, Layout.Well.Y + 31) });
            for (int i = 1; i < Layout.FenceBays; i += 3)
            {
                int x = Layout.Fence.X + i * 12 + 6;
                _perches.Add(new Perch { Top = new Point(x, Layout.Fence.Y + 3), Ground = new Point(x, Layout.Fence.Y - 5) });
            }
            // The stump makes a fine scratching post from the left.
            _posts.Add(new Post { Stand = new Point(Layout.Stump.X - 6, Layout.Stump.Y + 10), FaceLeft = false });
        }

        void AddTree(string sprite, int x, int y)
        {
            bool big = sprite == "tree_big";
            int w = big ? 24 : 16;
            int trunkL = x + (big ? 9 : 6), trunkR = x + (big ? 15 : 10);
            int baseY = y + (big ? 32 : 22);
            _blockers.Add(new Rectangle(x, y + (big ? 6 : 4), w, baseY - y - (big ? 6 : 4)));
            _posts.Add(new Post { Stand = new Point(trunkL - 6, baseY), FaceLeft = false });
            _posts.Add(new Post { Stand = new Point(trunkR + 6, baseY), FaceLeft = true });
            // Up in the leaves: a cat can climb up from either side and loaf on a branch.
            var canopy = new Point(x + w / 2, y + (big ? 17 : 11));
            _perches.Add(new Perch { Top = canopy, Ground = new Point(trunkL - 6, baseY) });
            _perches.Add(new Perch { Top = canopy, Ground = new Point(trunkR + 6, baseY) });
        }

        void AddBush(int x, int y)
        {
            _blockers.Add(new Rectangle(x, y, 12, 8));
            // Cats slip in from the front unless something (the fence) is in the way, then from behind.
            var stand = new Point(x + 6, y + 9);
            if (Blocked(stand.ToVector2())) stand = new Point(x + 6, y - 1);
            _hides.Add(new Hide { Stand = stand, Eyes = BushEyes(x, y) });
        }

        bool Blocked(Vector2 p)
        {
            foreach (var r in _blockers) if (r.Contains((int)p.X, (int)p.Y)) return true;
            return false;
        }

        /// <summary>True when a cat can stroll straight from a to b without walking through anything.</summary>
        bool Clear(Vector2 a, Vector2 b)
        {
            float len = Vector2.Distance(a, b);
            int steps = Math.Max(1, (int)(len / 3f));
            for (int i = 1; i <= steps; i++)
                if (Blocked(Vector2.Lerp(a, b, i / (float)steps))) return false;
            return true;
        }

        Vector2 RandomGrass()
        {
            var v = Camera.View;
            for (int tries = 0; tries < 20; tries++)
            {
                var p = new Vector2(_rng.Next(v.Left + 8, v.Right - 8), _rng.Next(Layout.Horizon + 22, v.Bottom - 14));
                if (!Blocked(p)) return p;
            }
            return Layout.WizardStart.ToVector2();
        }

        // ---- Deciding what to do next ------------------------------------------------------------

        /// <summary>Starts walking to <paramref name="to"/> if there is a way there (straight, or by way
        /// of one random waypoint); returns false if the cat cannot find a route and should do
        /// something else instead.</summary>
        bool WalkTo(Cat cat, Vector2 to, Action then, float speed = 28f)
        {
            if (Blocked(to)) return false;
            if (Vector2.Distance(cat.Feet, to) < 2f) { cat.Feet = to; then?.Invoke(); return true; }
            if (Clear(cat.Feet, to)) { StartWalk(cat, to, then, speed); return true; }
            for (int tries = 0; tries < 12; tries++)
            {
                var via = RandomGrass();
                if (Clear(cat.Feet, via) && Clear(via, to))
                {
                    StartWalk(cat, via, () => StartWalk(cat, to, then, speed), speed);
                    return true;
                }
            }
            return false;
        }

        void StartWalk(Cat cat, Vector2 to, Action then, float speed)
        {
            cat.Mood = Mood.Walk;
            cat.Target = to;
            cat.OnArrive = then;
            cat.Speed = speed;
            cat.FacingLeft = to.X < cat.Feet.X;
        }

        void Jump(Cat cat, Vector2 to, Action land)
        {
            cat.Mood = Mood.Jump;
            cat.JumpFrom = cat.Feet;
            cat.JumpTo = to;
            cat.JumpTime = 0;
            cat.JumpLength = 0.3f + Vector2.Distance(cat.Feet, to) / 90f;
            cat.OnLand = land;
            if (Math.Abs(to.X - cat.Feet.X) > 2) cat.FacingLeft = to.X < cat.Feet.X;
        }

        void Rest(Cat cat, Mood mood, float seconds)
        {
            bool settling = cat.Mood == Mood.Walk || cat.Mood == Mood.Jump;
            cat.Mood = mood;
            cat.Timer = seconds;
            if (!Camera.View.Contains((int)cat.Feet.X, (int)cat.Feet.Y)) return;
            switch (mood)
            {
                case Mood.Sit: if (settling && _rng.Next(3) == 0) OnMeow?.Invoke(); break;
                case Mood.Stretch: if (_rng.Next(2) == 0) OnMeow?.Invoke(); break;
                case Mood.Sleep: case Mood.Roll: OnPurr?.Invoke(); break;
                case Mood.Perch: if (cat.Napping) OnPurr?.Invoke(); break;
            }
        }

        float Between(float lo, float hi) => lo + (float)_rng.NextDouble() * (hi - lo);

        /// <summary>Picks the cat's next pastime. Most choices start with a walk somewhere.</summary>
        void Decide(Cat cat)
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                int roll = _rng.Next(100);
                if (roll < 28)
                {
                    // A stroll to nowhere in particular, then a short sit.
                    if (WalkTo(cat, RandomGrass(), () => Rest(cat, Mood.Sit, Between(2f, 6f)), Between(22f, 34f))) return;
                }
                else if (roll < 40)
                {
                    if (WalkTo(cat, RandomGrass(), () => Rest(cat, Mood.Sleep, Between(14f, 40f)), 24f)) return;
                }
                else if (roll < 52)
                {
                    if (WalkTo(cat, RandomGrass(), () => { cat.Rolls = _rng.Next(3, 7); Rest(cat, Mood.Roll, 0.5f); })) return;
                }
                else if (roll < 66 && _posts.Count > 0)
                {
                    var post = _posts[_rng.Next(_posts.Count)];
                    if (WalkTo(cat, post.Stand.ToVector2(), () => { cat.FacingLeft = post.FaceLeft; Rest(cat, Mood.Scratch, Between(2.5f, 5f)); })) return;
                }
                else if (roll < 80 && _hides.Count > 0)
                {
                    var bush = _hides[_rng.Next(_hides.Count)];
                    if (WalkTo(cat, bush.Stand.ToVector2(), () => { cat.HiddenEyes = bush.Eyes; Rest(cat, Mood.Hide, Between(8f, 25f)); })) return;
                }
                else if (_perches.Count > 0)
                {
                    var perch = _perches[_rng.Next(_perches.Count)];
                    bool nap = _rng.Next(3) == 0;
                    if (WalkTo(cat, perch.Ground.ToVector2(), () => Jump(cat, perch.Top.ToVector2(), () =>
                    {
                        cat.Napping = nap;
                        Rest(cat, Mood.Perch, Between(10f, 35f));
                        cat.OnLand = () => Jump(cat, perch.Ground.ToVector2(), () => Rest(cat, Mood.Sit, 1f));
                    }), 34f)) return;
                }
            }
            Rest(cat, Mood.Sit, Between(2f, 5f));
        }

        // ---- Update ----------------------------------------------------------------------------

        public void Update(float dt, SceneRenderer scene, Particles particles, WizardActor wizard)
        {
            _particles = particles;
            if (_spotsFor != Camera.View) { scene.EnsureProps(); BuildSpots(scene); }

            foreach (var cat in _cats)
            {
                cat.Anim += dt;
                // A window shrink can leave a cat outside the meadow: pop it into a bush.
                var meadow = Camera.View; meadow.Inflate(10, 10);
                if (!meadow.Contains((int)cat.Feet.X, (int)cat.Feet.Y) && cat.Mood != Mood.Jump && _hides.Count > 0)
                {
                    var bush = _hides[_rng.Next(_hides.Count)];
                    cat.Feet = bush.Stand.ToVector2();
                    cat.HiddenEyes = bush.Eyes;
                    Rest(cat, Mood.Hide, Between(3f, 8f));
                }

                if (Underfoot(cat, wizard)) { Scoot(cat, wizard); continue; }

                switch (cat.Mood)
                {
                    case Mood.Walk:
                        {
                            var delta = cat.Target - cat.Feet;
                            float step = cat.Speed * dt;
                            if (delta.Length() <= step)
                            {
                                cat.Feet = cat.Target;
                                var cb = cat.OnArrive; cat.OnArrive = null;
                                if (cb != null) cb(); else Decide(cat);
                            }
                            else { delta.Normalize(); cat.Feet += delta * step; }
                            break;
                        }
                    case Mood.Jump:
                        {
                            cat.JumpTime += dt;
                            float t = Math.Min(1f, cat.JumpTime / cat.JumpLength);
                            cat.Feet = Vector2.Lerp(cat.JumpFrom, cat.JumpTo, t);
                            if (t >= 1f)
                            {
                                var cb = cat.OnLand; cat.OnLand = null;
                                if (cb != null) cb(); else Decide(cat);
                            }
                            break;
                        }
                    case Mood.Roll:
                        cat.Timer -= dt;
                        if (cat.Timer <= 0)
                        {
                            // Each roll flops the cat over to face the other way and shuffles it along a little.
                            cat.FacingLeft = !cat.FacingLeft;
                            cat.Feet.X += cat.FacingLeft ? -1 : 1;
                            if (--cat.Rolls <= 0) Rest(cat, Mood.Stretch, 1.2f);
                            else cat.Timer = Between(0.35f, 0.7f);
                        }
                        break;
                    case Mood.Scratch:
                        cat.Timer -= dt;
                        // Flecks of bark come off where the claws are.
                        if (_rng.Next(6) == 0)
                        {
                            float px = cat.Feet.X + (cat.FacingLeft ? -6 : 6);
                            _particles.Spawn(new Vector2(px, cat.Feet.Y - 8 - _rng.Next(4)), new Vector2(cat.FacingLeft ? 8 : -8, -10 - _rng.Next(10)), 0.5f, Palette.Tan, 1, 90f);
                        }
                        if (cat.Timer <= 0) Rest(cat, Mood.Sit, Between(1f, 3f));
                        break;
                    case Mood.Sleep:
                        cat.Timer -= dt;
                        if (cat.Timer <= 0) Rest(cat, Mood.Stretch, 1.4f);
                        break;
                    case Mood.Perch:
                        cat.Timer -= dt;
                        if (cat.Timer <= 0)
                        {
                            var cb = cat.OnLand; cat.OnLand = null;
                            if (cb != null) cb(); else Decide(cat);
                        }
                        break;
                    case Mood.Hide:
                        cat.Timer -= dt;
                        if (cat.Timer <= 0) { cat.HiddenEyes = null; Decide(cat); }
                        break;
                    default: // Sit, Stretch
                        cat.Timer -= dt;
                        if (cat.Timer <= 0) Decide(cat);
                        break;
                }
            }
        }

        // ---- Keeping out from under the wizard ---------------------------------------------------

        /// <summary>True when the wizard is about to tread on this cat: it is on the ground and his feet are nearly on top of its own.</summary>
        static bool Underfoot(Cat cat, WizardActor wizard)
        {
            if (wizard == null || wizard.Hidden || wizard.InDoorway) return false;
            if (cat.Mood == Mood.Hide || cat.Mood == Mood.Perch || cat.Mood == Mood.Jump) return false;
            var d = wizard.Feet - cat.Feet;
            return Math.Abs(d.X) < 12 && Math.Abs(d.Y) < 7;
        }

        /// <summary>Hops the cat out of the wizard's way: sideways across his path when he is walking, otherwise straight away from him.</summary>
        void Scoot(Cat cat, WizardActor wizard)
        {
            var away = cat.Feet - wizard.Feet;
            if (away.LengthSquared() < 0.01f) away = new Vector2(cat.FacingLeft ? -1 : 1, 0);
            away.Normalize();
            var side = new Vector2(-away.Y, away.X);
            var meadow = Camera.View; meadow.Inflate(-8, -8);
            // Try beside him first, then behind, then wherever there is room.
            foreach (var dir in new[] { side, -side, away, side + away, -side + away })
            {
                var d = dir; d.Normalize();
                var to = cat.Feet + d * (14 + _rng.Next(6));
                if (Blocked(to) || !meadow.Contains((int)to.X, (int)to.Y)) continue;
                cat.OnArrive = null; cat.HiddenEyes = null;
                Jump(cat, to, null);
                if (_rng.Next(2) == 0 && Camera.View.Contains((int)cat.Feet.X, (int)cat.Feet.Y)) OnMeow?.Invoke();
                return;
            }
        }

        // ---- Drawing ---------------------------------------------------------------------------

        /// <summary>The cats' sun shadows, drawn with the rest of the yard's before anything stands on the grass.</summary>
        public void DrawCastShadows(Canvas c, float shear, float squash, Color color)
        {
            foreach (var cat in _cats)
            {
                if (cat.Mood == Mood.Hide || cat.Mood == Mood.Perch || cat.Mood == Mood.Jump) continue;
                string frame = Frame(cat, out int lift);
                var size = c.Size(frame);
                int x = (int)Math.Round(cat.Feet.X) - size.X / 2;
                c.CastShadow(frame, x, (int)Math.Round(cat.Feet.Y) - 1 - lift, shear, squash, color, cat.FacingLeft);
            }
        }

        public void Draw(Canvas c, bool dark)
        {
            foreach (var cat in _cats)
            {
                if (cat.Mood == Mood.Hide)
                {
                    // Two eyes blinking in the bush; brighter after dark, as cats' eyes are.
                    if (cat.HiddenEyes != null && cat.Anim % 4.1f > 0.18f)
                        foreach (var e in cat.HiddenEyes) c.Rect(e.X, e.Y, 1, 1, dark ? Palette.LightYellow : cat.Eyes);
                    continue;
                }

                string frame = Frame(cat, out int lift);
                var size = c.Size(frame);
                int x = (int)Math.Round(cat.Feet.X) - size.X / 2;
                int y = (int)Math.Round(cat.Feet.Y) - size.Y - lift;
                if (cat.Mood != Mood.Perch && cat.Mood != Mood.Jump)
                    c.GroundShadow((int)Math.Round(cat.Feet.X), size.X - 4, (int)Math.Round(cat.Feet.Y) - 1, cat.Mood == Mood.Sleep ? 0.6f : 1f);
                c.Sprite(frame, x, y, cat.Coat, cat.FacingLeft);

                bool blink = cat.Anim % 3.3f < 0.12f;
                if (!blink && !(cat.Mood == Mood.Perch && cat.Napping))
                    foreach (var e in EyeSpots[frame])
                    {
                        int ex = cat.FacingLeft ? size.X - 1 - e.X : e.X;
                        c.Rect(x + ex, y + e.Y, 1, 1, dark ? Palette.LightYellow : cat.Eyes);
                    }

                if (cat.Mood == Mood.Sit && cat.Anim % 2.3f < 0.35f)
                {
                    // Tail tip flicks up now and then.
                    int tx = cat.FacingLeft ? x : x + size.X - 1;
                    c.Rect(tx, y + size.Y - 3, 1, 1, cat.Coat);
                }
                if (cat.Mood == Mood.Sleep || (cat.Mood == Mood.Perch && cat.Napping))
                    DrawZ(c, x + (cat.FacingLeft ? size.X - 2 : 1), y - 2, cat.Anim);
            }
        }

        /// <summary>A little z that drifts up and fades from a sleeping cat's nose.</summary>
        static void DrawZ(Canvas c, int x, int y, float anim)
        {
            float t = anim % 1.8f / 1.8f;
            int rise = (int)(t * 7);
            var col = Palette.White * (0.8f * (1f - t));
            int zy = y - rise;
            c.Rect(x, zy, 3, 1, col);
            c.Rect(x + 1, zy + 1, 1, 1, col);
            c.Rect(x, zy + 2, 3, 1, col);
        }

        string Frame(Cat cat, out int lift)
        {
            lift = 0;
            switch (cat.Mood)
            {
                case Mood.Walk: return "cat_walk" + ((int)(cat.Anim * 6) % 2);
                case Mood.Jump:
                    {
                        float t = Math.Min(1f, cat.JumpTime / cat.JumpLength);
                        lift = (int)Math.Round(4f * 7f * t * (1f - t));
                        return "cat_jump";
                    }
                case Mood.Roll: return "cat_roll" + ((int)(cat.Anim * 4) % 2);
                case Mood.Scratch: return "cat_scratch" + ((int)(cat.Anim * 5) % 2);
                case Mood.Sleep: return "cat_sleep";
                case Mood.Stretch: return "cat_stretch";
                case Mood.Perch: return cat.Napping ? "cat_sleep" : "cat_loaf";
                default: return "cat_sit";
            }
        }
    }
}
