using Raylib_cs;
using System.Numerics;

class Program
{
    enum GameState { Loading, MainMenu, Playing }

    static GameState state = GameState.Loading;
    static bool settingsOpen = false;

    static float loadTimer = 0f;
    const float LOAD_DURATION = 4.5f;
    static float spinnerAngle = 0f;

    static Texture2D bgTexture;
    static int screenW = 1280;
    static int screenH = 720;

    static float mouseSensitivity = 1.0f;
    static float masterVolume = 70f;
    static float fov = 90f;

    static Sound menuMusic;
    static bool audioReady = false;

    static readonly Color AccentGold = new(230, 190, 60, 255);
    static readonly Color AccentGoldBright = new(255, 220, 90, 255);
    static readonly Color DarkPanel = new(15, 18, 24, 220);
    static readonly Color DarkPanelHover = new(30, 36, 46, 245);
    static readonly Color TextPrimary = new(240, 245, 250, 255);
    static readonly Color TextDim = new(150, 165, 180, 255);

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--make-icon")
        {
            MakeIconFiles();
            return;
        }
        if (args.Length > 0 && args[0] == "--make-banner")
        {
            MakeBannerFiles();
            return;
        }
        if (args.Length > 0 && args[0] == "--make-music")
        {
            MakeMusicFile();
            return;
        }

        Raylib.InitWindow(screenW, screenH, "STEEL RAIN: Heavy Warfare");
        Raylib.SetExitKey(KeyboardKey.Null);

        string iconPath = Path.Combine(AppContext.BaseDirectory, "icon.png");
        if (!File.Exists(iconPath))
        {
            Image gen = BuildIcon();
            Raylib.ExportImage(gen, iconPath);
            Raylib.UnloadImage(gen);
        }
        Image icon = Raylib.LoadImage(iconPath);
        Raylib.SetWindowIcon(icon);
        Raylib.UnloadImage(icon);

        Raylib.SetTargetFPS(60);
        bgTexture = GenerateBackground(screenW, screenH);

        Raylib.InitAudioDevice();
        if (Raylib.IsAudioDeviceReady())
        {
            string musicPath = Path.Combine(AppContext.BaseDirectory, "menu_music.wav");
            if (!File.Exists(musicPath))
            {
                Console.WriteLine("[music] генерирую эпик-трек...");
                byte[] wav = GenerateMenuMusicWav();
                File.WriteAllBytes(musicPath, wav);
                Console.WriteLine($"[music] сохранён: {musicPath} ({wav.Length / 1024} KB)");
            }
            else
            {
                Console.WriteLine($"[music] найден: {musicPath}");
            }
            menuMusic = Raylib.LoadSound(musicPath);
            audioReady = true;
        }

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            Raylib.BeginDrawing();

            if (audioReady)
            {
                bool inMenu = state == GameState.Loading || state == GameState.MainMenu;
                if (inMenu && !Raylib.IsSoundPlaying(menuMusic))
                    Raylib.PlaySound(menuMusic);
                else if (!inMenu && Raylib.IsSoundPlaying(menuMusic))
                    Raylib.StopSound(menuMusic);
            }

            switch (state)
            {
                case GameState.Loading:
                    UpdateLoading(dt);
                    DrawLoading();
                    break;
                case GameState.MainMenu:
                    DrawMainMenu();
                    break;
                case GameState.Playing:
                    DrawPlaying();
                    break;
            }

