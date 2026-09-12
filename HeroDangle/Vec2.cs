namespace HeroDangle;

public readonly struct Vec2
{
    public readonly float X;
    public readonly float Y;

    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float Length => MathF.Sqrt(X * X + Y * Y);

    public static Vec2 Zero => new(0, 0);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, float s) => new(a.X * s, a.Y * s);
    public static Vec2 operator *(float s, Vec2 a) => a * s;
    public static Vec2 operator /(Vec2 a, float s) => new(a.X / s, a.Y / s);

    public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;

    public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;

    public Vec2 Normalized()
    {
        float len = Length;
        return len > 1e-8f ? this / len : Zero;
    }
}
