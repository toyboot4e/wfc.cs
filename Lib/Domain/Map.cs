namespace Wfc {
    /// <summary>
    /// Fills a cell in a <c>Map</c>. Has size of 1x1. Corresponds to a pixel in "texture mode"
    /// </summary>
    public enum Tile {
        None = 0,
        Wall = 1,
        Floor = 2,
        DownStair = 3,
        UpStair = 4,
    }

    /// <summary>Grid/cells of tiles</sumary>
    public class Map {
        public Grid2D<Tile> tiles;
        public int width;
        public int height;

        /// <summary>Creates a <c>Map</c> with capacity (w * h)</summary>
        /// <remark>Never forget to <c>add</c> <c>Tile</c>s before accessing <c>tiles</c></remark>
        public Map(int w, int h) {
            this.width = w;
            this.height = h;
            this.tiles = new Grid2D<Tile>(w, h);
        }

        public static Map withItems(int w, int h) {
            var map = new Map(w, h);
            for (int i = 0; i < w * h; i++) {
                map.tiles.add(Tile.None);
            }
            return map;
        }

        public Tile this[int x, int y] {
            get => this.tiles[x, y];
            set => this.tiles[x, y] = value;
        }

        public Tile this[Vec2i v] {
            get => this.tiles[v.x, v.y];
            set => this.tiles[v.x, v.y] = value;
        }

        public void add(Tile tile) {
            this.tiles.add(tile);
        }
    }
}