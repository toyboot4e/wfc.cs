using System;
using System.Collections.Generic;

namespace Wfc {
    // copied from https://miafish.wordpress.com/2015/03/16/c-min-heap-implementation/
    public class MinHeap<T> where T : IComparable<T> {
        public List<T> xs = new List<T>();

        public MinHeap(int capacity) {
            this.xs = new List<T>(capacity);
        }

        public int len() {
            return xs.Count;
        }

        public T min() {
            return this.xs.Count > 0 ? this.xs[0] : default(T);
        }

        public void add(T item) {
            xs.Add(item);
            this.bubbleUp(xs.Count - 1);
        }

        public T pop() {
            if (xs.Count > 0) {
                T item = xs[0];
                xs[0] = xs[xs.Count - 1];
                xs.RemoveAt(xs.Count - 1);

                this.bubbleDown(0);
                return item;
            }

            throw new InvalidOperationException("no element in heap");
        }

        void bubbleUp(int index) {
            var parent = this.parentOf(index);
            if (parent >= 0 && xs[index].CompareTo(xs[parent]) < 0) {
                var temp = xs[index];
                xs[index] = xs[parent];
                xs[parent] = temp;

                this.bubbleUp(parent);
            }
        }

        void bubbleDown(int index) {
            var smallest = index;

            var left = this.leftOf(index);
            var right = this.rightOf(index);

            if (left < this.len() && xs[left].CompareTo(xs[index]) < 0) {
                smallest = left;
            }

            if (right < this.len() && xs[right].CompareTo(xs[smallest]) < 0) {
                smallest = right;
            }

            if (smallest != index) {
                var temp = xs[index];
                xs[index] = xs[smallest];
                xs[smallest] = temp;

                this.bubbleDown(smallest);
            }

        }

        int parentOf(int index) {
            if (index <= 0) {
                return -1;
            }

            return (index - 1) / 2;
        }

        int leftOf(int index) {
            return 2 * index + 1;
        }

        int rightOf(int index) {
            return 2 * index + 2;
        }
    }
}