            Raylib.EndDrawing();
        }

        if (audioReady) Raylib.UnloadSound(menuMusic);
        Raylib.UnloadTexture(bgTexture);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    // ======================================================
    // ============ ГЕНЕРАЦИЯ БАННЕРА =======================
    // ======================================================
    static void MakeBannerFiles()
    {
        string cwd = Directory.GetCurrentDirectory();
        Console.WriteLine($"[make-banner] папка: {cwd}");

        string svg = BuildBannerSvg();
        string svgPath = Path.Combine(cwd, "banner.svg");
        File.WriteAllText(svgPath, svg);
        Console.WriteLine($"[make-banner] SVG: {svgPath}");

        Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
        Raylib.InitWindow(1, 1, "Banner");
        Raylib.SetTargetFPS(60);

        RenderTexture2D rt = Raylib.LoadRenderTexture(1280, 720);
        Raylib.BeginTextureMode(rt);
        DrawBannerToScreen(1280, 720);
        Raylib.EndTextureMode();

        Image img = Raylib.LoadImageFromTexture(rt.Texture);
        Raylib.ImageFlipVertical(ref img);

        string pngPath = Path.Combine(cwd, "banner.png");
        Raylib.ExportImage(img, pngPath);
        Console.WriteLine($"[make-banner] PNG: {pngPath}");

        Raylib.UnloadImage(img);
        Raylib.UnloadRenderTexture(rt);
        Raylib.CloseWindow();
        Console.WriteLine("[make-banner] готово!");
    }

    static void DrawBannerToScreen(int w, int h)
    {
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            byte r, g, b;
            if (t < 0.55f)
            {
                float u = t / 0.55f;
                r = (byte)(10 + (26 - 10) * u);
                g = (byte)(13 + (34 - 13) * u);
                b = (byte)(18 + (51 - 18) * u);
            }
            else
            {
                float u = (t - 0.55f) / 0.45f;
                r = (byte)(26 - (26 - 5) * u);
                g = (byte)(34 - (34 - 7) * u);
                b = (byte)(51 - (51 - 10) * u);
            }
            Raylib.DrawRectangle(0, y, w, 1, new Color(r, g, b, (byte)255));
        }

        Raylib.DrawCircleGradient(
    w / 2, (int)(h * 0.42f), w * 0.55f,
    new Color((byte)230, (byte)190, (byte)60, (byte)45),
    new Color((byte)230, (byte)190, (byte)60, (byte)0));

        var rnd = new Random(42);
        for (int i = 0; i < 180; i++)
        {
            int x = rnd.Next(0, w);
            int y = rnd.Next(0, h);
            int len = rnd.Next(15, 45);
            int dx = rnd.Next(-3, 4);
            Raylib.DrawLine(x, y, x + dx, y + len,
                new Color((byte)143, (byte)165, (byte)189, (byte)90));
        }

        int cx = 0;
        while (cx < w)
        {
            int bw = rnd.Next(40, 120);
            int bh = rnd.Next(40, 200);
            int bx = cx;
            int by = h - bh - 60;
            Raylib.DrawRectangle(bx, by, bw, bh + 60,
                new Color((byte)5, (byte)7, (byte)10, (byte)255));

            for (int wy = by + 15; wy < h - 70; wy += 18)
                for (int wx = bx + 10; wx < bx + bw - 10; wx += 16)
                    if (rnd.Next(0, 5) == 0)
                        Raylib.DrawRectangle(wx, wy, 4, 6,
                            new Color((byte)230, (byte)190, (byte)90, (byte)180));

            cx += bw + rnd.Next(-5, 15);
        }

        Raylib.DrawLine(0, h - 60, w, h - 60,
            new Color((byte)90, (byte)101, (byte)114, (byte)100));

        Raylib.DrawTriangle(
            new Vector2(0, h - 80), new Vector2(380, h - 80), new Vector2(0, h),
            new Color((byte)168, (byte)132, (byte)30, (byte)30));
        Raylib.DrawTriangle(
            new Vector2(w, 0), new Vector2(w - 400, 0), new Vector2(w, 90),
            new Color((byte)168, (byte)132, (byte)30, (byte)30));

        DrawCorner(40, 40, 1, 1);
        DrawCorner(w - 40, 40, -1, 1);
        DrawCorner(40, h - 40, 1, -1);
        DrawCorner(w - 40, h - 40, -1, -1);

        string left = "STEEL ";
        string right = "RAIN";
        int ts = 118;
        int tw = Raylib.MeasureText(left, ts) + Raylib.MeasureText(right, ts);
        int tx = (w - tw) / 2;
        int ty = (int)(h * 0.36f);

        Raylib.DrawText(left, tx + 4, ty + 4, ts, new Color((byte)0, (byte)0, (byte)0, (byte)200));
        Raylib.DrawText(right, tx + Raylib.MeasureText(left, ts) + 4, ty + 4, ts,
            new Color((byte)0, (byte)0, (byte)0, (byte)200));

        Raylib.DrawText(left, tx, ty, ts, new Color((byte)245, (byte)210, (byte)100, (byte)255));
        Raylib.DrawText(right, tx + Raylib.MeasureText(left, ts), ty, ts,
            new Color((byte)255, (byte)255, (byte)255, (byte)255));

        int ly = ty + ts + 12;
        Raylib.DrawRectangle(w / 2 - 240, ly, 480, 3,
            new Color((byte)168, (byte)132, (byte)30, (byte)255));
        Raylib.DrawRectangle(w / 2 - 210, ly + 8, 420, 1,
            new Color((byte)168, (byte)132, (byte)30, (byte)150));

        string sub = "HEAVY WARFARE";
        int subSize = 28;
        int subW = Raylib.MeasureText(sub, subSize);
        Raylib.DrawText(sub, (w - subW) / 2, ly + 28, subSize,
            new Color((byte)201, (byte)209, (byte)217, (byte)255));

        Raylib.DrawText("STEEL RAIN - v0.1 ALPHA", 60, h - 40, 14,
            new Color((byte)107, (byte)118, (byte)130, (byte)255));

        string tag = "INDUSTRIAL EDITION";
        int tagW = Raylib.MeasureText(tag, 14);
        Raylib.DrawText(tag, w - tagW - 60, h - 40, 14,
            new Color((byte)107, (byte)118, (byte)130, (byte)255));
    }

    static void DrawCorner(int x, int y, int sx, int sy)
    {
        Color c = new Color((byte)168, (byte)132, (byte)30, (byte)180);
        Raylib.DrawLine(x, y, x + sx * 60, y, c);
        Raylib.DrawLine(x, y, x, y + sy * 60, c);
    }

    static string BuildBannerSvg()
    {
        var rnd = new Random(42);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  <g stroke=\"#8fa5bd\" stroke-width=\"2\" opacity=\"0.3\" stroke-linecap=\"round\">");
        for (int i = 0; i < 140; i++)
        {
            int x = rnd.Next(0, 1280);
            int y = rnd.Next(0, 720);
            int len = rnd.Next(15, 45);
            int dx = rnd.Next(-3, 4);
            sb.AppendLine($"    <line x1=\"{x}\" y1=\"{y}\" x2=\"{x + dx}\" y2=\"{y + len}\"/>");
        }
        sb.AppendLine("  </g>");
        string rain = sb.ToString();

        sb = new System.Text.StringBuilder();
        sb.AppendLine("  <g fill=\"#05070a\">");
        int cx = 0;
        while (cx < 1280)
        {
            int bw = rnd.Next(40, 120);
            int bh = rnd.Next(40, 200);
            sb.AppendLine($"    <rect x=\"{cx}\" y=\"{720 - bh - 60}\" width=\"{bw}\" height=\"{bh + 60}\"/>");
            cx += bw + rnd.Next(-5, 15);
        }
        sb.AppendLine("  </g>");
        string city = sb.ToString();

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg xmlns=""http://www.w3.org/2000/svg"" width=""1280"" height=""720"" viewBox=""0 0 1280 720"">
  <defs>
    <linearGradient id=""bg"" x1=""0"" y1=""0"" x2=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#0a0d12""/>
      <stop offset=""55%"" stop-color=""#1a2233""/>
      <stop offset=""100%"" stop-color=""#05070a""/>
    </linearGradient>
    <radialGradient id=""glow"" cx=""50%"" cy=""42%"" r=""60%"">
      <stop offset=""0%"" stop-color=""#e6be3c"" stop-opacity=""0.20""/>
      <stop offset=""100%"" stop-color=""#e6be3c"" stop-opacity=""0""/>
    </radialGradient>
    <linearGradient id=""gold"" x1=""0"" y1=""0"" x2=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#f5d264""/>
      <stop offset=""100%"" stop-color=""#a8841e""/>
    </linearGradient>
  </defs>

  <rect width=""1280"" height=""720"" fill=""url(#bg)""/>
  <ellipse cx=""640"" cy=""320"" rx=""700"" ry=""340"" fill=""url(#glow)""/>

{rain}

{city}

  <line x1=""0"" y1=""660"" x2=""1280"" y2=""660"" stroke=""#5a6572"" stroke-width=""1"" opacity=""0.4""/>

  <polygon points=""0,640 380,640 520,720 0,720"" fill=""#a8841e"" opacity=""0.12""/>
  <polygon points=""1280,0 880,0 760,90 1280,90"" fill=""#a8841e"" opacity=""0.12""/>

  <path d=""M 40 40 L 40 100 M 40 40 L 100 40"" stroke=""#a8841e"" stroke-width=""3"" fill=""none"" opacity=""0.7""/>
  <path d=""M 1240 40 L 1240 100 M 1240 40 L 1180 40"" stroke=""#a8841e"" stroke-width=""3"" fill=""none"" opacity=""0.7""/>
  <path d=""M 40 680 L 40 620 M 40 680 L 100 680"" stroke=""#a8841e"" stroke-width=""3"" fill=""none"" opacity=""0.7""/>
  <path d=""M 1240 680 L 1240 620 M 1240 680 L 1180 680"" stroke=""#a8841e"" stroke-width=""3"" fill=""none"" opacity=""0.7""/>

  <text x=""640"" y=""360"" font-family=""Impact, 'Arial Black', 'Helvetica Neue', sans-serif""
        font-size=""150"" font-weight=""900"" text-anchor=""middle"" letter-spacing=""4"">
    <tspan fill=""url(#gold)"">STEEL</tspan><tspan fill=""#ffffff""> RAIN</tspan>
  </text>

  <line x1=""400"" y1=""400"" x2=""880"" y2=""400"" stroke=""#a8841e"" stroke-width=""4""/>
  <line x1=""430"" y1=""408"" x2=""850"" y2=""408"" stroke=""#a8841e"" stroke-width=""1"" opacity=""0.6""/>

  <text x=""640"" y=""460"" font-family=""'Arial Black', Impact, sans-serif""
        font-size=""30"" font-weight=""700"" text-anchor=""middle""
        fill=""#c9d1d9"" letter-spacing=""16"">HEAVY WARFARE</text>

  <text x=""60"" y=""700"" font-family=""Arial, sans-serif"" font-size=""14""
        fill=""#6b7682"" letter-spacing=""3"">STEEL RAIN - v0.1 ALPHA</text>
  <text x=""1220"" y=""700"" font-family=""Arial, sans-serif"" font-size=""14""
        fill=""#6b7682"" text-anchor=""end"" letter-spacing=""3"">INDUSTRIAL EDITION</text>
</svg>";
    }

    // ======================================================
    // ============ ГЕНЕРАЦИЯ МУЗЫКИ =========================
    // ======================================================
    static void MakeMusicFile()
    {
        string cwd = Directory.GetCurrentDirectory();
        Console.WriteLine($"[make-music] рабочая папка: {cwd}");
        byte[] wav = GenerateMenuMusicWav();
        string path = Path.Combine(cwd, "menu_music.wav");
        File.WriteAllBytes(path, wav);
        Console.WriteLine($"[make-music] сохранён: {path} ({wav.Length / 1024} KB)");
        Console.WriteLine("[make-music] готово!");
    }

    static byte[] GenerateMenuMusicWav()
    {
        const int SR = 44100;
        const float BPM = 100f;
        const int BARS = 8;
        const int BEATS = 4;
        float beatDur = 60f / BPM;
        int total = (int)(BARS * BEATS * beatDur * SR);

        float[] kick  = new float[total];
        float[] snare = new float[total];
        float[] hat   = new float[total];
        float[] crash = new float[total];
        float[] tom   = new float[total];
        float[] bass  = new float[total];
        float[] pad   = new float[total];
        float[] lead  = new float[total];
        float[] arp   = new float[total];
        float[] sub   = new float[total];
        float[] atmo  = new float[total];

        var rnd = new Random(42);

        float[][] chord = {
            new[]{110.00f, 130.81f, 164.81f},
            new[]{110.00f, 130.81f, 164.81f},
            new[]{ 87.31f, 110.00f, 130.81f},
            new[]{ 87.31f, 110.00f, 130.81f},
            new[]{130.81f, 164.81f, 196.00f},
            new[]{130.81f, 164.81f, 196.00f},
            new[]{ 98.00f, 123.47f, 146.83f},
            new[]{ 98.00f, 123.47f, 146.83f}
        };

        float[][] melody = {
            new float[8],
            new float[8],
            new[]{ 220.00f, 0, 0, 0, 329.63f, 0, 0, 0 },
            new[]{ 220.00f, 0, 0, 0, 261.63f, 0, 0, 0 },
            new[]{ 329.63f, 0, 392.00f, 0, 523.25f, 0, 392.00f, 0 },
            new[]{ 349.23f, 0, 440.00f, 0, 523.25f, 0, 440.00f, 0 },
            new[]{ 392.00f, 0, 493.88f, 0, 587.33f, 0, 493.88f, 0 },
            new[]{ 293.66f, 0, 369.99f, 0, 440.00f, 0, 0, 0 }
        };

        float[][] arpNotes = {
            new float[16],
            new float[16],
            new[]{ 220f,329.63f,440f,329.63f, 220f,329.63f,440f,329.63f, 220f,329.63f,440f,329.63f, 220f,329.63f,440f,329.63f },
            new[]{ 220f,349.23f,440f,349.23f, 220f,349.23f,440f,349.23f, 220f,349.23f,440f,349.23f, 220f,349.23f,440f,349.23f },
            new[]{ 261.63f,392f,523.25f,392f, 261.63f,392f,523.25f,392f, 261.63f,392f,523.25f,392f, 261.63f,392f,523.25f,392f },
            new[]{ 261.63f,349.23f,523.25f,349.23f, 261.63f,349.23f,523.25f,349.23f, 261.63f,349.23f,523.25f,349.23f, 261.63f,349.23f,523.25f,349.23f },
            new[]{ 293.66f,392f,493.88f,392f, 293.66f,392f,493.88f,392f, 293.66f,392f,493.88f,392f, 293.66f,392f,493.88f,392f },
            new[]{ 293.66f,369.99f,440f,369.99f, 293.66f,369.99f,440f,369.99f, 293.66f,369.99f,440f,369.99f, 293.66f,369.99f,440f,369.99f }
        };

        for (int bar = 0; bar < BARS; bar++)
        {
            float bs = bar * BEATS * beatDur;
            bool hasDrums = bar >= 2;
            bool isEpic = bar >= 4 && bar <= 5;
            bool isFull = bar >= 4;

            for (int n = 0; n < 3; n++)
            {
                float f = chord[bar][n];
                AddSawLP(pad, SR, bs, BEATS * beatDur, f,         0.045f, 900f);
                AddSawLP(pad, SR, bs, BEATS * beatDur, f * 1.005f, 0.045f, 900f);
                AddSawLP(pad, SR, bs, BEATS * beatDur, f * 0.995f, 0.045f, 900f);
            }

            AddSine(sub, SR, bs, BEATS * beatDur, chord[bar][0] * 0.5f, 0.11f);

            if (hasDrums)
            {
                AddSawLP(bass, SR, bs + 0 * beatDur, beatDur * 0.95f, chord[bar][0], 0.18f, 420f);
                AddSawLP(bass, SR, bs + 2 * beatDur, beatDur * 0.95f, chord[bar][0], 0.18f, 420f);
                if (isFull)
                {
                    AddSawLP(bass, SR, bs + 1 * beatDur, beatDur * 0.5f, chord[bar][1], 0.12f, 500f);
                    AddSawLP(bass, SR, bs + 3 * beatDur, beatDur * 0.5f, chord[bar][1], 0.12f, 500f);
                }
            }

            for (int e = 0; e < 8; e++)
            {
                float f = melody[bar][e];
                if (f > 0)
                    AddLead(lead, SR, bs + e * beatDur / 2f, beatDur / 2f, f, 0.075f);
            }

            if (hasDrums)
            {
                for (int s = 0; s < 16; s++)
                {
                    float f = arpNotes[bar][s];
                    if (f > 0)
                        AddSine(arp, SR, bs + s * beatDur / 4f, beatDur / 4f * 0.8f, f, 0.045f);
                }
            }

            AddNoiseLP(atmo, SR, bs, BEATS * beatDur, 0.012f, 350f, rnd);

            if (hasDrums)
            {
                for (int b = 0; b < 4; b++)
                {
                    if (b == 0 || b == 2)
                        AddKick(kick, SR, bs + b * beatDur);
                    if (b == 1 || b == 3)
                        AddSnare(snare, SR, bs + b * beatDur, rnd);
                }
                for (int s = 0; s < 8; s++)
                    AddHat(hat, SR, bs + s * beatDur / 2f, rnd, 0.035f);
            }
            if (isFull)
            {
                AddKick(kick, SR, bs + 2.5f * beatDur);
                for (int s = 1; s < 16; s += 2)
                    AddHat(hat, SR, bs + s * beatDur / 4f, rnd, 0.018f);
            }
            if (isEpic)
            {
                AddTom(tom, SR, bs + 3.5f * beatDur, 110f);
                AddTom(tom, SR, bs + 3.75f * beatDur, 85f);
            }
            if (bar == 4 || bar == 6)
                AddCrash(crash, SR, bs, rnd);
        }

        ApplySidechain(bass, kick);
        ApplySidechain(pad,  kick);
        ApplySidechain(arp,  kick);

        float[] master = new float[total];
        for (int i = 0; i < total; i++)
        {
            master[i] = kick[i] + snare[i] + hat[i] + crash[i] + tom[i]
                      + bass[i] + pad[i] + lead[i] + arp[i] + sub[i] + atmo[i];
        }

        ApplyReverb(master, SR, mix: 0.20f, decay: 0.55f, delayMs: 60f);

        float peak = 0f;
        for (int i = 0; i < total; i++) peak = MathF.Max(peak, MathF.Abs(master[i]));
        if (peak > 0.01f)
        {
            float norm = 0.85f / peak;
            for (int i = 0; i < total; i++)
                master[i] = MathF.Tanh(master[i] * norm * 1.2f) * 0.85f;
        }

        int fadeIn  = (int)(0.20f * SR);
        int fadeOut = (int)(0.80f * SR);
        for (int i = 0; i < total; i++)
        {
            if (i < fadeIn) master[i] *= (float)i / fadeIn;
            if (i > total - fadeOut) master[i] *= (float)(total - i) / fadeOut;
        }

        byte[] pcm = new byte[total * 2];
        for (int i = 0; i < total; i++)
        {
            short s = (short)(Math.Clamp(master[i], -1f, 1f) * short.MaxValue);
            pcm[i * 2]     = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return BuildWav(pcm, SR, 1, 16);
    }

    static float Env(float t, float dur, float attack, float release)
    {
        if (t < attack) return t / attack;
        if (t > dur - release) return MathF.Max(0f, (dur - t) / release);
        return 1f;
    }

    static void AddSine(float[] buf, int sr, float start, float dur, float freq, float gain)
    {
        int ss = (int)(start * sr), len = (int)(dur * sr);
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            buf[idx] += MathF.Sin(2f * MathF.PI * freq * t) * Env(t, dur, 0.01f, 0.10f) * gain;
        }
    }

    static void AddSawLP(float[] buf, int sr, float start, float dur, float freq, float gain, float cutoff)
    {
        int ss = (int)(start * sr), len = (int)(dur * sr);
        float phase = 0f, lp = 0f;
        float alpha = 1f - MathF.Exp(-2f * MathF.PI * cutoff / sr);
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            phase += freq / sr; if (phase >= 1f) phase -= 1f;
            float saw = 2f * phase - 1f;
            lp += (saw - lp) * alpha;
            buf[idx] += lp * Env(t, dur, 0.005f, 0.15f) * gain;
        }
    }

    static void AddLead(float[] buf, int sr, float start, float dur, float freq, float gain)
    {
        int ss = (int)(start * sr), len = (int)(dur * sr);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            float vib = 1f + 0.005f * MathF.Sin(2f * MathF.PI * 5.5f * t);
            phase += freq * vib / sr;
            float v = MathF.Sin(2f * MathF.PI * phase) + 0.3f * MathF.Sin(4f * MathF.PI * phase);
            buf[idx] += v * Env(t, dur, 0.01f, 0.20f) * gain;
        }
    }

    static void AddNoiseLP(float[] buf, int sr, float start, float dur, float gain, float cutoff, Random rnd)
    {
        int ss = (int)(start * sr), len = (int)(dur * sr);
        float lp = 0f;
        float alpha = 1f - MathF.Exp(-2f * MathF.PI * cutoff / sr);
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            float n = (float)(rnd.NextDouble() * 2 - 1);
            lp += (n - lp) * alpha;
            buf[idx] += lp * Env(t, dur, 0.5f, 0.5f) * gain;
        }
    }

    static void AddKick(float[] buf, int sr, float start)
    {
        int ss = (int)(start * sr), len = (int)(0.25f * sr);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            float env = MathF.Exp(-t * 16f);
            float f = 45f + 130f * MathF.Exp(-t * 30f);
            phase += 2f * MathF.PI * f / sr;
            buf[idx] += MathF.Sin(phase) * env * 0.7f;
        }
    }

    static void AddSnare(float[] buf, int sr, float start, Random rnd)
    {
        int ss = (int)(start * sr), len = (int)(0.20f * sr);
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            float env = MathF.Exp(-t * 22f);
            float noise = (float)(rnd.NextDouble() * 2 - 1);
            float tone  = MathF.Sin(2f * MathF.PI * 200f * t) * 0.4f;
            buf[idx] += (noise * 0.7f + tone) * env * 0.30f;
        }
    }

    static void AddHat(float[] buf, int sr, float start, Random rnd, float gain)
    {
        int ss = (int)(start * sr), len = (int)(0.05f * sr);
        float prev = 0f;
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            float env = MathF.Exp(-t * 50f);
            float n = (float)(rnd.NextDouble() * 2 - 1);
            float h = n - prev; prev = n;
            buf[idx] += h * env * gain;
        }
    }

    static void AddCrash(float[] buf, int sr, float start, Random rnd)
    {
        int ss = (int)(start * sr), len = (int)(1.5f * sr);
        float prev = 0f;
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            float env = MathF.Exp(-t * 2.5f);
            float n = (float)(rnd.NextDouble() * 2 - 1);
            float h = n - prev; prev = n;
            buf[idx] += h * env * 0.15f;
        }
    }

    static void AddTom(float[] buf, int sr, float start, float freq)
    {
        int ss = (int)(start * sr), len = (int)(0.3f * sr);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            int idx = ss + i; if (idx < 0 || idx >= buf.Length) continue;
            float t = (float)i / sr;
            float env = MathF.Exp(-t * 8f);
            phase += 2f * MathF.PI * freq / sr;
            buf[idx] += MathF.Sin(phase) * env * 0.35f;
        }
    }

    static void ApplySidechain(float[] target, float[] kick)
    {
        float env = 0f;
        float decay = MathF.Exp(-1f / (0.15f * 44100f));
        for (int i = 0; i < target.Length; i++)
        {
            float s = MathF.Abs(kick[i]);
            env = MathF.Max(s, env * decay);
            float duck = 1f - 0.55f * MathF.Min(1f, env * 2f);
            target[i] *= duck;
        }
    }

    static void ApplyReverb(float[] buf, int sr, float mix, float decay, float delayMs)
    {
        int[] taps = {
            (int)(delayMs * sr / 1000f),
            (int)(delayMs * 1.7f * sr / 1000f),
            (int)(delayMs * 2.3f * sr / 1000f)
        };
        float[] dry = (float[])buf.Clone();
        for (int i = 0; i < buf.Length; i++)
        {
            float wet = 0f;
            for (int d = 0; d < taps.Length; d++)
            {
                if (i >= taps[d])
                    wet += dry[i - taps[d]] * MathF.Pow(decay, d + 1);
            }
            buf[i] = dry[i] * (1f - mix) + wet * mix;
        }
    }

    static byte[] BuildWav(byte[] pcm, int sr, short ch, short bps)
    {
        int byteRate = sr * ch * bps / 8;
        short align = (short)(ch * bps / 8);
        int ds = pcm.Length, fs = 36 + ds;
        byte[] wav = new byte[44 + ds];
        int p = 0;
        void WStr(string s) { foreach (var c in s) wav[p++] = (byte)c; }
        void WInt(int v) { BitConverter.GetBytes(v).CopyTo(wav, p); p += 4; }
        void WShort(short v) { BitConverter.GetBytes(v).CopyTo(wav, p); p += 2; }
        WStr("RIFF"); WInt(fs); WStr("WAVE");
        WStr("fmt "); WInt(16); WShort(1); WShort(ch);
        WInt(sr); WInt(byteRate); WShort(align); WShort(bps);
        WStr("data"); WInt(ds);
        pcm.CopyTo(wav, 44);
        return wav;
    }

    // ================== ЗАГРУЗКА ==================
    static void UpdateLoading(float dt)
    {
        loadTimer += dt;
        spinnerAngle += 320f * dt;
        if (spinnerAngle >= 360f) spinnerAngle -= 360f;
        if (loadTimer >= LOAD_DURATION) state = GameState.MainMenu;
    }

    static void DrawLoading()
    {
        Raylib.DrawTexture(bgTexture, 0, 0, Color.White);
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 110));
        string title = "STEEL RAIN";
        int ts = 64; int tw = Raylib.MeasureText(title, ts);
        Raylib.DrawText(title, (screenW - tw) / 2, 110, ts, Color.RayWhite);
        string sub = "HEAVY WARFARE";
        int sw = Raylib.MeasureText(sub, 26);
        Raylib.DrawText(sub, (screenW - sw) / 2, 185, 26, new Color(180, 190, 200, 255));
        Vector2 c = new Vector2(screenW / 2f, screenH / 2f + 20);
        Raylib.DrawRing(c, 44, 46, 0, 360, 64, new Color(255, 255, 255, 40));
        Raylib.DrawRing(c, 38, 46, spinnerAngle, spinnerAngle + 110, 48, Color.RayWhite);
        double rad = (spinnerAngle + 110.0) * Math.PI / 180.0;
        Vector2 tip = new(c.X + (float)Math.Cos(rad) * 42f, c.Y + (float)Math.Sin(rad) * 42f);
        Raylib.DrawCircleV(tip, 5, new Color(255, 230, 140, 255));
        int pct = (int)MathF.Min(100, loadTimer / LOAD_DURATION * 100f);
        string pctText = pct + "%";
        int pw = Raylib.MeasureText(pctText, 20);
        Raylib.DrawText(pctText, (screenW - pw) / 2, (int)c.Y - 10, 20, Color.RayWhite);
        int dots = (int)(loadTimer * 2) % 4;
        string loadText = "LOADING ARMAMENT" + new string('.', dots);
        int lw = Raylib.MeasureText(loadText, 22);
        Raylib.DrawText(loadText, (screenW - lw) / 2, (int)c.Y + 90, 22,
            new Color(170, 180, 190, 255));
    }

    // ================== МЕНЮ ==================
    static void DrawMainMenu()
    {
        Raylib.DrawTexture(bgTexture, 0, 0, Color.White);
        for (int x = 0; x < 760; x++)
        {
            int a = (int)(210 * (1 - x / 760f));
            Raylib.DrawRectangle(x, 0, 1, screenH,
                new Color((byte)0, (byte)0, (byte)0, (byte)a));
        }

        bool interactable = !settingsOpen;

        int logoX = 60, logoY = 56;
        Raylib.DrawText("STEEL", logoX, logoY, 88, AccentGold);
        int steelW = Raylib.MeasureText("STEEL", 88);
        Raylib.DrawText(" RAIN", logoX + steelW, logoY, 88, TextPrimary);
        Raylib.DrawRectangle(logoX, logoY + 100, 360, 3, AccentGold);
        Raylib.DrawText("HEAVY WARFARE", logoX, logoY + 114, 22, TextDim);

        int bx = 60, by = 340;
        int bw = 340;
        int bhPrimary = 84, bhSecondary = 62, gap = 14;

        if (DrawMenuButton(new Rectangle(bx, by, bw, bhPrimary), "PLAY", true, interactable))
            state = GameState.Playing;
        by += bhPrimary + gap;
        if (DrawMenuButton(new Rectangle(bx, by, bw, bhSecondary), "SETTINGS", false, interactable))
            settingsOpen = true;
        by += bhSecondary + gap;
        if (DrawMenuButton(new Rectangle(bx, by, bw, bhSecondary), "QUIT", false, interactable))
            Raylib.CloseWindow();

        Raylib.DrawText("OPERATOR // READY", 60, screenH - 66, 18, TextDim);
        Raylib.DrawText("STEEL RAIN  v0.1 ALPHA", 60, screenH - 40, 16, TextDim);

        Vector2 gearPos = new(screenW - 60, 60);
        if (DrawGearButton(gearPos, 22, settingsOpen, interactable))
            settingsOpen = !settingsOpen;

        if (settingsOpen) DrawSettingsOverlay();
        if (settingsOpen && Raylib.IsKeyPressed(KeyboardKey.Escape))
            settingsOpen = false;
    }

    static bool DrawMenuButton(Rectangle r, string text, bool primary, bool enabled)
    {
        Vector2 mp = Raylib.GetMousePosition();
        bool hover = enabled && Raylib.CheckCollisionPointRec(mp, r);
        float shift = hover ? 10f : 0f;
        Rectangle drawn = new(r.X + shift, r.Y, r.Width, r.Height);
        Color stripe = primary
            ? (hover ? AccentGoldBright : AccentGold)
            : (hover ? AccentGold : TextDim);
        Raylib.DrawRectangleRec(new Rectangle(drawn.X, drawn.Y, 6, drawn.Height), stripe);
        Color bg = primary
            ? (hover ? AccentGoldBright : AccentGold)
            : (hover ? DarkPanelHover : DarkPanel);
        Raylib.DrawRectangleRec(
            new Rectangle(drawn.X + 6, drawn.Y, drawn.Width - 6, drawn.Height), bg);
        if (!primary && hover)
            Raylib.DrawRectangleLinesEx(drawn, 2, AccentGold);
        Color textColor = primary ? new Color(15, 18, 24, 255) : TextPrimary;
        int ts = primary ? 38 : 26;
        int th = Raylib.MeasureText(text, ts);
        int tx = (int)(drawn.X + 30);
        int ty = (int)(drawn.Y + (drawn.Height - ts) / 2);
        Raylib.DrawText(text, tx, ty, ts, textColor);
        if (primary)
            Raylib.DrawText(">", tx + th + 20, ty + 6, 24, new Color(15, 18, 24, 180));
        return hover && enabled && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    static bool DrawGearButton(Vector2 center, float size, bool active, bool enabled)
    {
        Vector2 mp = Raylib.GetMousePosition();
        bool hover = enabled && Vector2.Distance(mp, center) < size + 10;
        Color col = active ? AccentGold : (hover ? TextPrimary : TextDim);
        Color bgCol = (active || hover)
            ? new Color(30, 36, 46, 200)
            : new Color(0, 0, 0, 120);
        Raylib.DrawCircleV(center, size + 10, bgCol);
        DrawGearIcon(center, size, col);
        return hover && enabled && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    static void DrawGearIcon(Vector2 c, float r, Color color)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.PI / 4;
            Vector2 p = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * (r * 0.82f);
            Raylib.DrawCircleV(p, r * 0.22f, color);
        }
        Raylib.DrawRing(c, r * 0.42f, r * 0.68f, 0, 360, 24, color);
        Raylib.DrawCircleV(c, r * 0.22f, color);
    }

    static void DrawSettingsOverlay()
    {
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 210));
        int pw = 720, ph = 520;
        int px = (screenW - pw) / 2, py = (screenH - ph) / 2;
        Raylib.DrawRectangleRec(new Rectangle(px, py, pw, ph), new Color(15, 18, 24, 248));
        Raylib.DrawRectangleLinesEx(new Rectangle(px, py, pw, ph), 2, AccentGold);
        Raylib.DrawRectangle(px, py, pw, 64, new Color(20, 26, 34, 255));
        Raylib.DrawText("SETTINGS", px + 30, py + 18, 32, AccentGold);

        Vector2 closePos = new(px + pw - 42, py + 32);
        Vector2 mp = Raylib.GetMousePosition();
        bool closeHover = Vector2.Distance(mp, closePos) < 22;
        Color closeCol = closeHover ? AccentGold : TextDim;
        float sz = 10;
        Raylib.DrawLineEx(closePos - new Vector2(sz, sz), closePos + new Vector2(sz, sz), 2.5f, closeCol);
        Raylib.DrawLineEx(closePos - new Vector2(sz, -sz), closePos + new Vector2(sz, -sz), 2.5f, closeCol);
        if (closeHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
            settingsOpen = false;

        int sx = px + 45, sw = pw - 140, y = py + 120;
        DrawSlider(new Rectangle(sx, y, sw, 30), ref mouseSensitivity, 0.1f, 5.0f, "MOUSE SENSITIVITY", "0.0"); y += 95;
        DrawSlider(new Rectangle(sx, y, sw, 30), ref masterVolume, 0f, 100f, "MASTER VOLUME", "0"); y += 95;
        DrawSlider(new Rectangle(sx, y, sw, 30), ref fov, 60f, 110f, "FIELD OF VIEW", "0"); y += 120;

        int bw = 220, bh = 56, bxx = px + (pw - bw) / 2;
        if (DrawMenuButton(new Rectangle(bxx, y, bw, bh), "BACK", true, true))
            settingsOpen = false;
    }

    static void DrawSlider(Rectangle area, ref float value, float min, float max, string label, string format)
    {
        Raylib.DrawText(label, (int)area.X, (int)area.Y - 26, 18, TextDim);
        string valStr = value.ToString(format);
        Raylib.DrawText(valStr, (int)(area.X + area.Width + 20), (int)area.Y + 4, 22, TextPrimary);
        Rectangle track = new(area.X, area.Y + 11, area.Width, 8);
        Raylib.DrawRectangleRec(track, new Color(40, 48, 60, 255));
        float t = Math.Clamp((value - min) / (max - min), 0f, 1f);
        Raylib.DrawRectangleRec(new Rectangle(track.X, track.Y, track.Width * t, track.Height), AccentGold);
        Vector2 knob = new(track.X + track.Width * t, track.Y + track.Height / 2);
        Vector2 mp = Raylib.GetMousePosition();
        Rectangle hit = new(track.X - 8, track.Y - 12, track.Width + 16, 32);
        bool hover = Raylib.CheckCollisionPointRec(mp, hit);
        Raylib.DrawCircleV(knob, 10, hover ? Color.White : new Color(230, 235, 240, 255));
        Raylib.DrawRing(knob, 10, 12, 0, 360, 24, new Color(15, 18, 24, 255));
        if (hover && Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            float nt = Math.Clamp((mp.X - track.X) / track.Width, 0f, 1f);
            value = min + nt * (max - min);
        }
    }

    static void DrawPlaying()
    {
        Raylib.ClearBackground(new Color(18, 22, 28, 255));
        Raylib.DrawText("STEEL RAIN: Heavy Warfare", 10, 10, 32, Color.RayWhite);
        Raylib.DrawText("v0.1 alpha", 10, 50, 18, Color.Gray);
        Raylib.DrawText($"Mouse Sens : {mouseSensitivity:0.0}", 10, 90, 20, Color.RayWhite);
        Raylib.DrawText($"Volume     : {masterVolume:0}", 10, 120, 20, Color.RayWhite);
        Raylib.DrawText($"FOV        : {fov:0}", 10, 150, 20, Color.RayWhite);
        Raylib.DrawText("Press ESC to return to menu", 10, screenH - 40, 18,
            new Color(150, 160, 170, 255));
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            state = GameState.MainMenu;
    }

    static Texture2D GenerateBackground(int w, int h)
    {
        Image img = Raylib.GenImageColor(w, h, new Color(10, 13, 18, 255));
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            byte r = (byte)(10 + 16 * t);
            byte g = (byte)(13 + 20 * t);
            byte b = (byte)(18 + 26 * t);
            Raylib.ImageDrawLine(ref img, 0, y, w, y, new Color(r, g, b, (byte)255));
        }
        var rnd = new Random(1337);
        var rain = new Color(140, 165, 190, 90);
        for (int i = 0; i < 350; i++)
        {
            int x = rnd.Next(0, w), y = rnd.Next(0, h);
            int len = rnd.Next(8, 24), dx = rnd.Next(-2, 3);
            Raylib.ImageDrawLine(ref img, x, y, x + dx, y + len, rain);
        }
        int horizon = (int)(h * 0.78f);
        Raylib.ImageDrawRectangle(ref img, 0, horizon, w, h - horizon, new Color(8, 10, 14, 255));
        Raylib.ImageDrawLine(ref img, 0, horizon, w, horizon, new Color(70, 80, 95, 255));
        int x2 = 0;
        while (x2 < w)
        {
            int bw = rnd.Next(30, 90), bh = rnd.Next(25, 130);
            Raylib.ImageDrawRectangle(ref img, x2, horizon - bh, bw, bh, new Color(6, 8, 11, 255));
            if (rnd.Next(0, 3) == 0)
            {
                for (int wy = horizon - bh + 8; wy < horizon - 8; wy += 14)
                    for (int wx = x2 + 6; wx < x2 + bw - 6; wx += 12)
                        if (rnd.Next(0, 4) == 0)
                            Raylib.ImageDrawRectangle(ref img, wx, wy, 3, 4, new Color(230, 190, 90, 200));
            }
            x2 += bw + rnd.Next(0, 15);
        }
        for (int i = 0; i < 40; i++)
        {
            byte a = (byte)(120 - i * 3);
            Raylib.ImageDrawLine(ref img, 0, i, w, i, new Color((byte)0, (byte)0, (byte)0, a));
            Raylib.ImageDrawLine(ref img, 0, h - 1 - i, w, h - 1 - i, new Color((byte)0, (byte)0, (byte)0, a));
        }
        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        return tex;
    }

    static void MakeIconFiles()
    {
        string cwd = Directory.GetCurrentDirectory();
        Console.WriteLine($"[make-icon] рабочая папка: {cwd}");
        Image img = BuildIcon();
        string pngPath = Path.Combine(cwd, "icon.png");
        Raylib.ExportImage(img, pngPath);
        Console.WriteLine($"[make-icon] PNG: {pngPath}");
        string icoPath = Path.Combine(cwd, "icon.ico");
        SaveIcoFromPng(pngPath, icoPath);
        Console.WriteLine($"[make-icon] ICO: {icoPath}");
        Raylib.UnloadImage(img);
        Console.WriteLine("[make-icon] готово!");
    }

    static void SaveIcoFromPng(string pngPath, string icoPath)
    {
        byte[] png = File.ReadAllBytes(pngPath);
        using var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);
        bw.Write((ushort)0); bw.Write((ushort)1); bw.Write((ushort)1);
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)0);
        bw.Write((ushort)1); bw.Write((ushort)32);
        bw.Write((uint)png.Length); bw.Write((uint)(6 + 16));
        bw.Write(png);
    }

    static Image BuildIcon()
    {
        int S = 256;
        Image img = Raylib.GenImageColor(S, S, new Color(20, 24, 30, 255));
        Raylib.ImageDrawRectangleLines(ref img, new Rectangle(4, 4, S - 8, S - 8), 1,
            new Color(90, 101, 114, 255));
        var rain = new Color(159, 184, 204, 160);
        Raylib.ImageDrawLine(ref img, 24, 20, 14, 60, rain);
        Raylib.ImageDrawLine(ref img, 56, 16, 46, 56, rain);
        Raylib.ImageDrawLine(ref img, 200, 20, 210, 60, rain);
        Raylib.ImageDrawLine(ref img, 232, 16, 242, 56, rain);
        Raylib.ImageDrawRectangle(ref img, 96, 60, 64, 22, new Color(200, 205, 210, 255));
        Raylib.ImageDrawTriangle(ref img,
            new Vector2(96, 60), new Vector2(128, 32), new Vector2(160, 60),
            new Color(200, 205, 210, 255));
        Raylib.ImageDrawRectangle(ref img, 96, 82, 64, 130, new Color(210, 175, 60, 255));
        Raylib.ImageDrawRectangle(ref img, 96, 82, 64, 8, new Color(168, 135, 31, 255));
        Raylib.ImageDrawRectangle(ref img, 88, 212, 80, 20, new Color(122, 99, 32, 255));
        Raylib.ImageDrawRectangle(ref img, 108, 92, 8, 112, new Color(255, 240, 150, 150));
        return img;
    }
}