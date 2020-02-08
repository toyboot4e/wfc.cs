using System;

// use segments to filter output
namespace Wfc.Segments {
    public class Circle {
        public static void print(ref Map map) {
            // e.g. (3, 3) -> (1,1), (4, 4) -> (1.5, 1.5)
            var center = ((map.width - 1) / 2.0, (map.height - 1) / 2.0);
            var r = Math.Min(center.Item1, center.Item2) + 0.5;
            for (int y = 0; y < map.height; y++) {
                for (int x = 0; x < map.width; x++) {
                    var delta = (center.Item1 - x, center.Item2 - y);
                    var d = Math.Sqrt(delta.Item1 * delta.Item1 + delta.Item2 * delta.Item2);
                    if (d <= r) {
                        System.Console.Write(map[x, y].toChar());
                    } else {
                        System.Console.Write(" ");
                    }
                }
                System.Console.WriteLine("");
            }
        }

        public static void printOdd(ref Map map) {
            // e.g. (3, 3) -> (1,1), (4, 4) -> (1, 1), (5, 5) -> (2, 2)
            var center = new Vec2i((map.width - 1) / 2, (map.height - 1) / 2);
            var r = Math.Min(center.x, center.y) + 0.5;
            System.Console.WriteLine($"{center}, {r}");
            for (int y = 0; y < map.height; y++) {
                for (int x = 0; x < map.width; x++) {
                    var d = (new Vec2i(x, y) - center).distanceD();
                    if (d <= r) {
                        System.Console.Write(map[x, y].toChar());
                    } else {
                        System.Console.Write(" ");
                    }
                }
                System.Console.WriteLine("");
            }
        }
    }
}