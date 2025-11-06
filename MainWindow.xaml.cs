using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DynamicExpresso;
using VariableCalculator;

namespace WpfCoreCalculator
{
	public partial class MainWindow : Window
	{
		readonly string _settingsFile = Path.ChangeExtension(Environment.ProcessPath, "json");
		readonly JsonSerializerOptions _serializerOptions = new()
		{
			WriteIndented = true,
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
		};
		readonly List<TextBox> _variables = [];
		readonly List<TextBox> _expressions = [];
		readonly List<TextBox> _results = [];
		readonly List<TextBox> _comments = [];
		readonly List<Button> _addButtons = [];
		readonly List<Button> _delButtons = [];

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Closed(object sender, System.EventArgs e)
		{
			var settings = new Settings
			{
				Top = Top,
				Left = Left,
				Height = Height,
				Width = Width,
				Rows = []
			};

			int lastNonEmptyRow = -1;

			for (var i = 0; i < _expressions.Count; i++)
			{
				string expression = _expressions[i].Text
					.Trim()
					.Replace(',', '.');
				var item = new Settings.Row { Variable = _variables[i].Text.Trim(), Expression = expression, Comment = _comments[i].Text.Trim() };

				if (!string.IsNullOrWhiteSpace(item.Variable) || !string.IsNullOrWhiteSpace(item.Expression) || !string.IsNullOrWhiteSpace(item.Comment))
					lastNonEmptyRow = i;

				settings.Rows.Add(item);

			}

			if (lastNonEmptyRow != -1 && settings.Rows.Count > lastNonEmptyRow + 2)
				settings.Rows = settings.Rows.GetRange(0, lastNonEmptyRow + 2);

			var newFile = JsonSerializer.Serialize(settings, _serializerOptions);

			bool createNewFile;
			if (File.Exists(_settingsFile))
			{
				var oldFile = File.ReadAllText(_settingsFile);
				createNewFile = oldFile != newFile;

				if (createNewFile)
				{
					File.Move(_settingsFile, _settingsFile + ".bak", true);
				}
			}
			else
			{
				createNewFile = true;
			}

			if (createNewFile)
			{
				File.WriteAllText(_settingsFile, newFile);
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Settings settings;
			if (File.Exists(_settingsFile))
			{
				try
				{
					settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsFile), _serializerOptions);
					settings.Height = Math.Max(110, settings.Height);
					settings.Width = Math.Max(400, settings.Width);

					if (settings.Rows == null)
					{
						throw new Exception("Rows settings are expected");
					}
				}
				catch (Exception ex)
				{
					AddGridRow(_expressions.Count, "", "", "");
					errorsLabel.Content = ex.Message;
					return;
				}
			}
			else
			{
				settings = new Settings
				{
					Top = 16,
					Left = 16,
					Height = 200,
					Width = 600,
					Rows =
					[
						new Settings.Row{ Variable = "v0", Expression = "123"},
						new Settings.Row{ Expression = "v0*2"}
					]
				};
			}

			Top = settings.Top;
			Left = settings.Left;
			Height = settings.Height;
			Width = settings.Width;

			foreach (var item in settings.Rows)
			{
				AddGridRow(_expressions.Count, item.Variable, item.Expression, item.Comment);
			}

