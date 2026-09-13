using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpiceWizard.Web.Art;

namespace SpiceWizard.Web.Scene
{
    /// <summary>The wizard walks (in a straight line) to whatever was clicked, then the panel opens.</summary>
    public sealed class WizardActor
    {
        public const float Speed = 70f; // virtual pixels per second

        public Vector2 Feet;
        public bool FacingLeft;
        /// <summary>Drawn from behind (walking into the tower).</summary>
        public bool FacingAway;
        /// <summary>Inside the tower: not drawn at all.</summary>
        public bool Hidden;
        /// <summary>Passing through the door: drawn behind the door frame so it hides his shoulders.</summary>
        public bool InDoorway;
        public bool Walking => _target.HasValue;
        /// <summary>Fires once per stride while walking, for footstep sounds.</summary>
        public Action OnStep;
        /// <summary>Fires with the pipe bowl's position each time he blows a puff of smoke.</summary>
        public Action<Vector2> OnPuff;
        /// <summary>Left idle for a while he sometimes gets his pipe out for a few seconds.</summary>
        public bool Smoking => _smoke > 0;
        /// <summary>Pose names for <see cref="Pose"/>: what he acts out after each kind of action.</summary>
        public const string PoseWater = "water", PoseStir = "stir", PoseGrind = "grind", PoseCheer = "cheer";
        /// <summary>Acting something out right now (see <see cref="Pose"/>).</summary>
        public bool Posing => _poseTime > 0 && !Walking && !Hidden && !FacingAway;
        string _pose;
        float _poseTime;
        float _anim;
        int _stride;
        float _smoke;
        float _puffTimer;
        float _idleCheck;
        readonly Random _rng = new Random();
        Vector2? _target;
        Action _onArrive;

        public WizardActor(Point start) { Feet = start.ToVector2(); }

        public void WalkTo(Point stand, Action onArrive)
        {
            var t = stand.ToVector2();
            if (Vector2.Distance(Feet, t) < 2f) { Feet = t; onArrive?.Invoke(); return; }
            _target = t;
            _onArrive = onArrive;
            FacingLeft = t.X < Feet.X;
            _smoke = 0;
            _poseTime = 0;
        }

        /// <summary>
        /// Acts out an action for a few seconds: watering, stirring, grinding or cheering. Plays behind
        /// whatever panel is open and is dropped the moment he walks off. <paramref name="faceLeft"/>
        /// turns him toward the thing he is using; null keeps the way he is facing.
        /// </summary>
        public void Pose(string pose, float seconds, bool? faceLeft = null)
        {
            if (Walking || Hidden) return;
            _pose = pose;
            _poseTime = seconds;
            _smoke = 0;
            if (faceLeft.HasValue) FacingLeft = faceLeft.Value;
        }

        /// <summary>The pose that goes with a successful action's sound, or null when it has none.</summary>
        public static string PoseFor(string sfx)
        {
            switch (sfx)
            {
                case Audio.Sfx.Cook: return PoseStir;
                case Audio.Sfx.Grind:
                case Audio.Sfx.Blend: return PoseGrind;
                default: return null;
            }
        }

        public void Update(float dt)
        {
            _anim += dt;
            if (_poseTime > 0) _poseTime -= dt;
            if (!_target.HasValue) { UpdateIdle(dt); return; }
            var t = _target.Value;
            var delta = t - Feet;
            float step = Speed * dt;
            if (delta.Length() <= step)
            {
                Feet = t;
                _target = null;
                var cb = _onArrive; _onArrive = null;
                cb?.Invoke();
            }
            else
            {
                delta.Normalize();
                Feet += delta * step;
                int stride = (int)(_anim * 8);
                if (stride != _stride) { _stride = stride; OnStep?.Invoke(); }
            }
        }

        /// <summary>Standing about, roughly once every fifteen seconds he lights his pipe and puffs on it for a while.</summary>
        void UpdateIdle(float dt)
        {
            if (Hidden || FacingAway || _poseTime > 0) { _smoke = 0; return; }
            if (_smoke > 0)
            {
                _smoke -= dt;
                _puffTimer -= dt;
                if (_puffTimer <= 0)
                {
                    _puffTimer = 1.1f + (float)_rng.NextDouble() * 0.6f;
                    OnPuff?.Invoke(PipeBowl());
                }
                return;
            }
            _idleCheck += dt;
            if (_idleCheck < 1f) return;
            _idleCheck = 0;
            if (_rng.Next(15) == 0) { _smoke = 5f + (float)_rng.NextDouble() * 4f; _puffTimer = 0.4f; }
        }

