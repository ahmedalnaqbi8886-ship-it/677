using System;
using System.Globalization;
using System.IO;

namespace EaxReverbEngine
{
    // سلسلة التأثيرات (بعد الـ Reverb):
    // Bass Boost -> Treble -> Subwoofer -> EQ 7 نطاقات -> Bass Management -> Crystalliser -> Surround
    // -> اشارة الاختبار -> Volume/Balance/Level
    public sealed class FxChain
    {
        public const int Bands = 7;
        public static readonly float[] Freq = new float[] { 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f };

        private sealed class Bq
        {
            public float b0 = 1f, b1, b2, a1, a2;
            public float[] z1 = new float[2];
            public float[] z2 = new float[2];
        }

        private readonly object _lock = new object();
        private readonly float[] _band = new float[Bands];
        private readonly Bq[] _eq = new Bq[Bands];
        private readonly Bq _bq = new Bq();
        private readonly Bq _tq = new Bq();
        private readonly Bq _sq = new Bq();
        private readonly Bq _lpq = new Bq();
        private readonly float[] _lp = new float[2];
        private float _bass, _treble, _crystal, _surround, _levelDb;
        private float _master = 100f, _balance, _volL = 100f, _volR = 100f, _sub = 50f;
        private float _bassCut = 100f, _bmPct = 100f, _bmCross = 80f;
        private bool _eqOn = true, _bassOn = true, _bmOn;
        private int _spkMode;
        private float _fs = 48000f;
        private float _lpA;
        private float _gL = 1f, _gR = 1f;
        private bool _dirty = true;
        private bool _stereoOn, _mute;
        private float[] _dly = new float[4096];
        private int _dlyPos;
        private float[] _tl = new float[0];
        private float[] _tr = new float[0];
        private float _peakL, _peakR;
        private int _testMode, _testRemain;
        private long _testPos;
        private readonly Random _rnd = new Random();

        public FxChain()
        {
            for (int i = 0; i < Bands; i++) _eq[i] = new Bq();
        }

        private static float Clamp(float v, float lo, float hi)
        {
            if (float.IsNaN(v)) return lo;
            return v < lo ? lo : (v > hi ? hi : v);
        }