			UpdateDeleteButtonVisibility();
			RefreshCalculations();
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			RefreshCalculations();
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is int rowIndex)
			{
				AddGridRow(_expressions.Count, "", "", "");
				UpdateDeleteButtonVisibility();
				ShiftValuesDown(rowIndex + 1);
				RefreshCalculations();
			}
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is int rowIndex)
			{
				// Don't delete if it's the only row or the last empty row
				if (_expressions.Count <= 1 || (rowIndex == _expressions.Count - 1 && string.IsNullOrWhiteSpace(_expressions[rowIndex].Text)))
					return;

				RemoveGridRow(rowIndex);
				UpdateDeleteButtonVisibility();
				RefreshCalculations();
			}
		}

		private void AddGridRow(int rowIndex, string newVariable, string newExpression, string newComment)
		{
			var newRow = new RowDefinition
			{
				Height = new GridLength(0, GridUnitType.Auto)
			};
			expressionsGrid.RowDefinitions.Add(newRow);

			var var = new TextBox { Text = newVariable };
			var.TextChanged += TextBox_TextChanged;
			Grid.SetRow(var, rowIndex);
			Grid.SetColumn(var, 0);
			expressionsGrid.Children.Add(var);
			_variables.Add(var);

			var eq = new Label { Content = "=" };
			Grid.SetRow(eq, rowIndex);
			Grid.SetColumn(eq, 1);
			expressionsGrid.Children.Add(eq);

			var exp = new TextBox { Text = newExpression };
			exp.TextChanged += TextBox_TextChanged;
			Grid.SetRow(exp, rowIndex);
			Grid.SetColumn(exp, 2);
			expressionsGrid.Children.Add(exp);
			_expressions.Add(exp);

			var res = new TextBox
			{
				IsReadOnly = true,
				HorizontalAlignment = HorizontalAlignment.Right,
				BorderThickness = new Thickness(0)
			};
			Grid.SetRow(res, rowIndex);
			Grid.SetColumn(res, 3);
			expressionsGrid.Children.Add(res);
			_results.Add(res);

			var com = new TextBox { Text = newComment };
			Grid.SetRow(com, rowIndex);
			Grid.SetColumn(com, 4);
			expressionsGrid.Children.Add(com);
			_comments.Add(com);

			var addButton = new Button
			{
				Tag = rowIndex,
				ToolTip = "Insert row below",
				Content = new Image
				{
					Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/images/add.png")),
					Stretch = System.Windows.Media.Stretch.Uniform
				}
			};
			addButton.Click += AddButton_Click;
			Grid.SetRow(addButton, rowIndex);
			Grid.SetColumn(addButton, 5);
			expressionsGrid.Children.Add(addButton);
			_addButtons.Add(addButton);

			var deleteButton = new Button
			{
				Tag = rowIndex,
				ToolTip = "Delete row",
				Visibility = Visibility.Hidden,
				Content = new Image
				{
					Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/images/delete.png")),
					Stretch = System.Windows.Media.Stretch.Uniform
				}
			};
			deleteButton.Click += DeleteButton_Click;
			Grid.SetRow(deleteButton, rowIndex);
			Grid.SetColumn(deleteButton, 6);
			expressionsGrid.Children.Add(deleteButton);
			_delButtons.Add(deleteButton);
		}

		private void RemoveGridRow(int rowIndex)
		{
			var toRemove = expressionsGrid.Children.Cast<UIElement>().Where(el => Grid.GetRow(el) == rowIndex).ToList();
			foreach (var control in toRemove)
				expressionsGrid.Children.Remove(control);

			_variables.RemoveAt(rowIndex);
			_expressions.RemoveAt(rowIndex);
			_results.RemoveAt(rowIndex);
			_comments.RemoveAt(rowIndex);
			_addButtons.RemoveAt(rowIndex);
			_delButtons.RemoveAt(rowIndex);
			expressionsGrid.RowDefinitions.RemoveAt(rowIndex);

			var toUpdate = expressionsGrid.Children.Cast<UIElement>().Where(el => Grid.GetRow(el) > rowIndex).ToList();
			foreach (var control in toUpdate)
				Grid.SetRow(control, Grid.GetRow(control) - 1);

			for (int i = 0; i < _addButtons.Count; i++)
			{
				_addButtons[i].Tag = i;
				_delButtons[i].Tag = i;
			}
		}

		private void UpdateDeleteButtonVisibility()
		{
			int lastIndex = _addButtons.Count - 1;

			for (int i = 0; i < _addButtons.Count; i++)
			{
				var visibility = i == lastIndex ? Visibility.Hidden : Visibility.Visible;
				_addButtons[i].Visibility = visibility;
				_delButtons[i].Visibility = visibility;
			}
		}

		private void ShiftValuesDown(int startRowIndex)
		{
			for (int i = _expressions.Count - 1; i > startRowIndex; i--)
			{
				_variables[i].Text = _variables[i - 1].Text;
				_expressions[i].Text = _expressions[i - 1].Text;
				_comments[i].Text = _comments[i - 1].Text;
			}

			_variables[startRowIndex].Text = "";
			_expressions[startRowIndex].Text = "";
			_comments[startRowIndex].Text = "";
		}

		private void RefreshCalculations()
		{
			var errors = "";
			var interpreter = new Interpreter();

			for (var i = 0; i < _expressions.Count; i++)
			{
				var expression = _expressions[i].Text
					.Trim()
					.Replace(',', '.')
					.Replace("_", "");

				double? result = null;
				if (!string.IsNullOrWhiteSpace(expression))
				{
					try
					{
						result = interpreter.Eval<double>(expression);
					}
					catch (Exception ex)
					{
						errors += ex.Message;
						result = null;
					}
				}

				if (result.HasValue)
				{
					var res = Math.Round(result.Value, 4);
					_results[i].Text = res.ToString();

					if (!string.IsNullOrWhiteSpace(_variables[i].Text))
					{
						interpreter.SetVariable(_variables[i].Text.Trim(), res);
					}
				}
				else
				{
					_results[i].Text = "";
				}
			}

			errorsLabel.Content = errors.Length == 0 ? "OK" : errors;

			if (_expressions.Count == 0 || !string.IsNullOrWhiteSpace(_expressions[^1].Text))
			{
				AddGridRow(_expressions.Count, "", "", "");
				UpdateDeleteButtonVisibility();
			}
		}
	}
}
