using Microsoft.Xna.Framework;

namespace SpiceWizard.Web.Scene
{
    public enum StationKind { Plot, Well, Board, Market, Cauldron, Shelf, Mortar, Pantry, Crate, Door }

    /// <summary>A clickable thing in the yard: where it is, and where the wizard stands to use it.</summary>
    public sealed class Station
    {
        public StationKind Kind;
        public int Index;            // plot number for Plot stations
        public Rectangle Bounds;     // click area
        public Point Stand;          // the wizard's feet when using it
        public string Name;
        public string Hint;

        public Station(StationKind kind, int index, Rectangle bounds, Point stand, string name, string hint)
        {
            Kind = kind; Index = index; Bounds = bounds; Stand = stand; Name = name; Hint = hint;
        }
    }

    /// <summary>Fixed positions of everything on the single screen (384x216 virtual pixels).</summary>
    public static class Layout
    {
        public const int HudHeight = 14;
        public const int Horizon = 92;
        public const int RoadTop = 96;
        public const int RoadBottom = 108;

        public static readonly Rectangle Tower = new Rectangle(300, 50, 48, 120);
        public static readonly Point Roof = new Point(300, 27);
        public static readonly Point Door = new Point(317, 144);
        public static readonly Point[] Windows = { new Point(310, 66), new Point(330, 82) };

        public static readonly Point CartPark = new Point(100, 80);
        public static readonly Point Merchant = new Point(140, 84);
        public static readonly Point Board = new Point(166, 106);
        public static readonly Point Well = new Point(126, 140);
        public static readonly Point Bucket = new Point(148, 158);
        public static readonly Point Cauldron = new Point(262, 174);
        public static readonly Point Shelf = new Point(302, 100);
        public static readonly Point Stump = new Point(356, 186);
        public static readonly Point Mortar = new Point(358, 176);
        public static readonly Point Pantry = new Point(354, 152);
        public static readonly Point Crate = new Point(236, 150);
        public static readonly Point WizardStart = new Point(200, 178);

        public static readonly Point[] Trees = { new Point(8, 104), new Point(222, 100), new Point(370, 88) };
        public static readonly Point[] Bushes = { new Point(62, 122), new Point(196, 198), new Point(284, 150) };

        public const int PlotCols = 4;
        public const int PlotRows = 2;
        public const int PlotX = 14;
        public const int PlotY = 150;
        public const int PlotStepX = 26;
        public const int PlotStepY = 20;

        public static Point PlotPos(int index) =>
            new Point(PlotX + (index % PlotCols) * PlotStepX, PlotY + (index / PlotCols) * PlotStepY);

        public static readonly Station[] Stations = Build();

        static Station[] Build()
        {
            var list = new System.Collections.Generic.List<Station>();
            for (int i = 0; i < PlotCols * PlotRows; i++)
            {
                var p = PlotPos(i);
                list.Add(new Station(StationKind.Plot, i, new Rectangle(p.X, p.Y - 10, 24, 26), new Point(p.X + 12, p.Y + 20), "Plot " + (i + 1), "Garden plot"));
            }
            list.Add(new Station(StationKind.Well, 0, new Rectangle(Well.X, Well.Y, 20, 26), new Point(Well.X + 26, Well.Y + 30), "Well", "Refill the bucket"));
            list.Add(new Station(StationKind.Board, 0, new Rectangle(Board.X, Board.Y, 20, 24), new Point(Board.X + 10, Board.Y + 30), "Notice board", "This week's quota"));
            list.Add(new Station(StationKind.Market, 0, new Rectangle(CartPark.X, CartPark.Y, 54, 24), new Point(CartPark.X + 30, RoadBottom + 6), "Merchant", "Buy seeds and spices"));
            list.Add(new Station(StationKind.Cauldron, 0, new Rectangle(Cauldron.X, Cauldron.Y - 6, 24, 30), new Point(Cauldron.X - 6, Cauldron.Y + 24), "Cauldron", "Cook sauces"));
            list.Add(new Station(StationKind.Shelf, 0, new Rectangle(Shelf.X, Shelf.Y, 44, 22), new Point(Shelf.X + 22, Tower.Bottom + 10), "Jar shelf", "Ferment peppers"));
            list.Add(new Station(StationKind.Mortar, 0, new Rectangle(Stump.X, Mortar.Y, 18, 20), new Point(Stump.X - 6, Stump.Y + 12), "Mortar", "Grind peppers, mix blends"));
            list.Add(new Station(StationKind.Pantry, 0, new Rectangle(Pantry.X, Pantry.Y, 18, 14), new Point(Pantry.X - 6, Pantry.Y + 18), "Pantry", "Your ingredients"));
            list.Add(new Station(StationKind.Crate, 0, new Rectangle(Crate.X, Crate.Y, 18, 14), new Point(Crate.X + 24, Crate.Y + 18), "Shipping crate", "Send sauces to town"));
            list.Add(new Station(StationKind.Door, 0, new Rectangle(Door.X, Door.Y, 14, 26), new Point(Door.X + 7, Tower.Bottom + 8), "Tower door", "Go to bed"));
            return list.ToArray();
        }

        public static Station Find(StationKind kind, int index = 0)
        {
            foreach (var s in Stations)
                if (s.Kind == kind && s.Index == index) return s;
            return null;
        }
    }
}