        /// <summary>Top of the pipe bowl, in world pixels, where the smoke comes from.</summary>
        Vector2 PipeBowl()
        {
            int x = (int)Math.Round(Feet.X) - 6;
            int y = (int)Math.Round(Feet.Y) - 20;
            return new Vector2(FacingLeft ? x : x + 11, y + 9);
        }

        public void Draw(Canvas c)
        {
            if (Hidden) return;
            // Walking alternates the two stride frames; standing still he just lets his hat tip
            // flop over now and then and blinks every few seconds.
            string frame;
            int hop = 0;      // pixels lifted off the ground (cheering, pounding the pestle)
            int shift = 0;    // columns the sprite extends past the 12px body on the left
            if (Walking) frame = (FacingAway ? "wizard_back" : "wizard") + ((int)(_anim * 8) % 2);
            else if (FacingAway) frame = "wizard_back0";
            else if (Posing) frame = PoseFrame(out hop, out shift);
            else if (Smoking) frame = "wizard_pipe";
            else frame = (int)(_anim / 1.6f) % 2 == 0 ? "wizard0" : "wizard_idle";
            bool blink = !FacingAway && _anim % 3.7f < 0.14f;
            int x = (int)Math.Round(Feet.X) - 6;
            int y = (int)Math.Round(Feet.Y) - 20 - hop;
            c.Rect(x + 1, y + 19 + hop, 10, 2, Palette.Shadow);
            // Wider sprites hang their extra columns off the right; flipped, that extra hangs off the left instead.
            int extra = c.Size(frame).X - 12 - shift;
            c.Sprite(frame, FacingLeft ? x - extra : x - shift, y, Color.White, FacingLeft);
            if (blink) { c.Rect(x + 4, y + 7, 1, 1, Palette.Skin); c.Rect(x + 7, y + 7, 1, 1, Palette.Skin); }
            // The bowl glows orange for a moment as he draws on it, just before each puff.
            if (!Posing && Smoking && _puffTimer < 0.35f) { var b = PipeBowl(); c.Rect((int)b.X, (int)b.Y, 1, 1, Palette.Orange); }
        }

        /// <summary>Which sprite the current pose shows this frame, and how it sits relative to the body.</summary>
        string PoseFrame(out int hop, out int shift)
        {
            hop = 0; shift = 0;
            switch (_pose)
            {
                case PoseWater: return "wizard_water";
                case PoseStir: return "wizard_stir" + ((int)(_anim * 3) % 2);
                case PoseGrind: hop = (int)(_anim * 6) % 2; return "wizard_grind";
                case PoseCheer: shift = 1; hop = (int)(Math.Abs(Math.Sin(_anim * 7)) * 3); return "wizard_cheer";
                default: return "wizard0";
            }
        }
    }

    public struct Particle
    {
        public Vector2 Pos, Vel;
        public float Life, MaxLife;
        public Color Color;
        public int Size;
        public float Gravity;
        /// <summary>Extra pixels of size gained over the particle's life (smoke puffs swell as they rise).</summary>
        public int Grow;
        /// <summary>Vertical flutter, in pixels per second, for leaves tumbling on the wind.</summary>
        public float Wobble;
        /// <summary>Drawn as this sprite (tinted) instead of a square; such particles arc from <see cref="Pos"/> to <see cref="To"/>.</summary>
        public string Sprite;
        public Vector2 From, To;
        public float Arc;
    }

    public sealed class Particles
    {
        readonly List<Particle> _list = new List<Particle>();
        readonly Random _rng = new Random();

        public int Count => _list.Count;
        /// <summary>Sideways push, in pixels per second, on anything light enough to be carried (smoke, steam, leaves).</summary>
        public float Wind;

