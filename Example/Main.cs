using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        static void Main(string[] args) {
            // var path = "Example/Res/curve.txt";
            // var path = "Example/Res/rooms.txt";
            // var path = "Example/Res/Adjacency/3x3_rooms.txt";
            // var path = "Example/Res/Adjacency/3x3_corridors.txt";
            var path = "Example/Res/Adjacency/6x6_rooms.txt";
            var outputSize = new Vec2i(12, 12);
            int N = 3;

            var source = loadAsciiMap(path);

            var wfc = WfcAdjacency.create(ref source, N, outputSize);
            // var wfc = WfcOverlap.create(ref source, 3, outputSize);
            debugPrintInput(wfc, ref source);

            while (!wfc.run()) {
                wfc = WfcAdjacency.create(ref source, N, outputSize);
                // wfc = WfcOverlap.create(ref source, N, outputSize);
            }

            var output = wfc.getOutput(ref source);
            debugPrintOutput(ref output);

            // make sure the output is fine
            Test.testEveryRow(wfc.state, ref wfc.model.rule, wfc.model.patterns);
            Test.testEveryColumn(wfc.state, ref wfc.model.rule, wfc.model.patterns);
        }

        static string nl => System.Environment.NewLine;

        static Map loadAsciiMap(string path) {
            string asciiMap = File.ReadAllText(path);
            var width = asciiMap.IndexOf(Environment.NewLine, 0);
            var height = lineCount(asciiMap);
            return MapExt.fromString(asciiMap, width, height);

            static int lineCount(string s) {
                int count = 1;
                int start = 0;
                while ((start = s.IndexOf('\n', start)) != -1) {
                    count++;
                    start++;
                }
                return count;
            }
        }

        static void debugPrintInput(WfcContext cx, ref Map source) {
            // print the extracted patterns
            Console.WriteLine("======================");
            cx.model.patterns.print();
            Console.WriteLine("======================");

            // print initial enabler counts
            var state = new State(50, 50, cx.model.patterns, ref cx.model.rule);
            Test.printInitialEnableCounter(50, 50, cx.model.patterns, ref cx.model.rule);

            // print input map
            Console.WriteLine($@"=== Source map==={nl}");
            source.print();
            Console.WriteLine("");

            // print adjacency rules over the extracted patterns
            // cx.model.rule.print(cx.model.patterns.len);
        }

        static void debugPrintOutput(ref Map output) {
            Console.WriteLine($"=== Output ({output.width}x{output.height}) ===");
            output.print();
            Console.WriteLine("");
            Console.WriteLine("=== Output in a circle: ===");
            Wfc.Segments.Circle.print(ref output);
            Console.WriteLine("");
        }
    }
}