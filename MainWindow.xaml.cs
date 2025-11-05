using System;
using System.Collections.Generic;
using System.IO;
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
				var item = new Settings.Row { Variable = _variables[i].Text.Trim(), Expression = _expressions[i].Text.Trim(), Comment = _comments[i].Text.Trim() };

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

			TextBox_TextChanged(null, null);
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var errors = "";
			var interpreter = new Interpreter();

			for (var i = 0; i < _expressions.Count; i++)
			{
				var expression = _expressions[i].Text.Trim();

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
		}
	}
}