        public void Spawn(Vector2 pos, Vector2 vel, float life, Color color, int size = 1, float gravity = 0f)
        {
            _list.Add(new Particle { Pos = pos, Vel = vel, Life = life, MaxLife = life, Color = color, Size = size, Gravity = gravity });
        }

        public void Water(Point at)
        {
            for (int i = 0; i < 8; i++)
                Spawn(new Vector2(at.X + _rng.Next(-6, 7), at.Y - 6), new Vector2(_rng.Next(-10, 11), -20 - _rng.Next(20)), 0.5f, Palette.Sky, 1, 120f);
        }

        public void Sparkle(Point at, Color color)
        {
            for (int i = 0; i < 10; i++)
                Spawn(new Vector2(at.X + _rng.Next(-8, 9), at.Y + _rng.Next(-8, 9)), new Vector2(_rng.Next(-8, 9), -10 - _rng.Next(10)), 0.7f, color);
        }

        /// <summary>A ring of sparks bursting outward from a point, for the level-up moment.</summary>
        public void Ring(Point at, Color color)
        {
            for (int i = 0; i < 16; i++)
            {
                double a = i * Math.PI * 2 / 16;
                var dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                Spawn(new Vector2(at.X, at.Y) + dir * 6, dir * (28 + _rng.Next(10)), 0.9f, i % 2 == 0 ? color : Palette.White, i % 3 == 0 ? 2 : 1);
            }
        }

        /// <summary>A freshly bottled sauce that pops up out of the cauldron and lobs over to the pantry.</summary>
        public void Bottle(Point from, Point to, Color tint)
        {
            var p = new Particle
            {
                Pos = from.ToVector2(),
                From = from.ToVector2(),
                To = to.ToVector2(),
                Life = 1.1f,
                MaxLife = 1.1f,
                Color = tint,
                Sprite = "bottle",
                Arc = 34f,
            };
            _list.Add(p);
        }

        public void Steam(Point at)
        {
            Spawn(new Vector2(at.X + _rng.Next(-8, 9), at.Y), new Vector2(_rng.Next(-4, 5), -12), 1.2f, Palette.LightGrey * 0.7f, 2);
        }

        /// <summary>A fat, slow puff of wood smoke from the fire that swells and thins as it climbs past the cauldron.</summary>
        public void Smoke(Point at)
        {
            var p = new Particle
            {
                Pos = new Vector2(at.X + _rng.Next(-3, 4), at.Y),
                Vel = new Vector2(3 + _rng.Next(0, 5), -14 - _rng.Next(6)),
                Life = 3.6f + (float)_rng.NextDouble() * 1.2f,
                Color = Palette.Grey * 0.6f,
                Size = 3,
                Grow = 5,
            };
            p.MaxLife = p.Life;
            _list.Add(p);
        }

        /// <summary>A wisp of pipe smoke: one small ring that drifts up and fattens a little before it fades.</summary>
        public void Puff(Vector2 at)
        {
            for (int i = 0; i < 2; i++)
            {
                var p = new Particle
                {
                    Pos = new Vector2(at.X + _rng.Next(-1, 2), at.Y - 1 - i),
                    Vel = new Vector2(_rng.Next(-3, 4), -7 - _rng.Next(5)),
                    Life = 1.6f + (float)_rng.NextDouble() * 0.8f,
                    Color = Palette.LightGrey * 0.75f,
                    Size = 1,
                    Grow = 2,
                };
                p.MaxLife = p.Life;
                _list.Add(p);
            }
        }

        /// <summary>A leaf torn loose somewhere off the left edge, to tumble the width of the window on the wind.</summary>
        public void Leaf(int left, int top, int bottom)
        {
            Color[] colors = { Palette.LightGreen, Palette.Tan, Palette.Yellow, Palette.LightTan, Palette.Orange };
            var p = new Particle
            {
                Pos = new Vector2(left - 4, _rng.Next(top, bottom)),
                Vel = new Vector2(40 + _rng.Next(35), -6 + _rng.Next(14)),
                Life = 14f,
                Color = colors[_rng.Next(colors.Length)],
                Size = _rng.Next(4) == 0 ? 1 : 2,
                Gravity = 4f,
                Wobble = 10f + _rng.Next(12),
            };
            p.MaxLife = p.Life;
            _list.Add(p);
        }

