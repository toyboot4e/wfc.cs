using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc {
    public static class TileExt {
        public static char toChar(this Tile self) {
            switch (self) {
                case Tile.None:
                    return ' ';
                case Tile.Wall:
                    return '#';
                case Tile.Floor:
                    return '.';
                case Tile.DownStair:
                    return '>';
                case Tile.UpStair:
                    return '<';
                default:
                    throw new System.Exception("error in Tile.toChar()");
            }
        }

        public static Tile charToTile(char c) {
            switch (c) {
                case ' ':
                    return Tile.None;
                case '#':
                    return Tile.Wall;
                case '.':
                    return Tile.Floor;
                case '>':
                    return Tile.DownStair;
                case '<':
                    return Tile.UpStair;
                default:
                    throw new System.Exception("error in TileExt.charToTile()");
            }
        }
    }

    public static class MapExt {
        public static Map fromString(string asciiMap, int width, int height) {
            var self = Map.withItems(width, height);

            using(StringReader reader = new StringReader(asciiMap)) {
                string buf;
                int y = -1;
                while ((buf = reader.ReadLine()) != null) {
                    y += 1;
                    for (int x = 0; x < width; x++) {
                        self[x, y] = TileExt.charToTile(buf[x]);
                    }
                }
            }

            return self;
        }

        public static void print(this Map self) {
            for (int y = 0; y < self.height; y++) {
                for (int x = 0; x < self.width; x++) {
                    Console.Write(self[x, y].toChar());
                }
                Console.WriteLine();
            }
        }
    }

    public static class PatternExt {
        public static void print(this Pattern self, Map source, int N) {
            for (int j = 0; j < N; j++) {
                for (int i = 0; i < N; i++) {
                    var pos = self.offset + self.variant.apply(new Vec2i(i, j), N);
                    Console.Write(source[pos.x, pos.y].toChar());
                }
                Console.WriteLine();
            }
        }
    }

    public static class PatternStorageExt {
        public static void print(this PatternStorage self) {
            for (int i = 0; i < self.len; i++) {
                var pattern = self[i];
                pattern.print(self.source, self.N);
                Console.WriteLine($"{i} (weight = {pattern.weight})");
                Console.WriteLine();
            }

            var n = self.len;
            Console.WriteLine($" { n } pattterns found ");
        }
    }

    public static class AdjacencyRuleExt {
        public static void print(this Rule self, int nPatterns) {
            for (int from = 0; from < self.nPatterns; from++) {
                for (int to = from; to < self.nPatterns; to++) {
                    for (int d = 0; d < 4; d++) {
                        if (!self.canOverlap(new PatternId(from), (Dir4) d, new PatternId(to))) continue;
                        Console.WriteLine($"legal: {from} to {to} in {(Dir4) d}");
                    }
                }
            }
        }
    }
}