using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        private const string Path = "Example/Res/T.txt";

        static void Main(string[] args) {
            var sourceMap = getSource(); // hard coded!
            var outputSize = new Vec2(36, 36);

            WfcContext cx = new WfcContext(sourceMap, 3, outputSize);
            debugPrintInput(cx);

            // run until succeed
            while (!cx.run()) {
                // reset and restart
                cx = new WfcContext(sourceMap, 3, outputSize);
            }

            var output = cx.getOutput();
            debugPrintOutput(ref output);
            Wfc.Segments.Circle.print(ref output);

            // make sure the output is fine
            Test.testEveryRow(cx.state, ref cx.model.rule, cx.model.patterns);
            Test.testEveryColumn(cx.state, ref cx.model.rule, cx.model.patterns);
        }

        static string nl => System.Environment.NewLine;

        static Map getSource() {
            var path = loadAsciiMap("Example/Res/rooms.txt", inputSize : new Vec2(16, 16));
            return path;
            // var sourceMap = loadAsciiMap("Example/Res/a.txt", new Vec2(6, 6));
            // var sourceMap = loadAsciiMap("Example/Res/c.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/wide.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/rect.txt", new Vec2(9, 9));
            // var sourceMap = loadAsciiMap("Example/Res/curve.txt", new Vec2(7, 7));
            // var sourceMap = loadAsciiMap("Example/Res/test.txt", new Vec2(6, 6));
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

        static void debugPrintOutput(ref Map output) {
            Console.WriteLine("=== Output: ===");
            output.print();
        }

        // not in use
        static void runStepByStep(WfcContext cx) {
            var outputSize = cx.model.input.outputSize;
            while (true) {
                if (update()) break;
                System.Console.WriteLine($"============================");
                System.Console.WriteLine($">>>>>>>>>>> RETRY <<<<<<<<<<");
                System.Console.WriteLine($"============================");
                cx = new WfcContext(cx.model.input.source, 3, outputSize);
            }

            Test.testEveryRow(cx.state, ref cx.model.rule, cx.model.patterns);
            Test.testEveryColumn(cx.state, ref cx.model.rule, cx.model.patterns);

            bool update() {
                foreach(var status in cx.runIter()) {
                    {
                        var temp = cx.getOutput();
                        debugPrintOutput(ref temp);
                        // cx.state.printAvaiablePatternCounts(outputSize, cx.model.patterns.len);
                    }

                    switch (status) {
                        case WfcContext.AdvanceStatus.Continue:
                            continue; // advance WFC
                        case WfcContext.AdvanceStatus.Success:
                            return true;
                        case WfcContext.AdvanceStatus.Fail:
                            return false;
                    }
                }
                return false;
            }
        }
    }
}