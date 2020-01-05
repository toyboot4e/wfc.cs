namespace Wfc {
    public class Vec2 {
        public int x;
        public int y;

        public Vec2(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public Vec2[] neighbors => new [] {
            new Vec2(this.x - 1, this.y),
            new Vec2(this.x, this.y - 1),
            new Vec2(this.x + 1, this.y),
            new Vec2(this.x, this.y + 1),
        };

        public int area => this.x * this.y;

        // operators
        public static bool operator ==(Vec2 v1, Vec2 v2) => v1.Equals(v2);
        public static bool operator !=(Vec2 v1, Vec2 v2) => !v1.Equals(v2);
        public static Vec2 operator +(Vec2 v1, Vec2 v2) => new Vec2(v1.x + v2.x, v1.y + v2.y);
        public static Vec2 operator +(Vec2 v1, int i2) => new Vec2(v1.x + i2, v1.y + i2);
        public static Vec2 operator +(int i1, Vec2 v2) => new Vec2(i1 + v2.x, i1 + v2.y);
        public static Vec2 operator -(Vec2 v1, Vec2 v2) => new Vec2(v1.x - v2.x, v1.y - v2.y);
        public static Vec2 operator -(Vec2 v1, int i2) => new Vec2(v1.x - i2, v1.y - i2);
        public static Vec2 operator -(int i1, Vec2 v2) => new Vec2(i1 - v2.x, i1 - v2.y);
        public static Vec2 operator *(Vec2 v1, int i2) => new Vec2(v1.x * i2, v1.y * i2);
        public static Vec2 operator *(int i1, Vec2 v2) => new Vec2(i1 * v2.x, i1 * v2.y);
        public static Vec2 operator /(Vec2 v1, int i2) => new Vec2(v1.x / i2, v1.y / i2);

        public override string ToString() => $"({x}, {y})";
        public override bool Equals(object obj) => obj is Vec2 ? Equals((Vec2) obj) : false;
        public override int GetHashCode() => this.ToString().GetHashCode();
    }
}