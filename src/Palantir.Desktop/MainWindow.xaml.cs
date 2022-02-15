using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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

namespace Palantir.Desktop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ObservableCollection<string> FileList { get; set; } = new ObservableCollection<string>();

		public DataTable Results { get; set; } = new DataTable();

		public string FilterUsername { get; set; }
		public string FilterIP { get; set; }
		public string FilterUri { get; set; }

		public class ResultRow
		{
			public Dictionary<string, string> RowData { get; set; }
		}

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			DataGridResults.ItemsSource = Results.DefaultView;
			BtnExport.Visibility = Visibility.Hidden;
			ResultsCount.Visibility = Visibility.Hidden;
			StackPanelColumns.Visibility = Visibility.Hidden;
		}

		private void BtnAddFile_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() == true)
			{
				FileList.Add(dialog.FileName);
			}
		}

		private void BtnRemoveFile_Click(object sender, RoutedEventArgs e)
		{
			if (ListViewFiles.SelectedIndex > -1)
			{
				FileList.RemoveAt(ListViewFiles.SelectedIndex);
			}
		}

		private void BtnSearch_Click(object sender, RoutedEventArgs e)
		{
			BtnSearch.IsEnabled = false;

			var _ = Search();
		}

		private async Task Search()
		{
			await Dispatcher.BeginInvoke(() =>
			  {
				  StackPanelInitial.Visibility = Visibility.Hidden;
				  StackPanelLoading.Visibility = Visibility.Visible;
				  DataGridResults.Visibility = Visibility.Hidden;
				  BtnExport.Visibility = Visibility.Hidden;
				  ResultsCount.Visibility = Visibility.Hidden;
				  StackPanelColumns.Visibility = Visibility.Hidden;

				  Results.Clear();
				  Results.Dispose();
				  Results = new DataTable();
				  Results.BeginLoadData();
				  DataGridResults.ItemsSource = null;
			  });

			await Task.Run(() =>
			{

				foreach (var file in FileList)
				{
					using (var textStream = System.IO.File.OpenRead(file))
					using (var textReader = new System.IO.StreamReader(textStream))
					{
						string line = null;
						string[] columns = null;
						DataRow row = null;
						while ((line = textReader.ReadLine()) != null)
						{
							if (line.StartsWith("#Fields"))
							{
								columns = line.Substring(9).Split(' ');
								foreach (var column in columns)
								{
									if (!Results.Columns.Contains(column))
									{
										Results.Columns.Add(column);
									}
								}
							}
							else if (!line.StartsWith("#"))
							{
								if (columns == null) throw new InvalidOperationException($"File [{file}] contains no #Fields metadata. Cannot parse!");
								var split = line.Split(' ');
								row = row ?? Results.NewRow();

								row.BeginEdit();
								for (var i = 0; i < split.Length; i++)
								{
									row[columns[i]] = split[i];
								}
								bool doesMatch = false;
								bool anyFilterPresent = false;

								// Let's check if any filter's are applied..
								if (!string.IsNullOrEmpty(FilterIP) && !doesMatch)
								{
									//if (!row.ContainsKey("c-ip")) throw new ArgumentNullException($"File [{file}] does not contain an 'c-ip'.. cannot parse!");
									var term = (row["c-ip"] ?? "").ToString();
									if (FilterIP.EndsWith("*"))
									{
										doesMatch = term.StartsWith(FilterIP.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									else if (FilterIP.StartsWith("*"))
									{
										doesMatch = term.EndsWith(FilterIP.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									else
									{
										doesMatch = term.Contains(FilterIP.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									anyFilterPresent = true;
								}
								if (!string.IsNullOrEmpty(FilterUsername) && !doesMatch)
								{
									//if (!parsed.ContainsKey("cs-username")) throw new ArgumentNullException($"File [{file}] does not contain an 'cs-username'.. cannot parse!");
									var term = (row["cs-username"] ?? "").ToString();
									if (FilterUsername.EndsWith("*"))
									{
										doesMatch = term.StartsWith(FilterUsername.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									else if (FilterUsername.StartsWith("*"))
									{
										doesMatch = term.EndsWith(FilterUsername.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									else
									{
										doesMatch = term.Contains(FilterUsername.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									anyFilterPresent = true;
								}
								if (!string.IsNullOrEmpty(FilterUri) && !doesMatch)
								{
									//if (!parsed.ContainsKey("cs-username")) throw new ArgumentNullException($"File [{file}] does not contain an 'cs-username'.. cannot parse!");
									var term = (row["cs-uri-stem"] ?? "").ToString();
									if (FilterUri.EndsWith("*"))
									{
										doesMatch = term.StartsWith(FilterUri.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									else if (FilterUri.StartsWith("*"))
									{
										doesMatch = term.EndsWith(FilterUri.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									else
									{
										doesMatch = term.Contains(FilterUri.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
									}
									anyFilterPresent = true;
								}
								row.EndEdit();
								row.AcceptChanges();

								if (doesMatch || !anyFilterPresent)
								{
									Results.Rows.Add(row);
									row = null;
								}
								else
								{
									row.CancelEdit();
								}
							}
						}
					}
				}
			});
			await Dispatcher.BeginInvoke(() =>
			{

				BtnSearch.IsEnabled = true;
				Results.EndLoadData();
				Results.AcceptChanges();
				DataGridResults.ItemsSource = Results.DefaultView;
				ListViewColumns.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();
				LabelColumnCount.GetBindingExpression(Label.ContentProperty).UpdateTarget();
				StackPanelInitial.Visibility = Visibility.Hidden;
				StackPanelLoading.Visibility = Visibility.Hidden;
				DataGridResults.Visibility = Visibility.Visible;
				BtnExport.Visibility = Visibility.Visible;
				ResultsCount.Visibility = Visibility.Visible;
				StackPanelColumns.Visibility = Visibility.Visible;
			});
		}

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
			using (var workbook = new XLWorkbook())
            {
				Results.TableName = "W3CData";
				workbook.AddWorksheet(Results);
				var dialog = new SaveFileDialog();
				if (dialog.ShowDialog() == true)
                {
					workbook.SaveAs(dialog.FileName);
					MessageBox.Show($"Exported {Results.Rows.Count} rows.");
                }
            }
        }
    }
}
