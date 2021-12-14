using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;
using Excel = Microsoft.Office.Interop.Excel;
using static System.Math;

namespace leastsquares
{
    public partial class Form1 : Form
    {
        PointPairList pointPairs = new PointPairList();
        PointPairList linear = new PointPairList();
        PointPairList quadratic = new PointPairList();
        double sumX, sumY, sumX2, sumXY, sumX2Y, sumX3, sumX4;
        int count;
        public Form1()
        {
            InitializeComponent();
            graph.GraphPane.Title.Text = "";
            graph.GraphPane.XAxis.Title.Text = "";
            graph.GraphPane.YAxis.Title.Text = "";
        }
        #region data
        private void getData()
        {
            pointPairs.Clear();
            linear.Clear();
            quadratic.Clear();
            for (int row = 0; row < dataGridView.Rows.Count - 1; ++row)
            {
                PointPair pair = new PointPair(Convert.ToDouble(dataGridView.Rows[row].Cells["columnX"].Value), 
                                               Convert.ToDouble(dataGridView.Rows[row].Cells["columnY"].Value));
                pointPairs.Add(pair);
            }
            pointPairs.Sort();
            graph.GraphPane.CurveList.Clear();
            graph.AxisChange();
            graph.Refresh();
        }
        private async void googleSheetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridView.Rows.Clear();
                await Task.Run(() =>
                {
                    GoogleCredential credential;
                    string[] Scopes = { SheetsService.Scope.Spreadsheets };
                    string credFile = "credentials.json";
                    string spreadsheetId = "1rq_p7SH-JEcnRkFx6gfxTQGppG2x_ydCN5hKygSuILQ";
                    string range = "Лист1!A:B";

                    using (var fs = new FileStream(credFile, FileMode.Open, FileAccess.Read))
                    {
                        credential = GoogleCredential.FromStream(fs).CreateScoped(Scopes);
                    }
                    SheetsService service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ""
                    });
                    var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                    var values = request.Execute().Values;
                    if (values != null && values.Count > 0)
                    {
                        foreach (var row in values)
                        {
                            dataGridView.Rows.Add(row[0].ToString(), row[1].ToString());
                        }
                        countToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Файл пуст!");
                    }
                });
            }
            catch
            {
                MessageBox.Show("Ошибка!");
            }

        }
        private void excelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridView.Rows.Clear();
                using (var ofd = new OpenFileDialog())
                {
                    ofd.DefaultExt = "*.xls;*.xlsx";
                    ofd.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
                    ofd.Title = "Выберите файл для импорта";
                    if (ofd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName))
                    {
                        Excel.Application excelApp = new Excel.Application();
                        Excel.Workbook workbook = excelApp.Workbooks.Open(ofd.FileName);
                        Excel.Worksheet sheet = (Excel.Worksheet)workbook.Worksheets.get_Item(1);
                        Excel.Range range = sheet.UsedRange;

                        var lastCell = sheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell);
                        int lastColumn = lastCell.Column;
                        int lastRow = lastCell.Row;
                        for (int index = 0; index < lastRow; ++index)
                        {
                            dataGridView.Rows.Add(sheet.Cells[index + 1, 1].Text.ToString(), sheet.Cells[index + 1, 2].Text.ToString());
                        }
                        workbook.Close(false, Type.Missing, Type.Missing);
                        excelApp.Quit();
                        excelApp.Quit();
                        GC.Collect();

                        countToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Файл не выбран!");
                    }
                }
            }
            catch
            {
                MessageBox.Show("Ошибка!");
            }
        }
        private void randomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView.Rows.Clear();
            Random random = new Random();
            int length = (int)random.Next(10, 40);
            for (int index = 0; index < length; ++index)
            {
                dataGridView.Rows.Add(random.Next(0, 100), random.Next(0, 100));
            }
            countToolStripMenuItem.Enabled = true;
        }
        #endregion
        #region buttons
        private async void countToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                getData();
                if (pointPairs.Count < 3)
                {
                    throw new Exception("Недостаточно точек!");
                }
                foreach (var pair in pointPairs)
                {
                    PointPairList point = new PointPairList();
                    point.Add(pair);
                    graph.GraphPane.AddCurve("", point, Color.Purple, SymbolType.Circle);
                }
                graph.AxisChange();
                graph.Refresh();
                count = pointPairs.Count;
                countSum();
                Task<string> linearFunc = makeLinearFunction();
                Task<string> quadFunc = makeQuadraticFunction();
                drawGraph(await linearFunc, linear, Color.DarkRed, SymbolType.None);
                drawGraph(await quadFunc, quadratic, Color.DarkGreen, SymbolType.None);
                //drawGraph(await quadFunc, pointPairs, Color.Transparent, SymbolType.None);
                countToolStripMenuItem.Enabled = false;
            }
            catch (FormatException)
            {
                MessageBox.Show("Некорректные данные!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            graph.GraphPane.CurveList.Clear();
            graph.Refresh();
            dataGridView.Rows.Clear();
            pointPairs.Clear();
            linear.Clear();
            quadratic.Clear();
            sumX = sumY = sumX2 = sumXY = sumX2Y = sumX3 = sumX4 = count = 0;
            countToolStripMenuItem.Enabled = true;
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        #endregion
        #region visuals
        private void drawGraph(string legend, PointPairList list, Color color, SymbolType type)
        {
            graph.GraphPane.AddCurve(legend, list, color, type);
            graph.AxisChange();
            graph.Refresh();
        }
        private Task<string> makeLinearFunction()
        {
            return Task.Run(() =>
            {
                double[] linArgs = linearModel(count);
                return String.Format("y = {0}x + {1}", Round(linArgs[0], 4), Round(linArgs[1], 4));
            });
        }
        private Task<string> makeQuadraticFunction()
        {
            return Task.Run(() =>
            {
                double[] quadArgs = quadraticModel(count);
                return String.Format("y = {0}x^2 + {1}x + {2}", Round(quadArgs[0], 4), Round(quadArgs[1], 4), Round(quadArgs[2], 4));
            });
        }
        #endregion
        #region math
        private void countSum()
        {
            foreach (var p in pointPairs)
            {
                sumX += p.X;
                sumY += p.Y;
                sumX2 += Pow(p.X, 2);
                sumXY += p.X * p.Y;
                sumX2Y += Pow(p.X, 2) * p.Y;
                sumX3 += Math.Pow(p.X, 3);
                sumX4 += Math.Pow(p.X, 4);
            }
        }
        private double[] linearModel(int count)
        {
            double d = Pow(sumX, 2) - count * sumX2;
            double b = (sumX * sumXY - sumX2 * sumY) / d;
            double k = (sumX * sumY - count * sumXY) / d;

            foreach (var p in pointPairs)
            {
                linear.Add(new PointPair(p.X, k * p.X + b));
            }

            return new double[] { k, b };
        }
        private double[] quadraticModel(int count)
        {
            double d = Round(count * sumX2 * sumX4 + 2 * (sumX2 * sumX * sumX3) - Pow(sumX2, 3) - Pow(sumX, 2) * sumX4 - count * Pow(sumX3, 2), 4); 
            double a = Round((sumX2Y * sumX2 * count + 2 * (sumX3 * sumX * sumX2) - Pow(sumX2, 3) - Pow(sumX, 2) * sumX2Y - Pow(sumX3, 2) * count) / d, 4);
            double b = Round((count * sumXY * sumX4 + sumX2 * sumX * sumX2Y + sumY * sumX3 * sumX2 - sumXY * Pow(sumX2, 2) - sumY * sumX * sumX4 - count * sumX3 * sumX2Y) / d, 4);
            double c = Round((sumY * sumX2 * sumX4 + sumX2 * sumXY * sumX3 + sumX * sumX3 * sumX2Y - Pow(sumX2, 2) * sumX2Y - sumX * sumXY * sumX4 - sumY * Pow(sumX3, 2)) / d, 4);

            foreach (var p in pointPairs)
            {
                quadratic.Add(new PointPair(p.X, a * Pow(p.X, 2) + b * p.X + c));
            }

            return new double[] { a, b, c };
        }
        #endregion
    }
}
