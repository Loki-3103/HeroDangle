namespace HeroDangle;

/// <summary>
/// Compact 2D simplex noise used as irregular idle "wind" so the rope never swings like a metronome.
/// </summary>
public static class SimplexNoise
{
    private static readonly int[] Perm;

    static SimplexNoise()
    {
        int[] p =
        [
            151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
            140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
            247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32,
            57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
            74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
            60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
            65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169,
            200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
            52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
            207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
            119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
            129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
            218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
            81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
            184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
            222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
        ];
        Perm = new int[512];
        for (int i = 0; i < 512; i++)
            Perm[i] = p[i & 255];
    }

    public static float Noise(float xin, float yin)
    {
        const float F2 = 0.366025403f;
        const float G2 = 0.211324865f;

        float s = (xin + yin) * F2;
        int i = FastFloor(xin + s);
        int j = FastFloor(yin + s);
        float t = (i + j) * G2;
        float x0 = xin - (i - t);
        float y0 = yin - (j - t);

        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1f + 2f * G2;
        float y2 = y0 - 1f + 2f * G2;

        int ii = i & 255;
        int jj = j & 255;

        float n0 = Corner(x0, y0, Grad(Perm[ii + Perm[jj]], x0, y0));
        float n1 = Corner(x1, y1, Grad(Perm[ii + i1 + Perm[jj + j1]], x1, y1));
        float n2 = Corner(x2, y2, Grad(Perm[ii + 1 + Perm[jj + 1]], x2, y2));
        return 70f * (n0 + n1 + n2);
    }

    private static float Corner(float x, float y, float g)
    {
        float t = 0.5f - x * x - y * y;
        if (t < 0)
            return 0;
        t *= t;
        return t * t * g;
    }

    private static int FastFloor(float x) => x > 0 ? (int)x : (int)x - 1;

    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 7;
        float u = h < 4 ? x : y;
        float v = h < 4 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
