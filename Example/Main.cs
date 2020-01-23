using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        private const string Path = "Example/Res/T.txt";

        static void Main(string[] args) {
            var sourceMap = getSource();
            // var outputSize = new Vec2(4, 4);
            var outputSize = new Vec2(36, 36);

            WfcContext cx = new WfcContext(sourceMap, 3, outputSize);
            debugPrintInput(cx);

            // run until succeed
            while (!cx.run()) {
                cx = new WfcContext(sourceMap, 3, outputSize);
            }
            debugPrintOutput(cx);
            var output = cx.getOutput();
            Wfc.Segments.Circle.print(ref output);

            // try until solve

            // while (true) {
            //     bool didSuccess = false;
            //     foreach(var status in cx.runIter()) {
            //         debugPrintOutput(cx);
            //         cx.state.printAvaiablePatternCounts(outputSize, cx.model.patterns.len);
            //         switch (status) {
            //             case WfcContext.AdvanceStatus.Continue:
            //                 continue; // advance WFC
            //             case WfcContext.AdvanceStatus.Success:
            //                 didSuccess = true;
            //                 break; // finish
            //             case WfcContext.AdvanceStatus.Fail:
            //                 break; // retry
            //         }
            //         break;
            //     }
            //     if (didSuccess) break;
            //     System.Console.WriteLine($"=== RETRY ===");
            //     System.Console.WriteLine($"=== RETRY ===");
            //     System.Console.WriteLine($"=== RETRY ===");
            //     cx = new WfcContext(sourceMap, 3, outputSize);
            // }

            Test.testEveryRow(cx.state, ref cx.model.rule, cx.model.patterns);
            Test.testEveryColumn(cx.state, ref cx.model.rule, cx.model.patterns);
        }

        static string nl => System.Environment.NewLine;

        static Map getSource() {
            // var sourceMap = loadAsciiMap("Example/Res/a.txt", new Vec2(6, 6));
            // var sourceMap = loadAsciiMap("Example/Res/c.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/wide.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/rect.txt", new Vec2(9, 9));
            var sourceMap = loadAsciiMap("Example/Res/rooms.txt", new Vec2(16, 16));
            // var sourceMap = loadAsciiMap("Example/Res/curve.txt", new Vec2(7, 7));
            // bug: no enabler in some direction
            // var sourceMap = loadAsciiMap("Example/Res/test.txt", new Vec2(6, 6));
            // var sourceMap = loadAsciiMap(Path, new Vec2(7, 7));
            return sourceMap;
        }

        static Map loadAsciiMap(string path, Vec2 inputSize) {
            string asciiMap = File.ReadAllText(path);
            return MapExt.fromString(asciiMap, inputSize.x, inputSize.y);
        }

        static void debugPrintInput(WfcContext cx) {
            // print the extracted patterns
            Console.WriteLine("======================");
            cx.model.patterns.print();
            Console.WriteLine("======================");

            // print initial enabler counts
            var state = new State(50, 50, cx.model.patterns, ref cx.model.rule);
            Test.printInitialEnableCounter(50, 50, cx.model.patterns, ref cx.model.rule);

            // print input map
            Console.WriteLine($@"=== Source map==={nl}");
            cx.model.input.source.print();
            Console.WriteLine("");

            // print adjacency rules over the extracted patterns
            // cx.model.rule.print(cx.model.patterns.len);
        }

        static void debugPrintOutput(WfcContext cx) {
            var model = cx.model;
            var input = model.input;
            var output = cx.state.getOutput(input.outputSize.x, input.outputSize.y, cx.model.input.source, input.N, cx.model.patterns);

            Console.WriteLine("=== Output: ===");
            output.print();
        }
    }
}