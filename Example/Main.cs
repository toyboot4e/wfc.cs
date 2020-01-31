using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        private const string Path = "Example/Res/T.txt";

        static void Main(string[] args) {
            var source = getSourceMap(); // hard coded!
            var outputSize = new Vec2i(36, 36);

            var wfc = WfcAdjacency.create(ref source, 3, outputSize);
            // var wfc = WfcOverlap.create(ref source, 3, outputSize);
            debugPrintInput(wfc, ref source);

            while (!wfc.run()) {
                wfc = WfcAdjacency.create(ref source, 3, outputSize);
                // wfc = WfcOverlap.create(ref source, 3, outputSize);
            }

            var output = wfc.getOutput(ref source);
            debugPrintOutput(ref output);

            // make sure the output is fine
            Test.testEveryRow(wfc.state, ref wfc.model.rule, wfc.model.patterns);
            Test.testEveryColumn(wfc.state, ref wfc.model.rule, wfc.model.patterns);
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
            Console.WriteLine($"=== Output ({output.width}x{output.height}) ===");
            output.print();
            Console.WriteLine("");
            Console.WriteLine("=== Output in a circle: ===");
            Wfc.Segments.Circle.print(ref output);
            Console.WriteLine("");
        }
    }
}