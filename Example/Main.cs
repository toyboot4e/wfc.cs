using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        static void Main(string[] args) {
            var sourceMap = getSource();

            WfcContext cx;
            bool isFirst = true;
            while (true) {
                cx = new WfcContext(sourceMap, 3, new Vec2(50, 50));
                if (isFirst) {
                    // debugPrintA(cx);
                }
                if (cx.run()) break;
                isFirst = false;
            }
            debugPrintB(cx);
        }

        static string nl => System.Environment.NewLine;

        static Map getSource() {
            // var sourceMap = loadAsciiMap("Example/Res/a.txt", new Vec2(6, 6));
            // var sourceMap = loadAsciiMap("Example/Res/c.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/wide.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/rect.txt", new Vec2(9, 9));
            // var sourceMap = loadAsciiMap("Example/Res/curve.txt", new Vec2(7, 7));
            var sourceMap = loadAsciiMap("Example/Res/T.txt", new Vec2(7, 7));
            return sourceMap;
        }

        static void debugPrintA(WfcContext cx) {
            Console.WriteLine("======================");
            cx.model.patterns.print();
            Console.WriteLine("======================");
            cx.model.rule.print(cx.model.patterns.len);

            var state = new State(50, 50, cx.model.patterns, ref cx.model.rule);
            Test.printInitialEnableCounter(50, 50, cx.model.patterns, ref cx.model.rule);
        }

        static void debugPrintB(WfcContext cx) {
            var model = cx.model;
            var input = model.input;
            var output = cx.state.getOutput(input.outputSize.x, input.outputSize.y, cx.model.input.source, input.N, cx.model.patterns);

            {
                Console.WriteLine("=== Output: ===");
                output.print();
            }
        }

        static Map loadAsciiMap(string path, Vec2 inputSize) {
            string asciiMap = File.ReadAllText(path);
            Console.WriteLine($@"=== Source map ==={nl}{asciiMap}{nl}");
            return MapExt.fromString(asciiMap, inputSize.x, inputSize.y);
        }
    }
}