        public void Confetti(int left, int right, int top)
        {
            Color[] colors = { Palette.Red, Palette.Yellow, Palette.LightGreen, Palette.Sky, Palette.Pink, Palette.LightPurple };
            Spawn(new Vector2(_rng.Next(left, right), top + 10), new Vector2(_rng.Next(-15, 16), 20 + _rng.Next(25)), 4f, colors[_rng.Next(colors.Length)], 2, 10f);
        }

        public void Update(float dt)
        {
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var p = _list[i];
                p.Life -= dt;
                if (p.Life <= 0) { _list.RemoveAt(i); continue; }
                if (p.Sprite != null)
                {
                    // Sprite particles fly a fixed lob from one point to another rather than under gravity.
                    float t = 1f - p.Life / p.MaxLife;
                    p.Pos = Vector2.Lerp(p.From, p.To, t);
                    p.Pos.Y -= (float)Math.Sin(t * Math.PI) * p.Arc;
                    _list[i] = p;
                    continue;
                }
                p.Vel.Y += p.Gravity * dt;
                p.Pos += p.Vel * dt;
                if (p.Gravity < 5f) p.Pos.X += Wind * dt;
                if (p.Wobble > 0) p.Pos.Y += (float)Math.Sin(p.Life * 5f) * p.Wobble * dt;
                _list[i] = p;
            }
        }

