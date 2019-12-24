using System.Collections.Generic;

namespace Wfc {
    public struct Array2D<T> {
        public int width;
        public List<T> values;

        public Array2D(int w, int h) {
            this.width = w;
            this.values = new List<T>(w * h);
        }

        int index(int x, int y) => x + y * this.width;

        public T get(int x, int y) {
            return this.values[this.index(x, y)];
        }

        public void set(int x, int y, T value) {
            this.values[this.index(x, y)] = value;
        }

        public void add(T value) => this.values.Add(value);
    }

    public struct Array3D<T> {
        public int nx;
        public int nz;
        public List<T> values;

        public Array3D(int nx, int ny, int nz) {
            this.nx = nx;
            this.nz = nz;
            this.values = new List<T>(nx * ny * nz);
        }

        int index(int x, int y, int z) => (x + y * this.nx) * this.nz + z;

        public T get(int x, int y, int z) {
            return this.values[this.index(x, y, z)];
        }

        public void set(int x, int y, int z, T value) {
            this.values[this.index(x, y, z)] = value;
        }

        public void add(T value) => this.values.Add(value);
    }
}