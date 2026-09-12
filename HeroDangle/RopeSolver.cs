namespace HeroDangle;

/// <summary>
/// Verlet rope: pinned top mass, distance constraints, optional drag on the free end.
/// No UI dependencies — step with a fixed dt and interpolate for rendering.
/// </summary>
public sealed class RopeSolver
{
    public const int PointCount = 12;
    public const float FixedDt = 1f / 120f;

    private readonly Vec2[] _pos = new Vec2[PointCount];
    private readonly Vec2[] _prev = new Vec2[PointCount];
    private readonly Vec2[] _stateA = new Vec2[PointCount];
    private readonly Vec2[] _stateB = new Vec2[PointCount];
    private readonly Vec2[] _dragHistory = new Vec2[4];
    private readonly long[] _dragTimes = new long[4];

    private int _historyCount;
    private int _historyIndex;
    private bool _dragging;
    private Vec2 _dragTarget;
    private bool _useA;

    public Vec2 Anchor { get; set; }
    public float RestLength { get; set; } = 220f;
    public float Gravity { get; set; } = 1650f;
    public float Damping { get; set; } = 0.99f;
    public int ConstraintIterations { get; set; } = 4;
    public float SegmentLength => RestLength / (PointCount - 1);

    public Vec2 CharmPosition => _pos[^1];

    public RopeSolver(Vec2 anchor, float restLength)
    {
        Anchor = anchor;
        RestLength = restLength;
        ResetToRest();
        Array.Copy(_pos, _stateA, PointCount);
        Array.Copy(_pos, _stateB, PointCount);
    }

    public void ResetToRest()
    {
        float seg = SegmentLength;
        for (int i = 0; i < PointCount; i++)
        {
            _pos[i] = new Vec2(Anchor.X, Anchor.Y + i * seg);
            _prev[i] = _pos[i];
        }

        _historyCount = 0;
        _historyIndex = 0;
        _dragging = false;
    }

    public void BeginDrag(Vec2 cursor)
    {
        _dragging = true;
        _dragTarget = cursor;
        _historyCount = 0;
        _historyIndex = 0;
        PushHistory(cursor);
    }

    public void UpdateDrag(Vec2 cursor)
    {
        _dragTarget = cursor;
        PushHistory(cursor);
    }

    public void EndDrag()
    {
        if (!_dragging)
            return;

        Vec2 velocity = EstimateReleaseVelocity();
        // Verlet stores implied velocity as pos - prev. Inject flick by rewriting prev.
        _prev[^1] = _pos[^1] - velocity * FixedDt;
        _dragging = false;
        _historyCount = 0;
    }

    public void Step(Vec2 wind)
    {
        Array.Copy(_pos, CurrentState(), PointCount);

        float dt = FixedDt;
        float dt2 = dt * dt;
        float damp = Damping;

        for (int i = 1; i < PointCount; i++)
        {
            Vec2 current = _pos[i];
            Vec2 velocity = (current - _prev[i]) * damp;
            Vec2 accel = new Vec2(wind.X, wind.Y + Gravity);
            Vec2 next = current + velocity + accel * dt2;
            _prev[i] = current;
            _pos[i] = next;
        }

        if (_dragging)
        {
            _pos[^1] = _dragTarget;
            _prev[^1] = _dragTarget;
        }

        float rest = SegmentLength;
        for (int iter = 0; iter < ConstraintIterations; iter++)
        {
            _pos[0] = Anchor;
            for (int i = 0; i < PointCount - 1; i++)
            {
                Vec2 a = _pos[i];
                Vec2 b = _pos[i + 1];
                Vec2 delta = b - a;
                float dist = delta.Length;
                if (dist < 1e-6f)
                    continue;

                float diff = (dist - rest) / dist;
                if (i == 0)
                {
                    _pos[i + 1] = b - delta * diff;
                }
                else if (_dragging && i + 1 == PointCount - 1)
                {
                    _pos[i] = a + delta * diff;
                }
                else
                {
                    Vec2 correction = delta * (0.5f * diff);
                    _pos[i] = a + correction;
                    _pos[i + 1] = b - correction;
                }
            }

            _pos[0] = Anchor;
            if (_dragging)
                _pos[^1] = _dragTarget;
        }

        Array.Copy(_pos, OtherState(), PointCount);
        _useA = !_useA;
    }

    public void Interpolate(float alpha, Vec2[] dest)
    {
        Vec2[] from = _useA ? _stateB : _stateA;
        Vec2[] to = _useA ? _stateA : _stateB;
        alpha = Math.Clamp(alpha, 0f, 1f);
        for (int i = 0; i < PointCount; i++)
            dest[i] = Vec2.Lerp(from[i], to[i], alpha);
    }

    public float SwayAngle(ReadOnlySpan<Vec2> points)
    {
        Vec2 tip = points[^1] - points[^2];
        return MathF.Atan2(tip.X, MathF.Max(tip.Y, 0.01f));
    }

    private Vec2[] CurrentState() => _useA ? _stateA : _stateB;
    private Vec2[] OtherState() => _useA ? _stateB : _stateA;

    private void PushHistory(Vec2 cursor)
    {
        _dragHistory[_historyIndex] = cursor;
        _dragTimes[_historyIndex] = Environment.TickCount64;
        _historyIndex = (_historyIndex + 1) % _dragHistory.Length;
        if (_historyCount < _dragHistory.Length)
            _historyCount++;
    }

    private Vec2 EstimateReleaseVelocity()
    {
        if (_historyCount < 2)
            return Vec2.Zero;

        int last = (_historyIndex - 1 + _dragHistory.Length) % _dragHistory.Length;
        int first = (_historyIndex - _historyCount + _dragHistory.Length) % _dragHistory.Length;
        long dtMs = _dragTimes[last] - _dragTimes[first];
        if (dtMs < 8)
            return Vec2.Zero;

        Vec2 velocity = (_dragHistory[last] - _dragHistory[first]) / (dtMs / 1000f);
        const float maxSpeed = 4200f;
        float speed = velocity.Length;
        return speed > maxSpeed ? velocity.Normalized() * maxSpeed : velocity;
    }
}