        public void Draw(Canvas c)
        {
            foreach (var p in _list)
            {
                if (p.Sprite != null)
                {
                    var size0 = c.Size(p.Sprite);
                    c.Rect((int)p.Pos.X - 2, (int)p.To.Y - 1, 5, 2, Palette.Shadow);
                    c.Sprite(p.Sprite, (int)p.Pos.X - size0.X / 2, (int)p.Pos.Y - size0.Y, p.Color);
                    continue;
                }
                // Growing particles (smoke) thin out steadily as they swell; the rest hold full until half-life.
                float a = p.Grow > 0 ? p.Life / p.MaxLife : Math.Min(1f, p.Life / p.MaxLife * 2f);
                int size = p.Size + (int)(p.Grow * (1f - p.Life / p.MaxLife));
                c.Rect((int)p.Pos.X - (size - p.Size) / 2, (int)p.Pos.Y, size, size, p.Color * a);
            }
        }
    }

    /// <summary>Someone on foot: walks in a straight line to <see cref="Target"/>, hopping along like the crowd does.</summary>
    public struct Walker
    {
        public Vector2 Feet, Target;
        public bool FacingLeft;
        public float Anim;
        public int Sprite;

        public bool Arrived => Vector2.DistanceSquared(Feet, Target) < 1f;

        public void Update(float dt, float speed)
        {
            Anim += dt;
            var d = Target - Feet;
            float step = speed * dt;
            if (d.Length() <= step) { Feet = Target; return; }
            d.Normalize();
            Feet += d * step;
            if (Math.Abs(d.X) > 0.2f) FacingLeft = d.X < 0;
        }

        public void Draw(Canvas c)
        {
            int hop = Arrived ? 0 : (int)(Math.Abs(Math.Sin(Anim * 6)) * 2);
            int x = (int)Math.Round(Feet.X), y = (int)Math.Round(Feet.Y);
            c.Rect(x - 3, y - 2, 7, 2, Palette.Shadow);
            c.Sprite("townsfolk" + Sprite, x - 5, y - 16 - hop, Color.White, FacingLeft);
        }
    }

    /// <summary>
    /// Two villagers who come up the road at dawn, after a night the crate went to town, and carry
    /// it off between them. Pure show: the sale itself happened at the day tick.
    /// </summary>
    public sealed class Villagers
    {
        const float Speed = 45f;
        Walker _a, _b;
        // 0 idle, 1 along the road, 2 down to the crate, 3 lifting, 4 back up to the road, 5 away down the road.
        int _phase;
        float _timer;
        static readonly Random Rng = new Random(23);

        public bool Active => _phase != 0;

        public void Start()
        {
            int road = Layout.RoadBottom - 4;
            _a = new Walker { Feet = new Vector2(Camera.Left - 12, road), Sprite = Rng.Next(3) };
            _b = new Walker { Feet = new Vector2(Camera.Left - 26, road + 2), Sprite = (_a.Sprite + 1 + Rng.Next(2)) % 3 };
            _a.Target = new Vector2(Layout.Crate.X - 8, road);
            _b.Target = new Vector2(Layout.Crate.X + 26, road + 2);
            _phase = 1;
        }

        public void Stop() { _phase = 0; }

        public void Update(float dt)
        {
            if (_phase == 0) return;
            if (_phase == 3)
            {
                _timer -= dt;
                if (_timer > 0) return;
                _a.Target = new Vector2(_a.Feet.X, Layout.RoadBottom - 4);
                _b.Target = new Vector2(_b.Feet.X, Layout.RoadBottom - 2);
                _phase = 4;
            }
            _a.Update(dt, Speed);
            _b.Update(dt, Speed);
            if (!_a.Arrived || !_b.Arrived) return;
            switch (_phase)
            {
                case 1:
                    _a.Target = new Vector2(Layout.Crate.X - 8, Layout.Crate.Y + 20);
                    _b.Target = new Vector2(Layout.Crate.X + 26, Layout.Crate.Y + 18);
                    _phase = 2;
                    break;
                case 2:
                    _phase = 3;
                    _timer = 0.8f;
                    _a.FacingLeft = false;
                    _b.FacingLeft = true;
                    break;
                case 4:
                    _a.Target = new Vector2(Camera.Left - 30, Layout.RoadBottom - 4);
                    _b.Target = new Vector2(Camera.Left - 14, Layout.RoadBottom - 2);
                    _phase = 5;
                    break;
                case 5:
                    _phase = 0;
                    break;
            }
        }

        public void Draw(Canvas c)
        {
            if (_phase == 0) return;
            _b.Draw(c);
            // On the way back the crate swings between them at waist height.
            if (_phase >= 4)
            {
                int mx = (int)Math.Round((_a.Feet.X + _b.Feet.X) / 2), my = (int)Math.Round(Math.Max(_a.Feet.Y, _b.Feet.Y));
                c.Sprite("crate_full", mx - 9, my - 15);
            }
            _a.Draw(c);
        }
    }

    /// <summary>Townsfolk who walk in along the road for the celebration and mill about.</summary>
    public sealed class Crowd
    {
        struct Person { public Vector2 Pos; public Vector2 Target; public int Sprite; public float Bob; }
        readonly List<Person> _people = new List<Person>();
        readonly Random _rng = new Random(7);
        float _spawnTimer;
        public bool Active;

        public void Start()
        {
            Active = true;
            _people.Clear();
            _spawnTimer = 0;
        }

        public void Stop() { Active = false; _people.Clear(); }

        public void Update(float dt)
        {
            if (!Active) return;
            _spawnTimer -= dt;
            if (_people.Count < 12 && _spawnTimer <= 0)
            {
                _spawnTimer = 0.35f;
                _people.Add(new Person
                {
                    Pos = new Vector2(-12, Layout.RoadBottom - 4 + _rng.Next(-3, 4)),
                    Target = new Vector2(150 + _rng.Next(120), 118 + _rng.Next(80)),
                    Sprite = _rng.Next(3),
                    Bob = (float)_rng.NextDouble() * 6f,
                });
            }
            for (int i = 0; i < _people.Count; i++)
            {
                var p = _people[i];
                p.Bob += dt;
                var d = p.Target - p.Pos;
                if (d.Length() > 1f) { d.Normalize(); p.Pos += d * 45f * dt; }
                else if (_rng.Next(200) == 0) p.Target = new Vector2(150 + _rng.Next(120), 118 + _rng.Next(80));
                _people[i] = p;
            }
        }

        public void Draw(Canvas c)
        {
            if (!Active) return;
            foreach (var p in _people)
            {
                int hop = (int)(Math.Abs(Math.Sin(p.Bob * 6)) * 2);
                c.Rect((int)p.Pos.X - 3, (int)p.Pos.Y - 2, 7, 2, Palette.Shadow);
                c.Sprite("townsfolk" + p.Sprite, (int)p.Pos.X - 5, (int)p.Pos.Y - 16 - hop);
            }
        }
    }
}