        // ---------- خصائص ----------
        public float GetBand(int i) { lock (_lock) { return _band[i]; } }
        public void SetBand(int i, float db)
        {
            if (i < 0 || i >= Bands) return;
            lock (_lock) { _band[i] = Clamp(db, -12f, 12f); _dirty = true; }
        }
        public float Bass { get { lock (_lock) { return _bass; } } set { lock (_lock) { _bass = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Treble { get { lock (_lock) { return _treble; } } set { lock (_lock) { _treble = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Crystal { get { lock (_lock) { return _crystal; } } set { lock (_lock) { _crystal = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Surround { get { lock (_lock) { return _surround; } } set { lock (_lock) { _surround = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float LevelDb { get { lock (_lock) { return _levelDb; } } set { lock (_lock) { _levelDb = Clamp(value, -24f, 0f); _dirty = true; } } }
        public float Master { get { lock (_lock) { return _master; } } set { lock (_lock) { _master = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Balance { get { lock (_lock) { return _balance; } } set { lock (_lock) { _balance = Clamp(value, -100f, 100f); _dirty = true; } } }
        public float VolL { get { lock (_lock) { return _volL; } } set { lock (_lock) { _volL = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float VolR { get { lock (_lock) { return _volR; } } set { lock (_lock) { _volR = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Sub { get { lock (_lock) { return _sub; } } set { lock (_lock) { _sub = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float BassCutoff { get { lock (_lock) { return _bassCut; } } set { lock (_lock) { _bassCut = Clamp(value, 10f, 200f); _dirty = true; } } }
        public float BmPercent { get { lock (_lock) { return _bmPct; } } set { lock (_lock) { _bmPct = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float BmCrossover { get { lock (_lock) { return _bmCross; } } set { lock (_lock) { _bmCross = Clamp(value, 10f, 200f); _dirty = true; } } }
        public bool EqOn { get { lock (_lock) { return _eqOn; } } set { lock (_lock) { _eqOn = value; } } }
        public bool BassOn { get { lock (_lock) { return _bassOn; } } set { lock (_lock) { _bassOn = value; } } }
        public bool BmOn { get { lock (_lock) { return _bmOn; } } set { lock (_lock) { _bmOn = value; } } }
        public bool StereoOn { get { lock (_lock) { return _stereoOn; } } set { lock (_lock) { _stereoOn = value; } } }
        public bool Mute { get { lock (_lock) { return _mute; } } set { lock (_lock) { _mute = value; _dirty = true; } } }
        public int SpeakerMode { get { lock (_lock) { return _spkMode; } } set { lock (_lock) { _spkMode = value < 0 ? 0 : (value > 2 ? 2 : value); } } }

        public void ResetEq()
        {
            lock (_lock)
            {
                for (int i = 0; i < Bands; i++) _band[i] = 0f;
                _eqOn = true; _levelDb = 0f; _stereoOn = false; _dirty = true;
            }
        }
        public void ResetMixer()
        {
            lock (_lock)
            {
                _mute = false; _master = 100f; _balance = 0f; _volL = 100f; _volR = 100f; _sub = 50f;
                _treble = 0f; _crystal = 0f; _surround = 0f; _dirty = true;
            }
        }
        public void ResetSpeaker()
        {
            lock (_lock)
            {
                _bass = 0f; _bassOn = true; _bassCut = 100f;
                _bmOn = false; _bmPct = 100f; _bmCross = 80f; _spkMode = 0; _dirty = true;
            }
        }
        public void Reset() { ResetEq(); ResetMixer(); ResetSpeaker(); }

        // اشارة اختبار: 1 = Channel (يمين/يسار بالتناوب)، 2 = Noise
        public int TestMode { get { lock (_lock) { return _testRemain > 0 ? _testMode : 0; } } }
        public void StartTest(int mode)
        {
            lock (_lock) { _testMode = mode; _testPos = 0; _testRemain = (int)(_fs * 6f); }
        }
        public void StopTest() { lock (_lock) { _testRemain = 0; } }

        public void ReadPeaks(out float l, out float r)
        {
            lock (_lock) { l = _peakL; r = _peakR; _peakL = 0f; _peakR = 0f; }
        }

        // ---------- فلاتر ----------
        private static void Peak(Bq q, double f0, double dB, double fs)
        {
            double A = Math.Pow(10.0, dB / 40.0);
            double w = 2.0 * Math.PI * f0 / fs;
            double alpha = Math.Sin(w) / (2.0 * 1.2);
            double cw = Math.Cos(w);
            double a0 = 1.0 + alpha / A;
            q.b0 = (float)((1.0 + alpha * A) / a0);
            q.b1 = (float)((-2.0 * cw) / a0);
            q.b2 = (float)((1.0 - alpha * A) / a0);
            q.a1 = (float)((-2.0 * cw) / a0);
            q.a2 = (float)((1.0 - alpha / A) / a0);
        }

        private static void Shelf(Bq q, bool high, double f0, double dB, double fs)
        {
            double A = Math.Pow(10.0, dB / 40.0);
            double w = 2.0 * Math.PI * f0 / fs;
            double cw = Math.Cos(w);
            double alpha = Math.Sin(w) / 2.0 * 1.41421356;
            double sA = 2.0 * Math.Sqrt(A) * alpha;
            double b0, b1, b2, a0, a1, a2;
            if (!high)
            {
                b0 = A * ((A + 1) - (A - 1) * cw + sA);
                b1 = 2 * A * ((A - 1) - (A + 1) * cw);
                b2 = A * ((A + 1) - (A - 1) * cw - sA);
                a0 = (A + 1) + (A - 1) * cw + sA;
                a1 = -2 * ((A - 1) + (A + 1) * cw);
                a2 = (A + 1) + (A - 1) * cw - sA;
            }
            else
            {
                b0 = A * ((A + 1) + (A - 1) * cw + sA);
                b1 = -2 * A * ((A - 1) + (A + 1) * cw);
                b2 = A * ((A + 1) + (A - 1) * cw - sA);
                a0 = (A + 1) - (A - 1) * cw + sA;
                a1 = 2 * ((A - 1) - (A + 1) * cw);
                a2 = (A + 1) - (A - 1) * cw - sA;
            }
            q.b0 = (float)(b0 / a0); q.b1 = (float)(b1 / a0); q.b2 = (float)(b2 / a0);
            q.a1 = (float)(a1 / a0); q.a2 = (float)(a2 / a0);
        }

        private static void Lowpass(Bq q, double f0, double fs)
        {
            double w = 2.0 * Math.PI * f0 / fs;
            double cw = Math.Cos(w);
            double alpha = Math.Sin(w) / (2.0 * 0.70710678);
            double a0 = 1.0 + alpha;
            q.b0 = (float)(((1.0 - cw) / 2.0) / a0);
            q.b1 = (float)((1.0 - cw) / a0);
            q.b2 = (float)(((1.0 - cw) / 2.0) / a0);
            q.a1 = (float)((-2.0 * cw) / a0);
            q.a2 = (float)((1.0 - alpha) / a0);
        }

        private static float VolGain(float v)
        {
            if (v <= 0.5f) return 0f;
            return (float)Math.Pow(10.0, (-60.0 * (1.0 - v / 100.0)) / 20.0);
        }

        private void Recalc()
        {
            for (int i = 0; i < Bands; i++) Peak(_eq[i], Freq[i], _band[i], _fs);
            Shelf(_bq, false, _bassCut, _bass * 0.14, _fs);
            Shelf(_tq, true, 6500.0, _treble * 0.12, _fs);
            Shelf(_sq, false, 120.0, (_sub - 50.0) * 0.24, _fs);
            Lowpass(_lpq, _bmCross, _fs);
            _lpA = (float)Math.Exp(-2.0 * Math.PI * 3000.0 / _fs);
            float lv = (float)Math.Pow(10.0, _levelDb / 20.0);
            float vm = _mute ? 0f : VolGain(_master);
            float bl = _balance > 0f ? 1f - _balance / 100f : 1f;
            float br = _balance < 0f ? 1f + _balance / 100f : 1f;
            _gL = lv * vm * VolGain(_volL) * bl;
            _gR = lv * vm * VolGain(_volR) * br;
            _dirty = false;
        }

        private static void Run(Bq q, float[] x, int ch, int n)
        {
            float z1 = q.z1[ch], z2 = q.z2[ch];
            for (int i = 0; i < n; i++)
            {
                float s = x[i];
                float y = q.b0 * s + z1;
                z1 = q.b1 * s - q.a1 * y + z2;
                z2 = q.b2 * s - q.a2 * y;
                x[i] = y;
            }
            q.z1[ch] = z1; q.z2[ch] = z2;
        }

        public void Process(float[] l, float[] r, int n, int rate)
        {
            lock (_lock)
            {
                if (rate > 0 && Math.Abs(rate - _fs) > 0.5f) { _fs = rate; _dirty = true; }
                if (_dirty) Recalc();

                if (_bassOn && _bass > 0.5f) { Run(_bq, l, 0, n); Run(_bq, r, 1, n); }
                if (_treble > 0.5f) { Run(_tq, l, 0, n); Run(_tq, r, 1, n); }
                if (Math.Abs(_sub - 50f) > 0.5f) { Run(_sq, l, 0, n); Run(_sq, r, 1, n); }
                if (_eqOn)
                {
                    for (int b = 0; b < Bands; b++)
                    {
                        if (Math.Abs(_band[b]) < 0.01f) continue;
                        Run(_eq[b], l, 0, n); Run(_eq[b], r, 1, n);
                    }
                }
                if (_bmOn)
                {
                    if (_tl.Length < n) { _tl = new float[n]; _tr = new float[n]; }
                    Array.Copy(l, _tl, n); Array.Copy(r, _tr, n);
                    Run(_lpq, _tl, 0, n); Run(_lpq, _tr, 1, n);
                    float p = _bmPct * 0.01f;
                    for (int i = 0; i < n; i++)
                    {
                        float lo = 0.5f * (_tl[i] + _tr[i]);
                        l[i] = (l[i] - _tl[i]) + lo * p;
                        r[i] = (r[i] - _tr[i]) + lo * p;
                    }
                }
                if (_crystal > 0.5f)
                {
                    float amt = _crystal * 0.01f * 0.6f;
                    for (int i = 0; i < n; i++)
                    {
                        float sl = l[i], sr = r[i];
                        _lp[0] = (1f - _lpA) * sl + _lpA * _lp[0];
                        _lp[1] = (1f - _lpA) * sr + _lpA * _lp[1];
                        l[i] = sl + amt * (float)Math.Tanh((sl - _lp[0]) * 4f);
                        r[i] = sr + amt * (float)Math.Tanh((sr - _lp[1]) * 4f);
                    }
                }
                if (_stereoOn)
                {
                    // ستيريو حقيقي: القناة اليسرى كما هي، واليمنى نسخة متأخرة زمنياً (حيلة Haas)
                    // + فرق بسيط بالتردد بين القناتين حتى تحس فرق يمين/يسار واضح بالسماعة
                    int dl = (int)(_fs * 0.021f);
                    if (dl < 8) dl = 8;
                    if (dl >= _dly.Length) dl = _dly.Length - 1;
                    for (int i = 0; i < n; i++)
                    {
                        float src = l[i];
                        int rp = _dlyPos - dl; if (rp < 0) rp += _dly.Length;
                        float delayed = _dly[rp];
                        _dly[_dlyPos] = src;
                        _dlyPos++; if (_dlyPos >= _dly.Length) _dlyPos = 0;
                        l[i] = src * 0.92f;
                        r[i] = delayed * 0.92f + src * 0.18f;
                    }
                }
                else if (_surround > 0.5f)
                {
                    float k = 1f + _surround * 0.01f * 1.5f;
                    float dm = _surround * 0.01f * 0.6f;
                    int dl = (int)(_fs * 0.022f);
                    if (dl < 8) dl = 8;
                    if (dl >= _dly.Length) dl = _dly.Length - 1;
                    for (int i = 0; i < n; i++)
                    {
                        float m = 0.5f * (l[i] + r[i]);
                        int rp = _dlyPos - dl; if (rp < 0) rp += _dly.Length;
                        float d = _dly[rp];
                        _dly[_dlyPos] = m;
                        _dlyPos++; if (_dlyPos >= _dly.Length) _dlyPos = 0;
                        float sd = 0.5f * (l[i] - r[i]) * k + dm * d;
                        l[i] = m + sd; r[i] = m - sd;
                    }
                }
                if (_testRemain > 0)
                {
                    int cnt = n < _testRemain ? n : _testRemain;
                    for (int i = 0; i < cnt; i++)
                    {
                        if (_testMode == 1)
                        {
                            double t = _testPos / (double)_fs;
                            float s = 0.2f * (float)Math.Sin(2.0 * Math.PI * 440.0 * t);
                            if (((long)t) % 2 == 0) l[i] += s; else r[i] += s;
                        }
                        else
                        {
                            float nz = ((float)_rnd.NextDouble() * 2f - 1f) * 0.15f;
                            l[i] += nz; r[i] += nz;
                        }
                        _testPos++;
                    }
                    _testRemain -= cnt;
                }
                float pl = _peakL, pr = _peakR;
                for (int i = 0; i < n; i++)
                {
                    l[i] *= _gL; r[i] *= _gR;
                    float al = l[i] < 0f ? -l[i] : l[i];
                    float ar = r[i] < 0f ? -r[i] : r[i];
                    if (al > pl) pl = al;
                    if (ar > pr) pr = ar;
                }
                _peakL = pl; _peakR = pr;
            }
        }

        // ---------- حفظ / تحميل (fx.ini) ----------
        private static string F(float v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }

        public void Save(string dir)
        {
            lock (_lock)
            {
                System.Collections.Generic.List<string> L = new System.Collections.Generic.List<string>();
                for (int i = 0; i < Bands; i++) L.Add("Band" + i + "=" + F(_band[i]));
                L.Add("EqOn=" + (_eqOn ? "1" : "0"));
                L.Add("Bass=" + F(_bass));
                L.Add("BassOn=" + (_bassOn ? "1" : "0"));
                L.Add("BassCut=" + F(_bassCut));
                L.Add("Treble=" + F(_treble));
                L.Add("Crystal=" + F(_crystal));
                L.Add("Surround=" + F(_surround));
                L.Add("Stereo=" + (_stereoOn ? "1" : "0"));
                L.Add("Level=" + F(_levelDb));
                L.Add("Master=" + F(_master));
                L.Add("Balance=" + F(_balance));
                L.Add("VolL=" + F(_volL));
                L.Add("VolR=" + F(_volR));
                L.Add("Sub=" + F(_sub));
                L.Add("BmOn=" + (_bmOn ? "1" : "0"));
                L.Add("BmPct=" + F(_bmPct));
                L.Add("BmCross=" + F(_bmCross));
                L.Add("SpkMode=" + _spkMode);
                File.WriteAllLines(Path.Combine(dir, "fx.ini"), L.ToArray());
            }
        }

        public void Load(string dir)
        {
            string path = Path.Combine(dir, "fx.ini");
            if (!File.Exists(path)) return;
            foreach (string line in File.ReadAllLines(path))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq).Trim();
                float v;
                if (!float.TryParse(line.Substring(eq + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) continue;
                if (k.StartsWith("Band"))
                {
                    int i;
                    if (int.TryParse(k.Substring(4), out i)) SetBand(i, v);
                }
                else if (k == "EqOn") EqOn = v != 0f;
                else if (k == "Bass") Bass = v;
                else if (k == "BassOn") BassOn = v != 0f;
                else if (k == "BassCut") BassCutoff = v;
                else if (k == "Treble") Treble = v;
                else if (k == "Crystal") Crystal = v;
                else if (k == "Surround") Surround = v;
                else if (k == "Stereo") StereoOn = v > 0.5f;
                else if (k == "Level") LevelDb = v;
                else if (k == "Master") Master = v;
                else if (k == "Balance") Balance = v;
                else if (k == "VolL") VolL = v;
                else if (k == "VolR") VolR = v;
                else if (k == "Sub") Sub = v;
                else if (k == "BmOn") BmOn = v != 0f;
                else if (k == "BmPct") BmPercent = v;
                else if (k == "BmCross") BmCrossover = v;
                else if (k == "SpkMode") SpeakerMode = (int)v;
            }
        }
    }
}
