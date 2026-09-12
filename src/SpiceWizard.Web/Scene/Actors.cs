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
        float _anim;
        int _stride;
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
        }

        public void Update(float dt)
        {
            _anim += dt;
            if (!_target.HasValue) return;
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

        public void Draw(Canvas c)
        {
            if (Hidden) return;
            // Walking alternates the two stride frames; standing still he just lets his hat tip
            // flop over now and then and blinks every few seconds.
            string frame;
            if (Walking) frame = (FacingAway ? "wizard_back" : "wizard") + ((int)(_anim * 8) % 2);
            else if (FacingAway) frame = "wizard_back0";
            else frame = (int)(_anim / 1.6f) % 2 == 0 ? "wizard0" : "wizard_idle";
            bool blink = !FacingAway && _anim % 3.7f < 0.14f;
            int x = (int)Math.Round(Feet.X) - 6;
            int y = (int)Math.Round(Feet.Y) - 20;
            c.Rect(x + 1, y + 19, 10, 2, Palette.Shadow);
            c.Sprite(frame, x, y, Color.White, FacingLeft);
            if (blink) { c.Rect(x + 4, y + 7, 1, 1, Palette.Skin); c.Rect(x + 7, y + 7, 1, 1, Palette.Skin); }
        }
    }

    public struct Particle
    {
        public Vector2 Pos, Vel;
        public float Life, MaxLife;
        public Color Color;
        public int Size;
        public float Gravity;
    }

    public sealed class Particles
    {
        readonly List<Particle> _list = new List<Particle>();
        readonly Random _rng = new Random();

        public int Count => _list.Count;

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

        public void Steam(Point at)
        {
            Spawn(new Vector2(at.X + _rng.Next(-8, 9), at.Y), new Vector2(_rng.Next(-4, 5), -12), 1.2f, Palette.LightGrey * 0.7f, 2);
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
                p.Vel.Y += p.Gravity * dt;
                p.Pos += p.Vel * dt;
                _list[i] = p;
            }
        }

        public void Draw(Canvas c)
        {
            foreach (var p in _list)
            {
                float a = Math.Min(1f, p.Life / p.MaxLife * 2f);
                c.Rect((int)p.Pos.X, (int)p.Pos.Y, p.Size, p.Size, p.Color * a);
            }
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
