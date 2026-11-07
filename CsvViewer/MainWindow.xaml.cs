using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace CsvViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _loaded;

        public MainWindow()
        {
            InitializeComponent();
            _loaded = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            int rows = DataGrid.Items.Count;
            MessageBox.Show($"{rows}行を取り込みました。", "読み込み成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loaded)
            {
                MessageBoxResult r = MessageBox.Show("読み込んだデータを破棄して閉じますか？", "確認",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            Close();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "CSVファイル|*.csv|テキストファイル|*.txt" };
            if (dlg.ShowDialog() != true)
            {
                return;
            }

            List<string[]> raw;
            List<CoordinateData> data;
            try
            {
                raw = TryReadCsv(dlg.FileName);
                data = TryConvertAll(raw);
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message, "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (MalformedLineException ex)
            {
                MessageBox.Show($"{ex.LineNumber}行目：CSVの形式が不正です。", "読み込みエラー",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch(OverflowException ex)
            {
                MessageBox.Show("絶対値が大きすぎる数値が含まれています。", "読み込みエラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DataGrid.ItemsSource = data;
            StatusText.Text = $"ファイル名:{System.IO.Path.GetFileName(dlg.FileName)}\n" +
                  $"ファイル全体の行数:{raw.Count}\n" +
                  $"1行目の列数:{raw[0].Length}";

            _loaded = true;
            OkButton.IsEnabled = _loaded;
        }

        private List<string[]> TryReadCsv(string path)
        {
            var rows = new List<string[]>();
            using (var parser = new TextFieldParser(path, Encoding.GetEncoding(932)))
            {
                //parser.ReadLine();                         //　ヘッダーをスキップさせる
                parser.TextFieldType = FieldType.Delimited;  // CSVファイル
                parser.SetDelimiters(",");                   // カンマ区切りであることを明示
                parser.TrimWhiteSpace = true;                // フィールド前後の空白を取り除く
                parser.HasFieldsEnclosedInQuotes = true;     // ダブルクオートを1まとめにする

                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    if (fields != null)
                    {
                        rows.Add(fields);
                    }
                }
            }
            return rows;
        }

        private List<CoordinateData> TryConvertAll(List<string[]> rows)
        {
            if (rows.Count == 0)
            {
                throw new FormatException("データがありません。");
            }

            var list = new List<CoordinateData>();
            for (int r = 0; r < rows.Count; r++)
            {
                string[] fields = rows[r];
                ValidateRow(fields);

                list.Add(new CoordinateData
                {
                    C1 = double.Parse(fields[0], CultureInfo.InvariantCulture),
                    C2 = double.Parse(fields[1], CultureInfo.InvariantCulture),
                    C3 = double.Parse(fields[2], CultureInfo.InvariantCulture),
                    C4 = double.Parse(fields[3], CultureInfo.InvariantCulture),
                    C5 = double.Parse(fields[4], CultureInfo.InvariantCulture),
                    C6 = double.Parse(fields[5], CultureInfo.InvariantCulture),
                });
            }
            return list;
        }

        private void ValidateRow(string[] fields)
        {
            if (fields.Length < 6)
            {
                throw new FormatException($"列数が6未満の行があります。");
            }
            for (int c = 0; c < 6; c++)
            {
                if (!IsNumber(fields[c]))
                {
                    throw new FormatException($"半角数字以外のデータがあります。");
                }
            }
        }

        private bool IsNumber(string value)
        {
            return Regex.IsMatch(value, @"^-?[0-9]+(\.[0-9]+)?\z");
        }
    }
}
