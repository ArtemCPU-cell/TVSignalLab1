using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TVSignalLab;

internal static class Program
{
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    // ==================== МОДЕЛИ ====================
    private sealed class ColorBar
    {
        public string Name { get; set; } = "";
        public double Er { get; set; }
        public double Eg { get; set; }
        public double Eb { get; set; }
    }

    private sealed class ComputedSignal
    {
        public string Name { get; set; } = "";
        public double Er { get; set; }
        public double Eg { get; set; }
        public double Eb { get; set; }
        public double Ey { get; set; }
        public double ErY { get; set; }
        public double EbY { get; set; }
        public double Ecr { get; set; }
        public double Ecb { get; set; }
        public byte Yq { get; set; }
        public byte Crq { get; set; }
        public byte Cbq { get; set; }
        public string Ybin { get; set; } = "";
        public string Crbin { get; set; } = "";
        public string Cbbin { get; set; } = "";
    }

    private sealed class TvFormat
    {
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public string Sub { get; set; } = "";
        public double FyMhz { get; set; }
    }

    // ==================== СТАНДАРТНЫЕ ЦВЕТА ====================
    private static List<ColorBar> StandardColorBars() => new()
    {
        new ColorBar { Name = "Белый",       Er = 1, Eg = 1, Eb = 1 },
        new ColorBar { Name = "Желтый",      Er = 1, Eg = 1, Eb = 0 },
        new ColorBar { Name = "Голубой",     Er = 0, Eg = 1, Eb = 1 },
        new ColorBar { Name = "Зеленый",     Er = 0, Eg = 1, Eb = 0 },
        new ColorBar { Name = "Пурпурный",   Er = 1, Eg = 0, Eb = 1 },
        new ColorBar { Name = "Красный",     Er = 1, Eg = 0, Eb = 0 },
        new ColorBar { Name = "Синий",       Er = 0, Eg = 0, Eb = 1 },
        new ColorBar { Name = "Черный",      Er = 0, Eg = 0, Eb = 0 },
    };

    // ==================== РАСЧЁТ ====================
    private static ComputedSignal Compute(ColorBar cb, int bits)
    {
        double ey = 0.299 * cb.Er + 0.587 * cb.Eg + 0.114 * cb.Eb;
        double erY = cb.Er - ey;
        double ebY = cb.Eb - ey;
        double ecr = 0.713 * erY;
        double ecb = 0.564 * ebY;

        byte yq, crq, cbq;
        if (bits == 8)
        {
            yq = ClampByte(RoundAway(16.0 + 219.0 * ey), 16, 235);
            crq = ClampByte(RoundAway(128.0 + 224.0 * ecr), 16, 240);
            cbq = ClampByte(RoundAway(128.0 + 224.0 * ecb), 16, 240);
        }
        else
        {
            yq = ClampByte((byte)RoundAway(64.0 + 876.0 * ey), 64, 940);
            crq = ClampByte((byte)RoundAway(512.0 + 896.0 * ecr), 64, 960);
            cbq = ClampByte((byte)RoundAway(512.0 + 896.0 * ecb), 64, 960);
        }

        return new ComputedSignal
        {
            Name = cb.Name,
            Er = cb.Er,
            Eg = cb.Eg,
            Eb = cb.Eb,
            Ey = ey,
            ErY = erY,
            EbY = ebY,
            Ecr = ecr,
            Ecb = ecb,
            Yq = yq,
            Crq = crq,
            Cbq = cbq,
            Ybin = ToBin(yq, bits),
            Crbin = ToBin(crq, bits),
            Cbbin = ToBin(cbq, bits)
        };
    }

    private static double RoundAway(double v) => Math.Round(v, MidpointRounding.AwayFromZero);
    private static byte ClampByte(double v, int lo, int hi) => (byte)Math.Clamp(v, lo, hi);
    private static string ToBin(byte v, int bits) => Convert.ToString(v, 2).PadLeft(bits, '0');

    private static List<TvFormat> TvFormats() => new()
    {
        new TvFormat { Name = "СТВ 4:2:2", Class = "Стандартная чёткость (SDTV)", Sub = "4:2:2", FyMhz = 13.5 },
        new TvFormat { Name = "Широко 625/100/1:1", Class = "Широкоформатное", Sub = "4:2:2 (625/100/1:1)", FyMhz = 74.250 },
        new TvFormat { Name = "Широко 625/50/1:1",  Class = "Широкоформатное", Sub = "4:2:2 (625/50/1:1)", FyMhz = 37.125 },
        new TvFormat { Name = "Широко 625/25/1:1",  Class = "Широкоформатное", Sub = "4:2:2 (625/25/1:1; 625/50/2:1)", FyMhz = 18.5625 },
        new TvFormat { Name = "ПЧТВ 4:2:2", Class = "Повышенная чёткость (EDTV)", Sub = "4:2:2", FyMhz = 74.25 },
        new TvFormat { Name = "ВЧТВ 4:2:2", Class = "Высокая чёткость (HDTV)", Sub = "4:2:2", FyMhz = 148.5 },
    };

    private static double BitUs(double mbitPerS) => 1.0 / mbitPerS;

    // ==================== ГЛАВНОЕ МЕНЮ ====================
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        bool demo = args.Length > 0 &&
                    (args[0] == "--demo" || args[0] == "demo" ||
                     args[0] == "--report" || args[0] == "report");

        if (!demo)
        {
            try { Console.Clear(); } catch { }
        }

        if (demo)
        {
            RunDemo();
            return 0;
        }

