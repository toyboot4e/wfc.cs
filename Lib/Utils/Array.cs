using System.Collections.Generic;

namespace Wfc {
    /// <summary>Properties mapped from coordinates in a square</summary>
    /// <remark><c>add</c> items before using it. Continuous with x index</summary>
    public struct RectArray<T> {
        public int width;
        public List<T> items;

        /// <summary>Creates an <c>Array2d</c> with capacity w * h</summary>
        /// <remark>Never forget to <c>add</c> items before accessing <c>values</c></summary>
        public RectArray(int w, int h) {
            this.width = w;
            this.items = new List<T>(w * h);
        }

        public int capacity => this.items.Capacity;

        public int index(int x, int y) => x + this.width * y;

        public T get(int x, int y) {
            return this.items[this.index(x, y)];
        }
        public void set(int x, int y, T value) {
            this.items[this.index(x, y)] = value;
        }

        public void add(T value) => this.items.Add(value);

        public T this[int x, int y] {
            get => this.get(x, y);
            set => this.set(x, y, value);
        }
    }

    /// <summary>Properties mapped from coordinates in a cuboid</summary>
    /// <remark><c>add</c> items before using it. Continuous with z index (then x index)</summary>
    public struct CuboidArray<T> {
        public int nx;
        public int nz;
        public List<T> items;

        /// <summary>Creates an <c>Array2d</c> with capacity nx * ny * nz</summary>
        /// <remark>Never forget to <c>add</c> items before accessing <c>values</c></summary>
        public CuboidArray(int nx, int ny, int nz) {
            this.nx = nx;
            this.nz = nz;
            this.items = new List<T>(nx * ny * nz);
        }

        public int capacity => this.items.Capacity;

        public int index(int x, int y, int z) => z + this.nz * (x + this.nx * y);

        public T get(int x, int y, int z) {
            return this.items[this.index(x, y, z)];
        }
        public void set(int x, int y, int z, T value) {
            this.items[this.index(x, y, z)] = value;
        }

        public void add(T value) => this.items.Add(value);

        public T this[int x, int y, int z] {
            get => this.get(x, y, z);
            set => this.set(x, y, z, value);
        }
    }

    /// <summary>Properties mapped from coordinates in a triangular prism</summary>
    /// <remark>
    /// <c>add</c> items before using it. Continuous with z index (then x index).
    /// </summary>
    public struct TriangularPrismArray<T> {
        public int nx;
        public int nz;
        public List<T> items;

        public TriangularPrismArray(int nx, int ny, int nz) {
            this.nx = nx;
            this.nz = nz;
            this.items = new List<T>((nx + 1) * ny / 2 * nz);
        }

        public int capacity => this.items.Capacity;

        public int xyIndex(int x, int y) {
            // force (x >= y)
            if (x < y) { // swap
                y = x + y; //  a + b
                x = y - x; // (a + b) - a (=b)
                y = y - x; // (a + b) - b (=a)
            }

            //        x
            //     0123
            //   0 0123
            //   1  456
            //   2   78
            // y 3    9
            int top = this.nx;
            int bottom = nx - y;
            return (top + bottom) * y / 2 + (x - y);
        }

        public int index(int x, int y, int z) {
            return this.xyIndex(x, y) * this.nz + z;
        }

        public T get(int x, int y, int z) {
            return this.items[this.index(x, y, z)];
        }
        public void set(int x, int y, int z, T value) {
            this.items[this.index(x, y, z)] = value;
        }

        public void add(T value) => this.items.Add(value);

        public T this[int x, int y, int z] {
            get => this.get(x, y, z);
            set => this.set(x, y, z, value);
        }
    }
}