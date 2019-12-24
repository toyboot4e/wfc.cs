namespace Wfc {
    /// <summary>
    /// Fills a cell in a map. Has size of 1x1. Chunk of tiles is a <c>Map</c> or a <c>Pattern</c></summary>
    public enum Tile {
        None = 0,
        Wall = 1,
        Floor = 2,
        DownStair = 3,
        UpStair = 4,
    }

    /// <summary>Two-dimensional array of <c>Tile</c>s. Used for both input and output of WFC.</summary>
    /// <remark>The domain</summary>
    public class Map {
        public Array2D<Tile> tiles;
        public int width;
        public int height;

        public Map(int w, int h) {
            this.width = w;
            this.height = h;
            this.tiles = new Array2D<Tile>(w, h);
            for (int i = 0; i < w * h; i++) {
                this.tiles.add(Tile.None);
            }
        }

        public Tile this[int x, int y] {
            get => this.tiles.get(x, y);
            set => this.tiles.set(x, y, value);
        }
    }
}