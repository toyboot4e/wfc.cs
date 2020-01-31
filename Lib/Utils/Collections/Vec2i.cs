using System;

namespace Wfc {
    public class Vec2i {
        public int x;
        public int y;

        public Vec2i(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public Vec2i[] neighbors => new [] {
            new Vec2i(this.x - 1, this.y),
            new Vec2i(this.x, this.y - 1),
            new Vec2i(this.x + 1, this.y),
            new Vec2i(this.x, this.y + 1),
        };

        public int area => this.x * this.y;

        /// <summary>
        /// Consider this vector as a corner of a rectangle and see if it contains a point
        /// </summary>
        public bool contains(Vec2i other) {
            if (other.x < 0 || other.x >= this.x) return false;
            if (other.y < 0 || other.y >= this.y) return false;
            return true;
        }

        public int distanceInt() {
            return (int) Math.Sqrt(this.x * this.x + this.y * this.y);
        }

        // operators
        public static bool operator ==(Vec2i v1, Vec2i v2) => v1.Equals(v2);
        public static bool operator !=(Vec2i v1, Vec2i v2) => !v1.Equals(v2);
        public static Vec2i operator +(Vec2i v1, Vec2i v2) => new Vec2i(v1.x + v2.x, v1.y + v2.y);
        public static Vec2i operator +(Vec2i v1, int i2) => new Vec2i(v1.x + i2, v1.y + i2);
        public static Vec2i operator +(int i1, Vec2i v2) => new Vec2i(i1 + v2.x, i1 + v2.y);
        public static Vec2i operator -(Vec2i v1, Vec2i v2) => new Vec2i(v1.x - v2.x, v1.y - v2.y);
        public static Vec2i operator -(Vec2i v1, int i2) => new Vec2i(v1.x - i2, v1.y - i2);
        public static Vec2i operator -(int i1, Vec2i v2) => new Vec2i(i1 - v2.x, i1 - v2.y);
        public static Vec2i operator *(Vec2i v1, int i2) => new Vec2i(v1.x * i2, v1.y * i2);
        public static Vec2i operator *(int i1, Vec2i v2) => new Vec2i(i1 * v2.x, i1 * v2.y);
        public static Vec2i operator /(Vec2i v1, int i2) => new Vec2i(v1.x / i2, v1.y / i2);
        public static Vec2i operator %(Vec2i v1, Vec2i v2) => new Vec2i(v1.x % v2.x, v1.y % v2.y);

        public override string ToString() => $"({x}, {y})";
        public override bool Equals(object obj) => obj is Vec2i ? Equals((Vec2i) obj) : false;
        public override int GetHashCode() => this.ToString().GetHashCode();
    }
}