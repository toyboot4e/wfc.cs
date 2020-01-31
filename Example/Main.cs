using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        private const string Path = "Example/Res/T.txt";

        static void Main(string[] args) {
            var source = getSourceMap(); // hard coded!
            var outputSize = new Vec2i(36, 36);

            var cx = WfcAdjacency.create(ref source, 3, outputSize);
            debugPrintInput(cx, ref source);

            while (!cx.run()) {
                cx = WfcAdjacency.create(ref source, 3, outputSize);
            }

            var output = cx.getOutput(ref source);
            debugPrintOutput(ref output);

            // make sure the output is fine
            Test.testEveryRow(cx.state, ref cx.model.rule, cx.model.patterns);
            Test.testEveryColumn(cx.state, ref cx.model.rule, cx.model.patterns);
        }

        static string nl => System.Environment.NewLine;

        static Map getSourceMap() {
            var path = loadAsciiMap("Example/Res/rooms.txt", inputSize : new Vec2i(16, 16));
            return path;
            // var sourceMap = loadAsciiMap("Example/Res/a.txt", new Vec2(6, 6));
            // var sourceMap = loadAsciiMap("Example/Res/c.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/wide.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/rect.txt", new Vec2(9, 9));
            // var sourceMap = loadAsciiMap("Example/Res/curve.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/test.txt", new Vec2(6, 6));
        }

        static Map loadAsciiMap(string path, Vec2i inputSize) {
            string asciiMap = File.ReadAllText(path);
            return MapExt.fromString(asciiMap, inputSize.x, inputSize.y);
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
            Console.WriteLine("=== Output: ===");
            output.print();
            Console.WriteLine("");
            Console.WriteLine("=== Output in a circle: ===");
            Wfc.Segments.Circle.print(ref output);
            Console.WriteLine("");
        }
    }
}