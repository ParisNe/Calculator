using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Calculator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CmbType.SelectedIndex = 0;
        }

        // ==================== показ/скрытие полей ====================
        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PnlN == null || PnlM == null) return;
            string tag = ((ComboBoxItem)CmbType.SelectedItem).Tag.ToString();
            PnlN.Visibility = (tag == "4" || tag == "5" || tag == "6")
                ? Visibility.Visible : Visibility.Collapsed;
            PnlM.Visibility = (tag == "3" || tag == "6")
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RbMode_Checked(object sender, RoutedEventArgs e)
        {
            if (PnlMu == null || PnlTime == null) return;
            bool byTime = RbTime.IsChecked == true;
            PnlTime.Visibility = byTime ? Visibility.Visible : Visibility.Collapsed;
            PnlMu.Visibility = byTime ? Visibility.Collapsed : Visibility.Visible;
        }

        // ==================== кнопка РАССЧИТАТЬ ====================
        private void BtnCalc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tag = ((ComboBoxItem)CmbType.SelectedItem).Tag.ToString();
                bool byTime = RbTime.IsChecked == true;

                double lambdaVal = ParsePos(TxtLambda.Text, "λ");
                string lambdaUnit = ((ComboBoxItem)CmbLambdaUnit.SelectedItem).Tag.ToString();
                double lambda = ToPerHour(lambdaVal, lambdaUnit);
                string lambdaLabel = "λ = " + F(lambdaVal) + " " + PerHourUnitName(lambdaUnit)
                                     + " (= " + F(lambda) + " заявок/ч)";

                double mu;
                string muLabel;
                if (byTime)
                {
                    double tVal = ParsePos(TxtTime.Text, "t_об");
                    string tUnit = ((ComboBoxItem)CmbTimeUnit.SelectedItem).Tag.ToString();
                    double tHours = ToHours(tVal, tUnit);
                    mu = 1.0 / tHours;
                    muLabel = "μ (из t_об=" + F(tVal) + " " + TimeUnitName(tUnit)
                              + " = " + F(tHours) + " ч) = " + F(mu) + " заявок/ч";
                }
                else
                {
                    double muVal = ParsePos(TxtMu.Text, "μ");
                    string muUnit = ((ComboBoxItem)CmbMuUnit.SelectedItem).Tag.ToString();
                    mu = ToPerHour(muVal, muUnit);
                    muLabel = "μ = " + F(muVal) + " " + PerHourUnitName(muUnit)
                              + " (= " + F(mu) + " заявок/ч)";
                }

                int n = 1, m = 0;
                if (tag == "4" || tag == "5" || tag == "6") n = ParseInt(TxtN.Text, "n", 1);
                if (tag == "3" || tag == "6") m = ParseInt(TxtM.Text, "m", 1);

                double rho = lambda / mu;

                var sb = new StringBuilder();
                sb.AppendLine(lambdaLabel);
                sb.AppendLine(muLabel);
                sb.AppendLine("t_об = " + F(60.0 / mu) + " мин = " + F(1.0 / mu) + " ч");
                sb.AppendLine("n = " + n + (m > 0 ? "   m = " + m : ""));
                sb.AppendLine("ρ = " + F(rho) + (n > 1 ? "   ρ/n = " + F(rho / n) : ""));
                sb.AppendLine();

                if (tag == "1") CalcSingleReject(sb, lambda, mu);
                else if (tag == "2") CalcSingleUnlimited(sb, lambda, mu);
                else if (tag == "3") CalcSingleLimited(sb, lambda, mu, m);
                else if (tag == "4") CalcMultiReject(sb, lambda, mu, n);
                else if (tag == "5") CalcMultiUnlimited(sb, lambda, mu, n);
                else if (tag == "6") CalcMultiLimited(sb, lambda, mu, n, m);

                TxtOut.Text = sb.ToString();
                TxtHeader.Text = GetTypeShortName(tag);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ==================== 1. M/M/1/0 ====================
        private void CalcSingleReject(StringBuilder sb, double lambda, double mu)
        {
            double rho = lambda / mu;
            double p0 = 1.0 / (1.0 + rho);
            double pRej = rho / (1.0 + rho);
            double pSer = 1.0 - pRej;
            double L = rho / (1.0 + rho);
            double W = L / lambda;
            double Wob = 1.0 / mu;
            sb.AppendLine("--- Одноканальная СМО с отказами ---");
            sb.AppendLine("P0      = " + F(p0));
            sb.AppendLine("Pотк    = " + F(pRej));
            sb.AppendLine("Pобс    = " + F(pSer));
            sb.AppendLine("A       = " + F(lambda * pSer) + " заявок/ч");
            sb.AppendLine("L       = " + F(L));
            sb.AppendLine("T       = " + T(W));
            sb.AppendLine("Tоб     = " + T(Wob));
            sb.AppendLine("Tожид   = 0 (отказы)");
        }

        // ==================== 2. M/M/1/∞ ====================
        private void CalcSingleUnlimited(StringBuilder sb, double lambda, double mu)
        {
            double rho = lambda / mu;
            sb.AppendLine("--- Одноканальная СМО с неогр. очередью ---");
            if (rho >= 1) { sb.AppendLine("НЕСТАБИЛЬНА: ρ ≥ 1, очередь растёт бесконечно"); return; }
            double Lq = rho * rho / (1.0 - rho);
            double L = rho / (1.0 - rho);
            double Wq = Lq / lambda;
            double W = 1.0 / (mu - lambda);
            sb.AppendLine("P0      = " + F(1.0 - rho));
            sb.AppendLine("Pожид   = " + F(rho));
            sb.AppendLine("Lоч     = " + F(Lq));
            sb.AppendLine("L       = " + F(L));
            sb.AppendLine("Tоч     = " + T(Wq));
            sb.AppendLine("T       = " + T(W));
        }

        // ==================== 3. M/M/1/m ====================
        private void CalcSingleLimited(StringBuilder sb, double lambda, double mu, int m)
        {
            double rho = lambda / mu;
            double p0 = Math.Abs(rho - 1) < 1e-9
                ? 1.0 / (m + 2)
                : (1 - rho) / (1 - Math.Pow(rho, m + 2));
            double pRej = Math.Pow(rho, m + 1) * p0;
            double pSer = 1 - pRej;
            double lEff = lambda * pSer;
            double Lq = Math.Abs(rho - 1) < 1e-9
                ? m * (m + 1) / (2.0 * (m + 2))
                : rho * rho * (1 - (m + 1) * Math.Pow(rho, m) + m * Math.Pow(rho, m + 1))
                  / ((1 - rho) * (1 - Math.Pow(rho, m + 2)));
            double L = Lq + pSer;
            double Wq = lEff > 0 ? Lq / lEff : 0;
            double W = Wq + 1.0 / mu;
            sb.AppendLine("--- Одноканальная СМО с огр. очередью ---");
            sb.AppendLine("P0      = " + F(p0));
            sb.AppendLine("Pотк    = " + F(pRej));
            sb.AppendLine("Pобс    = " + F(pSer));
            sb.AppendLine("A       = " + F(lEff) + " заявок/ч");
            sb.AppendLine("Lоч     = " + F(Lq));
            sb.AppendLine("L       = " + F(L));
            sb.AppendLine("Tоч     = " + T(Wq));
            sb.AppendLine("T       = " + T(W));
        }

        // ==================== 4. M/M/n/0 ====================
        private void CalcMultiReject(StringBuilder sb, double lambda, double mu, int n)
        {
            double rho = lambda / mu;
            double sum = 0;
            for (int k = 0; k <= n; k++) sum += Math.Pow(rho, k) / Fact(k);
            double p0 = 1.0 / sum;
            double pRej = Math.Pow(rho, n) / Fact(n) * p0;
            double pSer = 1 - pRej;
            sb.AppendLine("--- Многоканальная СМО с отказами (Эрланг) ---");
            sb.AppendLine("P0      = " + F(p0));
            sb.AppendLine("Pотк    = " + F(pRej));
            sb.AppendLine("Pобс    = " + F(pSer));
            sb.AppendLine("A       = " + F(lambda * pSer) + " заявок/ч");
            sb.AppendLine("L       = " + F(rho * pSer));
            sb.AppendLine("T       = " + T(1.0 / mu));
        }

        // ==================== 5. M/M/n/∞ ====================
        private void CalcMultiUnlimited(StringBuilder sb, double lambda, double mu, int n)
        {
            double rho = lambda / mu, rn = rho / n;
            sb.AppendLine("--- Многоканальная СМО с неогр. очередью ---");
            if (rn >= 1) { sb.AppendLine("НЕСТАБИЛЬНА: ρ/n ≥ 1, очередь растёт бесконечно"); return; }
            double sum = 0;
            for (int k = 0; k < n; k++) sum += Math.Pow(rho, k) / Fact(k);
            sum += Math.Pow(rho, n) / Fact(n) * (1.0 / (1.0 - rn));
            double p0 = 1.0 / sum;
            double pWait = Math.Pow(rho, n) / Fact(n) * (1.0 / (1.0 - rn)) * p0;
            double Lq = pWait * rn / (1.0 - rn);
            double Wq = Lq / lambda;
            double W = Wq + 1.0 / mu;
            sb.AppendLine("P0      = " + F(p0));
            sb.AppendLine("Pожид   = " + F(pWait));
            sb.AppendLine("Lоч     = " + F(Lq));
            sb.AppendLine("L       = " + F(Lq + rho));
            sb.AppendLine("Tоч     = " + T(Wq));
            sb.AppendLine("T       = " + T(W));
        }

        // ==================== 6. M/M/n/m ====================
        private void CalcMultiLimited(StringBuilder sb, double lambda, double mu, int n, int m)
        {
            double rho = lambda / mu, q = rho / n;
            double sum = 0;
            for (int k = 0; k <= n; k++) sum += Math.Pow(rho, k) / Fact(k);
            double sumQ = 0;
            for (int k = 1; k <= m; k++) sumQ += Math.Pow(q, k);
            sum += Math.Pow(rho, n) / Fact(n) * sumQ;
            double p0 = 1.0 / sum;
            double pRej = Math.Pow(rho, n + m) / (Fact(n) * Math.Pow(n, m)) * p0;
            double pSer = 1 - pRej;
            double lEff = lambda * pSer;
            double pWait = 0;
            for (int k = 1; k <= m; k++)
                pWait += Math.Pow(rho, n) / Fact(n) * Math.Pow(q, k) * p0;
            double Lq;
            if (Math.Abs(q - 1) < 1e-9)
                Lq = Math.Pow(rho, n) / Fact(n) * p0 * m * (m + 1) / 2.0;
            else
            {
                double num = Math.Pow(q, m + 1) - (m + 1) * q + m;
                double den = Math.Pow(1 - q, 2);
                Lq = Math.Pow(rho, n) / Fact(n) * p0 * q * num / den;
            }
            double Wq = lEff > 0 ? Lq / lEff : 0;
            double W = Wq + 1.0 / mu;
            sb.AppendLine("--- Многоканальная СМО с огр. очередью ---");
            sb.AppendLine("P0      = " + F(p0));
            sb.AppendLine("Pотк    = " + F(pRej));
            sb.AppendLine("Pобс    = " + F(pSer));
            sb.AppendLine("Pожид   = " + F(pWait));
            sb.AppendLine("A       = " + F(lEff) + " заявок/ч");
            sb.AppendLine("Lоч     = " + F(Lq));
            sb.AppendLine("L       = " + F(Lq + rho * pSer));
            sb.AppendLine("Tоч     = " + T(Wq));
            sb.AppendLine("T       = " + T(W));
        }

        // ==================== ЭКСПОРТ В EXCEL (Interop) ====================
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtOut.Text) || TxtHeader.Text == "расчёт не выполнен")
            { MessageBox.Show("Сначала нажмите «Рассчитать»."); return; }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx|Excel (*.xls)|*.xls",
                FileName = "SMO_results.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            Microsoft.Office.Interop.Excel.Application xlApp = null;
            Microsoft.Office.Interop.Excel.Workbook xlWb = null;
            Microsoft.Office.Interop.Excel.Worksheet xlWs = null;

            try
            {
                xlApp = new Microsoft.Office.Interop.Excel.Application();
                xlApp.Visible = false;
                xlApp.DisplayAlerts = false;

                xlWb = xlApp.Workbooks.Add();
                xlWs = (Microsoft.Office.Interop.Excel.Worksheet)xlWb.Worksheets[1];
                xlWs.Name = "СМО";

                xlWs.Cells[1, 1] = "Показатель";
                xlWs.Cells[1, 2] = "Значение";
                var hdr = xlWs.Range["A1:B1"];
                hdr.Font.Bold = true;
                hdr.Interior.Color = 0xC0C0C0;

                int row = 2;
                string[] lines = TxtOut.Text
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (line.StartsWith("---")) continue;

                    string key = "", val = "";
                    int eq = line.IndexOf('=');
                    if (eq >= 0)
                    {
                        key = line.Substring(0, eq).Trim();
                        val = line.Substring(eq + 1).Trim();
                    }
                    else key = line.Trim();

                    xlWs.Cells[row, 1] = key;
                    xlWs.Cells[row, 2] = val;
                    row++;
                }

                xlWs.Columns["A:B"].AutoFit();
                xlWs.Range["A2:B" + (row - 1)].Borders.LineStyle =
                    Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                if (dlg.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                    xlWb.SaveAs(dlg.FileName,
                        Microsoft.Office.Interop.Excel.XlFileFormat.xlExcel8);
                else
                    xlWb.SaveAs(dlg.FileName,
                        Microsoft.Office.Interop.Excel.XlFileFormat.xlOpenXMLWorkbook);

                MessageBox.Show("Сохранено:\n" + dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
            finally
            {
                if (xlWb != null)
                {
                    xlWb.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWb);
                }
                if (xlApp != null)
                {
                    xlApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
                }
                xlWs = null;
                xlWb = null;
                xlApp = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // ==================== вспомогательные ====================
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtOut.Text = "";
            TxtHeader.Text = "расчёт не выполнен";
        }

        private static string GetTypeShortName(string tag)
        {
            switch (tag)
            {
                case "1": return "M/M/1/0 — одноканальная с отказами";
                case "2": return "M/M/1/∞ — одноканальная с неогр. очередью";
                case "3": return "M/M/1/m — одноканальная с огр. очередью";
                case "4": return "M/M/n/0 — многоканальная с отказами";
                case "5": return "M/M/n/∞ — многоканальная с неогр. очередью";
                case "6": return "M/M/n/m — многоканальная с огр. очередью";
                default: return "";
            }
        }

        private static double ToPerHour(double value, string unit)
        {
            if (unit == "min") return value * 60.0;
            if (unit == "day") return value / 24.0;
            return value;
        }

        private static double ToHours(double value, string unit)
        {
            if (unit == "min") return value / 60.0;
            if (unit == "day") return value * 24.0;
            return value;
        }

        private static string PerHourUnitName(string unit)
        {
            if (unit == "min") return "заявок/мин";
            if (unit == "day") return "заявок/сут";
            return "заявок/ч";
        }

        private static string TimeUnitName(string unit)
        {
            if (unit == "min") return "мин";
            if (unit == "day") return "сут";
            return "ч";
        }

        private static double Fact(int n)
        {
            double f = 1;
            for (int i = 2; i <= n; i++) f *= i;
            return f;
        }

        private static string F(double v)
        {
            return v.ToString("F4", CultureInfo.InvariantCulture);
        }

        private static string T(double hours)
        {
            return F(hours) + " ч = "
                 + F(hours * 60) + " мин = "
                 + F(hours / 24.0) + " сут";
        }

        private static double ParsePos(string s, string name)
        {
            double v;
            if (!double.TryParse(s.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                throw new FormatException("Некорректное " + name + ": «" + s + "»");
            if (v <= 0) throw new ArgumentException(name + " должно быть > 0");
            return v;
        }

        private static int ParseInt(string s, string name, int min)
        {
            int v;
            if (!int.TryParse(s, out v))
                throw new FormatException("Некорректное " + name + ": «" + s + "»");
            if (v < min) throw new ArgumentException(name + " должно быть ≥ " + min);
            return v;
        }
    }

}
