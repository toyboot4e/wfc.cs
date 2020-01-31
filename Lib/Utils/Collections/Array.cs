using System.Collections.Generic;

namespace Wfc {
    /// <summary>Properties mapped from coordinates in a square</summary>
    /// <remark><c>add</c> items before using it. Continuous with x index</summary>
    public struct Grid2D<T> {
        public int width;
        public List<T> items;

        public Grid2D(int w, int h) {
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
    /// <remark><c>add</c> items before using it. Continuous with z, then x</summary>
    public struct Grid3D<T> {
        public int nx;
        public int nz;
        public List<T> items;

        public Grid3D(int nx, int ny, int nz) {
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
}