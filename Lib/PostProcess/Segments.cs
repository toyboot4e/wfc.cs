using System;

// use segments to filter output
namespace Wfc.Segments {
    public class Circle {
        public static void print(ref Map map) {
            var center = new Vec2(map.width / 2, map.height / 2);
            var r = Math.Min(center.x, center.y) - 0.5f;
            for (int y = 0; y < map.height; y++) {
                for (int x = 0; x < map.width; x++) {
                    var d = (new Vec2(x, y) - center).distanceInt();
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