        while (true)
        {
            try { Console.Clear(); } catch { }
            Console.WriteLine("============================================================");
            Console.WriteLine("  ОЦИФРОВКА ТВ-СИГНАЛА — учебная программа (лабораторная №1)");
            Console.WriteLine("  Предмет: Основы цифрового телевидения");
            Console.WriteLine("============================================================");
            Console.WriteLine(" 1. Стандартный тест «цветные полосы» (по методичке)");
            Console.WriteLine(" 2. Ввести свои значения для всех 8 цветов");
            Console.WriteLine(" 3. Экспорт всех таблиц в CSV (Excel)");
            Console.WriteLine(" 4. Теория и формулы (обучающий раздел)");
            Console.WriteLine(" 5. Отчёт с графиками (HTML + SVG)");
            Console.WriteLine(" 0. Выход");
            Console.Write("> ");
            var key = Console.ReadLine()?.Trim();

            switch (key)
            {
                case "1": RunStandard(); Pause(); break;
                case "2": RunCustomAll(); Pause(); break;
                case "3": ExportCsv(); Pause(); break;
                case "4": ShowTheory(); Pause(); break;
                case "5": GenerateReport(); Pause(); break;
                case "0": return 0;
                default: Console.WriteLine("Неверный выбор."); Pause(); break;
            }
        }
    }

    private static void RunDemo()
    {
        Console.WriteLine("=== НЕИНТЕРАКТИВНЫЙ РЕЖИМ: стандартный тест + отчёт ===\n");
        RunCalculation(StandardColorBars(), 8, showDetails: true);
        GenerateReport();
        ExportCsvSilent();
        Console.WriteLine("\nГотово. Отчёт: report.html, таблицы: *.csv (рядом с программой).");
    }

    private static void Pause()
    {
        if (Console.IsInputRedirected) return;
        Console.WriteLine("\nНажмите Enter для продолжения...");
        Console.ReadLine();
    }

    // ==================== ВАРИАНТЫ РАСЧЁТА ====================
    private static void RunStandard()
    {
        RunCalculation(StandardColorBars(), 8, showDetails: true);
    }

    private static void RunCustomAll()
    {
        var bars = new List<ColorBar>();
        string[] names = { "Белый", "Желтый", "Голубой", "Зеленый", "Пурпурный", "Красный", "Синий", "Черный" };
        Console.WriteLine("\nВведите компоненты ER, EG, EB (0 или 1) для каждого цвета:");
        foreach (var nm in names)
        {
            Console.WriteLine($"\n--- {nm} ---");
            var cb = new ColorBar { Name = nm };
            cb.Er = Ask01("  ER: ");
            cb.Eg = Ask01("  EG: ");
            cb.Eb = Ask01("  EB: ");
            bars.Add(cb);
        }
        RunCalculation(bars, 8, showDetails: true);
    }

    private static double Ask01(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = Console.ReadLine();
            if (double.TryParse(s, NumberStyles.Any, CI, out var v) && (v == 0 || v == 1))
                return v;
            Console.WriteLine("  Введите 0 или 1.");
        }
    }

    // ==================== ТАБЛИЦЫ В КОНСОЛЬ ====================
    private static void RunCalculation(List<ColorBar> bars, int bits, bool showDetails)
    {
        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine($"ЭТАП 1. Испытательный сигнал «цветные полосы» (таблица 10)");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"{"Цвет",-12} {"ER",-5} {"EG",-5} {"EB",-5}");
        foreach (var b in bars)
            Console.WriteLine($"{b.Name,-12} {b.Er,-5:G} {b.Eg,-5:G} {b.Eb,-5:G}");

        var signals = bars.Select(b => Compute(b, bits)).ToList();

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine($"ЭТАП 2–3. Служебные и сжатые сигналы (таблицы 11–12)");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"{"Цвет",-12} {"EY",-9} {"ER-Y",-10} {"EB-Y",-10} {"ECR",-9} {"ECB",-9}");
        foreach (var s in signals)
            Console.WriteLine($"{s.Name,-12} {s.Ey,-9:F3} {s.ErY,-10:F3} {s.EbY,-10:F3} {s.Ecr,-9:F3} {s.Ecb,-9:F3}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine($"ЭТАП 4. {bits}-разрядное квантование (таблица 13)");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"{"Цвет",-12} {"Y",-5} {"CR",-5} {"CB",-5}");
        foreach (var s in signals)
            Console.WriteLine($"{s.Name,-12} {s.Yq,-5} {s.Crq,-5} {s.Cbq,-5}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine($"ЭТАП 5. {bits}-битный двоичный код (таблица 14)");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"{"Цвет",-12} {"Y (bin)",-11} {"CR (bin)",-11} {"CB (bin)",-11}");
        foreach (var s in signals)
            Console.WriteLine($"{s.Name,-12} {s.Ybin,-11} {s.Crbin,-11} {s.Cbbin,-11}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("ЭТАП 6. Скорость передачи данных, C = f·k (таблица 15)");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"{"Формат",-30} {"CY",-9} {"CR-Y",-9} {"CB-Y",-9} {"Сумма",-9}");
        foreach (var fmt in TvFormats())
        {
            double cy = fmt.FyMhz * bits;
            double ccr = fmt.FyMhz * bits / 2.0;
            double ccb = ccr;
            Console.WriteLine($"{fmt.Name,-30} {cy,-9:F2} {ccr,-9:F2} {ccb,-9:F2} {cy + ccr + ccb,-9:F2}");
        }

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("ЭТАП 7. Длительность бита τ = 1/C (таблица 16)");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"{"Формат",-30} {"τY, мкс",-11} {"τCR-Y, мкс",-13} {"τCB-Y, мкс",-13}");
        foreach (var fmt in TvFormats())
        {
            double cy = fmt.FyMhz * bits;
            double ccr = fmt.FyMhz * bits / 2.0;
            Console.WriteLine($"{fmt.Name,-30} {BitUs(cy),-11:F5} {BitUs(ccr),-13:F5} {BitUs(ccr),-13:F5}");
        }

        if (showDetails)
        {
            Console.WriteLine("\n================================================================");
            Console.WriteLine("Промежуточные выводы:");
            Console.WriteLine(" - EY = 0.299·ER + 0.587·EG + 0.114·EB");
            Console.WriteLine(" - ECR = 0.713·(ER − EY),  ECB = 0.564·(EB − EY)");
            Console.WriteLine($" - 8-бит: Y∈[16,235], CR/CB∈[16,240], ноль цветоразности = 128");
            Console.WriteLine(" - C = f·k; τ = 1/C. При росте чёткости растёт битовая скорость.");
            Console.WriteLine("================================================================");
        }
    }

    // ==================== ЭКСПОРТ CSV ====================
    private static void ExportCsv()
    {
        var bars = StandardColorBars();
        var signals8 = bars.Select(b => Compute(b, 8)).ToList();
        var signals10 = bars.Select(b => Compute(b, 10)).ToList();
        var formats = TvFormats();
        var dir = AppContext.BaseDirectory;

        WriteCsv(Path.Combine(dir, "colorbars.csv"), new[] { "Цвет", "ER", "EG", "EB" },
            bars.Select(b => new[] { b.Name, b.Er.ToString(CI), b.Eg.ToString(CI), b.Eb.ToString(CI) }));

        WriteCsv(Path.Combine(dir, "signals_8bit.csv"),
            new[] { "Цвет", "EY", "ER-Y", "EB-Y", "ECR", "ECB", "Y", "CR", "CB", "Y_bin", "CR_bin", "CB_bin" },
            signals8.Select(s => new[]
            {
                s.Name, s.Ey.ToString("F4", CI), s.ErY.ToString("F4", CI), s.EbY.ToString("F4", CI),
                s.Ecr.ToString("F4", CI), s.Ecb.ToString("F4", CI),
                s.Yq.ToString(), s.Crq.ToString(), s.Cbq.ToString(),
                s.Ybin, s.Crbin, s.Cbbin
            }));

        WriteCsv(Path.Combine(dir, "signals_10bit.csv"),
            new[] { "Цвет", "EY", "ER-Y", "EB-Y", "ECR", "ECB", "Y", "CR", "CB", "Y_bin", "CR_bin", "CB_bin" },
            signals10.Select(s => new[]
            {
                s.Name, s.Ey.ToString("F4", CI), s.ErY.ToString("F4", CI), s.EbY.ToString("F4", CI),
                s.Ecr.ToString("F4", CI), s.Ecb.ToString("F4", CI),
                s.Yq.ToString(), s.Crq.ToString(), s.Cbq.ToString(),
                s.Ybin, s.Crbin, s.Cbbin
            }));

        WriteCsv(Path.Combine(dir, "bitrate.csv"),
            new[] { "Формат", "Класс", "fY, МГц", "CY, Мбит/с", "CR-Y, Мбит/с", "CB-Y, Мбит/с", "Сумма, Мбит/с", "τY, мкс", "τCR-Y, мкс" },
            formats.Select(f =>
            {
                double cy = f.FyMhz * 8, cr = f.FyMhz * 4;
                return new[]
                {
                    f.Name, f.Class, f.FyMhz.ToString(CI),
                    cy.ToString("F2", CI), cr.ToString("F2", CI), cr.ToString("F2", CI),
                    (cy + cr + cr).ToString("F2", CI),
                    BitUs(cy).ToString("F6", CI), BitUs(cr).ToString("F6", CI)
                };
            }));

        Console.WriteLine("\nCSV-файлы сохранены в:");
        Console.WriteLine("  " + Path.Combine(dir, "colorbars.csv"));
        Console.WriteLine("  " + Path.Combine(dir, "signals_8bit.csv"));
        Console.WriteLine("  " + Path.Combine(dir, "signals_10bit.csv"));
        Console.WriteLine("  " + Path.Combine(dir, "bitrate.csv"));
        Console.WriteLine("Откройте их в Excel (CSV разделитель — запятая).");
    }

    private static void ExportCsvSilent()
    {
        var bars = StandardColorBars();
        var signals8 = bars.Select(b => Compute(b, 8)).ToList();
        var signals10 = bars.Select(b => Compute(b, 10)).ToList();
        var formats = TvFormats();
        var dir = AppContext.BaseDirectory;

        WriteCsv(Path.Combine(dir, "colorbars.csv"), new[] { "Цвет", "ER", "EG", "EB" },
            bars.Select(b => new[] { b.Name, b.Er.ToString(CI), b.Eg.ToString(CI), b.Eb.ToString(CI) }));

        WriteCsv(Path.Combine(dir, "signals_8bit.csv"),
            new[] { "Цвет", "EY", "ER-Y", "EB-Y", "ECR", "ECB", "Y", "CR", "CB", "Y_bin", "CR_bin", "CB_bin" },
            signals8.Select(s => new[]
            {
                s.Name, s.Ey.ToString("F4", CI), s.ErY.ToString("F4", CI), s.EbY.ToString("F4", CI),
                s.Ecr.ToString("F4", CI), s.Ecb.ToString("F4", CI),
                s.Yq.ToString(), s.Crq.ToString(), s.Cbq.ToString(),
                s.Ybin, s.Crbin, s.Cbbin
            }));

        WriteCsv(Path.Combine(dir, "bitrate.csv"),
            new[] { "Формат", "Класс", "fY, МГц", "CY, Мбит/с", "CR-Y, Мбит/с", "CB-Y, Мбит/с", "Сумма, Мбит/с", "τY, мкс", "τCR-Y, мкс" },
            formats.Select(f =>
            {
                double cy = f.FyMhz * 8, cr = f.FyMhz * 4;
                return new[]
                {
                    f.Name, f.Class, f.FyMhz.ToString(CI),
                    cy.ToString("F2", CI), cr.ToString("F2", CI), cr.ToString("F2", CI),
                    (cy + cr + cr).ToString("F2", CI),
                    BitUs(cy).ToString("F6", CI), BitUs(cr).ToString("F6", CI)
                };
            }));
    }

    private static void WriteCsv(string path, string[] header, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", header));
        foreach (var r in rows)
            sb.AppendLine(string.Join(",", r));
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    // ==================== ТЕОРИЯ ====================
    private static void ShowTheory()
    {
        Console.Clear();
        Console.WriteLine("================================================================");
        Console.WriteLine("ОБУЧАЮЩИЙ РАЗДЕЛ: этапы оцифровки ТВ-сигнала");
        Console.WriteLine("================================================================");
        Console.WriteLine(@"
Этап 1 — Дискретизация по времени.
  Теорема Котельникова: f_discret >= 2 · f_макс (формула 1).
  На практике выбирают больше, чем в 2 раза (сверхдискретизация).

Этап 2 — Квантование по уровню.
  Число уровней N = 2^k, где k — разрядность кода (формула 2).
  Шаг квантования Δ = (макс − мин) / (N − 1).

Этап 3 — Кодирование (присвоение двоичного кода) — ИКМ.

ТВ-сигнал разлагается на компоненты:
  Y (яркость), R−Y и B−Y (цветоразностные).
  Сжатые: ECR = 0.713·(R−Y),  ECB = 0.564·(B−Y) (формула 16).

Гамма-коррекция (формула 5): выход = макс · (вход/макс)^γ, γ = 0.45.
  Сжимает динамический диапазон vicino к черному уровню.

Квантование 8 бит (формулы 6, 7):
  Y  = Round(16 + 219·EY),      диапазон [16, 235]
  CR = Round(128 + 224·ECR),    диапазон [16, 240], ноль = 128
  CB = Round(128 + 224·ECB),    диапазон [16, 240], ноль = 128

Скорость передачи (формула 3): C = f_discret · k.
Длительность бита (формула 4): τ = 1 / C.

Форматы дискретизации (соотношение выборок Y:CR:CB):
  4:4:4 — полная выборка обоих цветовых компонентов в каждой строке.
  4:2:2 — цветовая выборка вдвое реже по горизонтали (стандарт 601).
  4:2:0 — ещё и вдвое реже по вертикали (каждая вторая строка).
  4:1:1 — цветовая выборка в 4 раза реже по горизонтали.

При росте качества (SDTV → EDTV → HDTV) растёт:
  частота дискретизации, битовая скорость, ширина полосы.
");
    }

    // ==================== ОТЧЁТ HTML + SVG ====================
    private static void GenerateReport()
    {
        var bars = StandardColorBars();
        var signals8 = bars.Select(b => Compute(b, 8)).ToList();
        var signals10 = bars.Select(b => Compute(b, 10)).ToList();
        var formats = TvFormats();

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"UTF-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("<title>Отчёт: Оцифровка ТВ-сигнала</title>");
        html.AppendLine("<style>");
        html.AppendLine("*{box-sizing:border-box;} body{font-family:Arial,sans-serif;padding:16px;line-height:1.5;color:#222;max-width:1500px;margin:0 auto;}");
        html.AppendLine("table{border-collapse:collapse;width:100%;margin:16px 0;}");
        html.AppendLine("th,td{border:1px solid #999;padding:5px 8px;text-align:center;font-size:13px;}");
        html.AppendLine("th{background:#eef;} h2{border-bottom:2px solid #333;padding-bottom:6px;font-size:17px;}");
        html.AppendLine(".note{color:#555;font-size:0.9em;} .chart{border:1px solid #ccc;border-radius:6px;padding:8px;margin:18px 0;background:#fff;overflow-x:auto;} .chart svg{display:block;width:100%;height:auto;}");
        html.AppendLine("@media print{body{max-width:none;padding:8mm;} .chart{break-inside:avoid;} .chart svg{width:100%;}}");
        html.AppendLine(".row{display:flex;gap:16px;flex-wrap:wrap;}");
        html.AppendLine(".col{flex:1;min-width:300px;}");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<h1>Отчёт по лабораторной работе №1</h1>");
        html.AppendLine("<p><b>Дисциплина:</b> Основы цифрового телевидения<br>");
        html.AppendLine("<b>Тема:</b> Цифровое представление ТВ-сигналов</p>");

        // Таблица 10
        html.AppendLine("<h2>1. Испытательный сигнал «цветные полосы» (табл. 10)</h2>");
        html.AppendLine("<table><tr><th>Цвет</th><th>ER</th><th>EG</th><th>EB</th></tr>");
        foreach (var b in bars)
            html.AppendLine($"<tr><td>{b.Name}</td><td>{b.Er:G}</td><td>{b.Eg:G}</td><td>{b.Eb:G}</td></tr>");
        html.AppendLine("</table>");

        // Таблицы 11-12
        html.AppendLine("<h2>2. Служебные и сжатые сигналы (табл. 11–12)</h2>");
        html.AppendLine("<table><tr><th>Цвет</th><th>EY</th><th>ER−Y</th><th>EB−Y</th><th>ECR</th><th>ECB</th></tr>");
        foreach (var s in signals8)
            html.AppendLine($"<tr><td>{s.Name}</td><td>{s.Ey:F3}</td><td>{s.ErY:F3}</td><td>{s.EbY:F3}</td><td>{s.Ecr:F3}</td><td>{s.Ecb:F3}</td></tr>");
        html.AppendLine("</table>");

        // Таблица 13
        html.AppendLine("<h2>3. Квантование 8 бит (табл. 13)</h2>");
        html.AppendLine("<table><tr><th>Цвет</th><th>Y</th><th>CR</th><th>CB</th></tr>");
        foreach (var s in signals8)
            html.AppendLine($"<tr><td>{s.Name}</td><td>{s.Yq}</td><td>{s.Crq}</td><td>{s.Cbq}</td></tr>");
        html.AppendLine("</table>");

        // Таблица 14
        html.AppendLine("<h2>4. Двоичный код (табл. 14)</h2>");
        html.AppendLine("<table><tr><th>Цвет</th><th>Y (bin)</th><th>CR (bin)</th><th>CB (bin)</th></tr>");
        foreach (var s in signals8)
            html.AppendLine($"<tr><td>{s.Name}</td><td>{s.Ybin}</td><td>{s.Crbin}</td><td>{s.Cbbin}</td></tr>");
        html.AppendLine("</table>");

        // Таблица 15
        html.AppendLine("<h2>5. Скорость передачи данных, Мбит/с (табл. 15, k=8)</h2>");
        html.AppendLine("<table><tr><th>Формат</th><th>CY</th><th>CR−Y</th><th>CB−Y</th><th>Сумма</th></tr>");
        foreach (var f in formats)
        {
            double cy = f.FyMhz * 8, cr = f.FyMhz * 4;
            html.AppendLine($"<tr><td>{f.Name}</td><td>{cy:F2}</td><td>{cr:F2}</td><td>{cr:F2}</td><td>{cy + cr + cr:F2}</td></tr>");
        }
        html.AppendLine("</table>");

        // Таблица 16
        html.AppendLine("<h2>6. Длительность бита, мкс (табл. 16)</h2>");
        html.AppendLine("<table><tr><th>Формат</th><th>τY</th><th>τCR−Y</th><th>τCB−Y</th></tr>");
        foreach (var f in formats)
        {
            double cy = f.FyMhz * 8, cr = f.FyMhz * 4;
            html.AppendLine($"<tr><td>{f.Name}</td><td>{BitUs(cy):F5}</td><td>{BitUs(cr):F5}</td><td>{BitUs(cr):F5}</td></tr>");
        }
        html.AppendLine("</table>");

        // ==================== ГРАФИКИ ====================
        html.AppendLine("<h2>Графики (обучающая визуализация)</h2>");
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CI;
        try
        {

            html.AppendLine("<h3>3.1 Амплитуды цветовых компонентов ER, EG, EB</h3>");
            html.AppendLine($"<div class=\"chart\">{BarChart(bars.Select(b => (b.Name, b.Er)), bars.Select(b => (b.Name, b.Eg)), bars.Select(b => (b.Name, b.Eb)), "ER (красный)", "EG (зелёный)", "EB (синий)", 0, 1)}</div>");

            html.AppendLine("<h3>3.2 Служебные сигналы: EY, ER−Y, EB−Y</h3>");
            html.AppendLine($"<div class=\"chart\">{LineChart(signals8.Select(s => (s.Name, s.Ey)), signals8.Select(s => (s.Name, s.ErY)), signals8.Select(s => (s.Name, s.EbY)), "EY (яркость)", "ER−Y", "EB−Y", -1, 1)}</div>");

            html.AppendLine("<h3>3.3 Сжатые цветоразностные ECR, ECB</h3>");
            html.AppendLine($"<div class=\"chart\">{LineChart(signals8.Select(s => (s.Name, s.Ecr)), signals8.Select(s => (s.Name, s.Ecb)), new List<(string name, double v)>(), "ECR", "ECB", "", -0.6, 0.6)}</div>");

            html.AppendLine("<h3>3.4 Квантованные значения (лестница), 8 бит</h3>");
            html.AppendLine($"<div class=\"chart\">{StairChart(signals8.Select(s => (s.Name, (double)s.Yq)), signals8.Select(s => (s.Name, (double)s.Crq)), signals8.Select(s => (s.Name, (double)s.Cbq)), "Y", "CR", "CB", 0, 255)}</div>");

            html.AppendLine("<h3>3.5 Сравнение битовых скоростей</h3>");
            html.AppendLine($"<div class=\"chart\">{GroupedBarChart(formats.Select(f => (f.Name, f.FyMhz * 8)), formats.Select(f => (f.Name, f.FyMhz * 4)), formats.Select(f => (f.Name, f.FyMhz * 4)), "CY", "CR−Y", "CB−Y", 0, 2500)}</div>");

            html.AppendLine("<h3>3.6 Длительность бита τ (мкс)</h3>");
            html.AppendLine($"<div class=\"chart\">{BarChart(formats.Select(f => (f.Name, BitUs(f.FyMhz * 8))), formats.Select(f => (f.Name, BitUs(f.FyMhz * 4))), formats.Select(f => (f.Name, BitUs(f.FyMhz * 4))), "τY", "τCR−Y", "τCB−Y", 0, 0.02)}</div>");

            html.AppendLine("<h3>3.7 Гамма-коррекция (γ = 0.45)</h3>");
            html.AppendLine($"<div class=\"chart\">{GammaChart()}</div>");

            html.AppendLine("<h3>3.8 Дискретизация и квантование (иллюстрация)</h3>");
            html.AppendLine($"<div class=\"chart\">{SamplingChart()}</div>");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        html.AppendLine("<p class=\"note\">Сгенерировано программой «Оцифровка ТВ-сигнала» (C# / .NET).");
        html.AppendLine(" Все расчёты по формулам методической инструкции (1)–(16).</p>");
        html.AppendLine("</body></html>");

        var outPath = Path.Combine(AppContext.BaseDirectory, "report.html");
        File.WriteAllText(outPath, html.ToString(), Encoding.UTF8);
        Console.WriteLine($"\nОтчёт сохранён: {outPath}");
        Console.WriteLine("Откройте файл report.html в браузере — графики отобразятся внутри.");
    }

    // ==================== SVG ХЕЛПЕРЫ ====================

    private static StringBuilder CreateSvg(double width, double height)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width:F0}\" height=\"{height:F0}\" viewBox=\"0 0 {width:F0} {height:F0}\" role=\"img\">");
        sb.AppendLine($"<rect width=\"{width:F0}\" height=\"{height:F0}\" fill=\"#fff\"/>");
        return sb;
    }

    private static void AppendAxes(StringBuilder sb, double width, double height, double left, double top, double bottom)
    {
        sb.AppendLine($"<line x1=\"{left:F1}\" y1=\"{height - bottom:F1}\" x2=\"{width - 25:F1}\" y2=\"{height - bottom:F1}\" stroke=\"#333\" stroke-width=\"1.5\"/>");
        sb.AppendLine($"<line x1=\"{left:F1}\" y1=\"{top:F1}\" x2=\"{left:F1}\" y2=\"{height - bottom:F1}\" stroke=\"#333\" stroke-width=\"1.5\"/>");
    }

    private static void AppendLegend(StringBuilder sb, double x, double y, params (string name, string color)[] series)
    {
        double currentX = x;
        foreach (var item in series)
        {
            sb.AppendLine($"<rect x=\"{currentX:F1}\" y=\"{y - 10:F1}\" width=\"11\" height=\"11\" rx=\"2\" fill=\"{item.color}\"/>");
            sb.AppendLine($"<text x=\"{currentX + 16:F1}\" y=\"{y:F1}\" font-size=\"11\" fill=\"#444\">{item.name}</text>");
            currentX += 42 + item.name.Length * 8.5;
        }
    }

    // Сгруппированная столбчатая диаграмма: столбцы серий не перекрывают друг друга.
    private static string BarChart(
        IEnumerable<(string name, double v)> a,
        IEnumerable<(string name, double v)> b,
        IEnumerable<(string name, double v)> c,
        string la, string lb, string lc, double ymin, double ymax)
    {
        var series = new[] { a.ToList(), b.ToList(), c.ToList() };
        int n = series[0].Count;
        double w = 1200, h = 440, pad = 70, top = 48, bottom = 82;
        double plotHeight = h - top - bottom;
        double groupW = (w - pad - 35) / Math.Max(1, n);
        int seriesCount = string.IsNullOrEmpty(lc) ? 2 : 3;
        double barW = groupW * 0.72 / seriesCount;
        double inner = (groupW - barW * seriesCount) / 2;
        double yMax = ymax <= 0 ? 1 : ymax * 1.15;
        string[] colors = { "#e74c3c", "#2ecc71", "#3498db" };
        var sb = CreateSvg(w, h);
        AppendAxes(sb, w, h, pad, top, bottom);

        for (int tick = 0; tick <= 4; tick++)
        {
            double value = ymin + (yMax - ymin) * tick / 4.0;
            double y = h - bottom - (value - ymin) / (yMax - ymin) * plotHeight;
            sb.AppendLine($"<line x1=\"{pad:F1}\" y1=\"{y:F1}\" x2=\"{w - 25:F1}\" y2=\"{y:F1}\" stroke=\"#e6e6e6\"/>");
            string tickLabel = yMax < 0.1 ? value.ToString("F3", CI) : value.ToString("F2", CI);
            sb.AppendLine($"<text x=\"{pad - 9:F1}\" y=\"{y + 4:F1}\" text-anchor=\"end\" font-size=\"10\" fill=\"#666\">{tickLabel}</text>");
        }

        for (int i = 0; i < n; i++)
        {
            double gx = pad + i * groupW;
            for (int j = 0; j < seriesCount; j++)
            {
                if (i >= series[j].Count) continue;
                double value = series[j][i].v;
                double y = h - bottom - (value - ymin) / (yMax - ymin) * plotHeight;
                double x = gx + inner + j * barW;
                sb.AppendLine($"<rect x=\"{x + 2:F1}\" y=\"{y:F1}\" width=\"{Math.Max(1, barW - 4):F1}\" height=\"{h - bottom - y:F1}\" rx=\"2\" fill=\"{colors[j]}\"/>");
                if (Math.Abs(value - ymin) > 1e-9)
                {
                    string valueLabel = yMax < 0.1 ? value.ToString("F5", CI) : value.ToString("F2", CI);
                    double labelY = Math.Max(top + 12, y - 5 - (j == 2 && i < series[1].Count && Math.Abs(value - series[1][i].v) < 1e-9 ? 11 : 0));
                    sb.AppendLine($"<text x=\"{x + barW / 2:F1}\" y=\"{labelY:F1}\" font-size=\"9\" text-anchor=\"middle\" fill=\"{colors[j]}\">{valueLabel}</text>");
                }
            }
            sb.AppendLine($"<text x=\"{gx + groupW / 2:F1}\" y=\"{h - bottom + 18:F1}\" font-size=\"10\" text-anchor=\"middle\" fill=\"#444\">{series[0][i].name}</text>");
        }

        sb.AppendLine($"<text x=\"{pad}\" y=\"25\" font-size=\"14\" font-weight=\"bold\">{la} / {lb}{(seriesCount == 3 ? " / " + lc : "")}</text>");
        AppendLegend(sb, pad, h - 12, (la, colors[0]), (lb, colors[1]), (lc, colors[2]));
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // Линейный график с общей осью категорий и корректной шкалой значений.
    private static string LineChart(
        IEnumerable<(string name, double v)> a,
        IEnumerable<(string name, double v)> b,
        IEnumerable<(string name, double v)> c,
        string la, string lb, string lc, double ymin, double ymax)
    {
        var series = new[] { a.ToList(), b.ToList(), c.ToList() };
        int n = series[0].Count;
        int seriesCount = series[2].Count == 0 ? 2 : 3;
        double w = 1250, h = 460, pad = 82, top = 48, bottom = 100;
        double plotHeight = h - top - bottom;
        double plotWidth = w - pad - 35;
        string[] colors = la == "ECR"
            ? new[] { "#e74c3c", "#3498db", "#2ecc71" }
            : new[] { "#2ecc71", "#e74c3c", "#3498db" };
        var sb = CreateSvg(w, h);
        AppendAxes(sb, w, h, pad, top, bottom);

        double y0 = h - bottom - (0 - ymin) / (ymax - ymin) * plotHeight;
        if (y0 >= top && y0 <= h - bottom)
            sb.AppendLine($"<line x1=\"{pad:F1}\" y1=\"{y0:F1}\" x2=\"{w - 35:F1}\" y2=\"{y0:F1}\" stroke=\"#999\" stroke-dasharray=\"5 5\"/>");

        for (int tick = 0; tick <= 4; tick++)
        {
            double value = ymin + (ymax - ymin) * tick / 4.0;
            double y = h - bottom - (value - ymin) / (ymax - ymin) * plotHeight;
            sb.AppendLine($"<line x1=\"{pad:F1}\" y1=\"{y:F1}\" x2=\"{w - 35:F1}\" y2=\"{y:F1}\" stroke=\"#eee\"/>");
            sb.AppendLine($"<text x=\"{pad - 10:F1}\" y=\"{y + 4:F1}\" text-anchor=\"end\" font-size=\"10\" fill=\"#666\">{value:F1}</text>");
        }

        for (int j = 0; j < seriesCount; j++)
        {
            var data = series[j];
            var points = new List<string>();
            for (int i = 0; i < data.Count; i++)
            {
                double x = pad + (n <= 1 ? 0 : i * plotWidth / (n - 1));
                double y = h - bottom - (data[i].v - ymin) / (ymax - ymin) * plotHeight;
                points.Add($"{x:F1},{y:F1}");
            }
            sb.AppendLine($"<polyline points=\"{string.Join(" ", points)}\" fill=\"none\" stroke=\"{colors[j]}\" stroke-width=\"2.5\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>");
            for (int i = 0; i < data.Count; i++)
            {
                double x = pad + (n <= 1 ? 0 : i * plotWidth / (n - 1));
                double y = h - bottom - (data[i].v - ymin) / (ymax - ymin) * plotHeight;
                sb.AppendLine($"<circle cx=\"{x:F1}\" cy=\"{y:F1}\" r=\"4\" fill=\"{colors[j]}\" stroke=\"#fff\" stroke-width=\"1\"/>");
            }
        }

        for (int i = 0; i < n; i++)
        {
            double x = pad + (n <= 1 ? 0 : i * plotWidth / (n - 1));
            sb.AppendLine($"<text x=\"{x:F1}\" y=\"{h - bottom + 19:F1}\" font-size=\"10\" text-anchor=\"middle\" fill=\"#444\">{series[0][i].name}</text>");
        }
        sb.AppendLine($"<text x=\"{pad}\" y=\"25\" font-size=\"14\" font-weight=\"bold\">{la} / {lb}{(seriesCount == 3 ? " / " + lc : "")}</text>");
        var legend = seriesCount == 3
            ? new[] { (la, colors[0]), (lb, colors[1]), (lc, colors[2]) }
            : new[] { (la, colors[0]), (lb, colors[1]) };
        AppendLegend(sb, pad, h - 12, legend);
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // График квантованных уровней по восьми цветовым полосам.
    private static string StairChart(
        IEnumerable<(string name, double v)> a,
        IEnumerable<(string name, double v)> b,
        IEnumerable<(string name, double v)> c,
        string la, string lb, string lc, double ymin, double ymax)
    {
        var series = new[] { a.ToList(), b.ToList(), c.ToList() };
        int n = series[0].Count;
        double w = 1300, h = 480, pad = 85, top = 48, bottom = 100;
        double plotHeight = h - top - bottom;
        double plotWidth = w - pad - 35;
        string[] colors = { "#2ecc71", "#e74c3c", "#3498db" };
        var sb = CreateSvg(w, h);
        AppendAxes(sb, w, h, pad, top, bottom);

        for (int tick = 0; tick <= 4; tick++)
        {
            double value = ymin + (ymax - ymin) * tick / 4.0;
            double y = h - bottom - (value - ymin) / (ymax - ymin) * plotHeight;
            sb.AppendLine($"<line x1=\"{pad:F1}\" y1=\"{y:F1}\" x2=\"{w - 35:F1}\" y2=\"{y:F1}\" stroke=\"#eee\"/>");
            sb.AppendLine($"<text x=\"{pad - 10:F1}\" y=\"{y + 4:F1}\" font-size=\"10\" text-anchor=\"end\" fill=\"#666\">{value:F0}</text>");
        }

        for (int j = 0; j < 3; j++)
        {
            var data = series[j];
            var points = new List<string>();
            for (int i = 0; i < data.Count; i++)
            {
                double x = pad + (i + 0.5) * plotWidth / n;
                double y = h - bottom - (data[i].v - ymin) / (ymax - ymin) * plotHeight;
                points.Add($"{x:F1},{y:F1}");
            }
            sb.AppendLine($"<polyline points=\"{string.Join(" ", points)}\" fill=\"none\" stroke=\"{colors[j]}\" stroke-width=\"2.5\" stroke-linejoin=\"round\"/>");
            for (int i = 0; i < data.Count; i++)
            {
                double x = pad + (i + 0.5) * plotWidth / n;
                double y = h - bottom - (data[i].v - ymin) / (ymax - ymin) * plotHeight;
                sb.AppendLine($"<circle cx=\"{x:F1}\" cy=\"{y:F1}\" r=\"4\" fill=\"{colors[j]}\" stroke=\"#fff\" stroke-width=\"1\"/>");
            }
        }
        for (int i = 0; i < n; i++)
        {
            double x = pad + (i + 0.5) * plotWidth / n;
            sb.AppendLine($"<text x=\"{x:F1}\" y=\"{h - bottom + 19:F1}\" font-size=\"10\" text-anchor=\"middle\" fill=\"#444\">{series[0][i].name}</text>");
        }
        sb.AppendLine($"<text x=\"{pad}\" y=\"25\" font-size=\"14\" font-weight=\"bold\">Квантованные значения: {la} / {lb} / {lc}</text>");
        AppendLegend(sb, pad, h - 12, (la, colors[0]), (lb, colors[1]), (lc, colors[2]));
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // Групповая гистограмма битовых скоростей.
    private static string GroupedBarChart(
        IEnumerable<(string name, double v)> a,
        IEnumerable<(string name, double v)> b,
        IEnumerable<(string name, double v)> c,
        string la, string lb, string lc, double ymin, double ymax)
    {
        var series = new[] { a.ToList(), b.ToList(), c.ToList() };
        int n = series[0].Count;
        double w = 1350, h = 490, pad = 90, top = 48, bottom = 112;
        double plotHeight = h - top - bottom;
        double groupW = (w - pad - 35) / Math.Max(1, n);
        double barW = groupW * 0.23;
        double gap = groupW * 0.035;
        string[] colors = { "#2ecc71", "#e74c3c", "#3498db" };
        var sb = CreateSvg(w, h);
        AppendAxes(sb, w, h, pad, top, bottom);

        for (int tick = 0; tick <= 5; tick++)
        {
            double value = ymin + (ymax - ymin) * tick / 5.0;
            double y = h - bottom - (value - ymin) / (ymax - ymin) * plotHeight;
            sb.AppendLine($"<line x1=\"{pad:F1}\" y1=\"{y:F1}\" x2=\"{w - 25:F1}\" y2=\"{y:F1}\" stroke=\"#eee\"/>");
            sb.AppendLine($"<text x=\"{pad - 10:F1}\" y=\"{y + 4:F1}\" text-anchor=\"end\" font-size=\"10\" fill=\"#666\">{value:F0}</text>");
        }

        for (int i = 0; i < n; i++)
        {
            double gx = pad + i * groupW + (groupW - (3 * barW + 2 * gap)) / 2;
            for (int j = 0; j < 3; j++)
            {
                double value = series[j][i].v;
                double y = h - bottom - (value - ymin) / (ymax - ymin) * plotHeight;
                double x = gx + j * (barW + gap);
                sb.AppendLine($"<rect x=\"{x:F1}\" y=\"{y:F1}\" width=\"{barW:F1}\" height=\"{h - bottom - y:F1}\" rx=\"2\" fill=\"{colors[j]}\"/>");
                sb.AppendLine($"<text x=\"{x + barW / 2:F1}\" y=\"{Math.Max(top + 12, y - 5):F1}\" font-size=\"9\" text-anchor=\"middle\" fill=\"{colors[j]}\">{value:F0}</text>");
            }
            sb.AppendLine($"<text x=\"{pad + i * groupW + groupW / 2:F1}\" y=\"{h - bottom + 20:F1}\" font-size=\"10\" text-anchor=\"middle\" fill=\"#444\">{series[0][i].name}</text>");
        }
        sb.AppendLine($"<text x=\"{pad}\" y=\"25\" font-size=\"14\" font-weight=\"bold\">Битовая скорость: {la} / {lb} / {lc}</text>");
        AppendLegend(sb, pad, h - 12, (la, colors[0]), (lb, colors[1]), (lc, colors[2]));
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string GammaChart()
    {
        double w = 750, h = 460, pad = 70, top = 48, bottom = 70, gamma = 0.45;
        double plotHeight = h - top - bottom;
        double plotWidth = w - pad - 40;
        var sb = CreateSvg(w, h);
        AppendAxes(sb, w, h, pad, top, bottom);
        var corrected = new List<string>();
        var linear = new List<string>();
        for (int i = 0; i <= 100; i++)
        {
            double input = i / 100.0;
            double output = Math.Pow(input, gamma);
            double x = pad + input * plotWidth;
            corrected.Add($"{x:F1},{h - bottom - output * plotHeight:F1}");
            linear.Add($"{x:F1},{h - bottom - input * plotHeight:F1}");
        }
        sb.AppendLine($"<polyline points=\"{string.Join(" ", linear)}\" fill=\"none\" stroke=\"#999\" stroke-width=\"2\" stroke-dasharray=\"6 5\"/>");
        sb.AppendLine($"<polyline points=\"{string.Join(" ", corrected)}\" fill=\"none\" stroke=\"#e74c3c\" stroke-width=\"3\"/>");
        sb.AppendLine($"<text x=\"{pad}\" y=\"25\" font-size=\"14\" font-weight=\"bold\">Гамма-коррекция: Out = In^γ, γ = 0.45</text>");
        sb.AppendLine($"<text x=\"{pad}\" y=\"{h - bottom + 20}\" font-size=\"11\">In = 0</text>");
        sb.AppendLine($"<text x=\"{w - 30}\" y=\"{h - bottom + 20}\" font-size=\"11\" text-anchor=\"end\">In = 1</text>");
        AppendLegend(sb, pad, h - 12, ("Гамма-коррекция", "#e74c3c"), ("Линейная зависимость", "#999"));
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string SamplingChart()
    {
        double w = 950, h = 460, pad = 75, top = 48, bottom = 78;
        double plotHeight = h - top - bottom;
        double plotWidth = w - pad - 40;
        var sb = CreateSvg(w, h);
        AppendAxes(sb, w, h, pad, top, bottom);
        var points = new List<string>();
        for (int i = 0; i <= 300; i++)
        {
            double t = i / 100.0;
            double value = 0.5 + 0.4 * Math.Sin(2 * Math.PI * t);
            double x = pad + t / 3.0 * plotWidth;
            double y = h - bottom - value * plotHeight;
            points.Add($"{x:F1},{y:F1}");
        }
        sb.AppendLine($"<polyline points=\"{string.Join(" ", points)}\" fill=\"none\" stroke=\"#7f8c8d\" stroke-width=\"2\"/>");
        for (int i = 0; i <= 8; i++)
        {
            double t = i / 8.0 * 3.0;
            double value = 0.5 + 0.4 * Math.Sin(2 * Math.PI * t);
            double x = pad + t / 3.0 * plotWidth;
            double y = h - bottom - value * plotHeight;
            sb.AppendLine($"<line x1=\"{x:F1}\" y1=\"{y:F1}\" x2=\"{x:F1}\" y2=\"{h - bottom:F1}\" stroke=\"#e74c3c\" stroke-width=\"1\" stroke-dasharray=\"4 4\"/>");
            sb.AppendLine($"<circle cx=\"{x:F1}\" cy=\"{y:F1}\" r=\"4.5\" fill=\"#e74c3c\" stroke=\"#fff\" stroke-width=\"1\"/>");
        }
        sb.AppendLine($"<text x=\"{pad}\" y=\"25\" font-size=\"14\" font-weight=\"bold\">Дискретизация: отсчёты на аналоговой огибающей</text>");
        AppendLegend(sb, pad, h - 12, ("Аналоговый сигнал", "#7f8c8d"), ("Дискретные отсчёты", "#e74c3c"));
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
}
