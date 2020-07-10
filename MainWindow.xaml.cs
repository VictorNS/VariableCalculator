using DynamicExpresso;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using VariableCalculator;

namespace WpfCoreCalculator
{
	public partial class MainWindow : Window
	{
		readonly string settingsFile = Path.ChangeExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, "json");
		readonly List<TextBox> variables;
		readonly List<TextBox> expressions;
		readonly List<TextBox> results;
		readonly List<TextBox> comments;

		public MainWindow()
		{
			InitializeComponent();

			variables = new List<TextBox>();
			expressions = new List<TextBox>();
			results = new List<TextBox>();
			comments = new List<TextBox>();
		}

		private void Window_Closed(object sender, System.EventArgs e)
		{
			var settings = new Settings
			{
				Top = Top,
				Left = Left,
				Height = Height,
				Width = Width,
				Rows = new List<Settings.Row>()
			};

			for (var i = 0; i < expressions.Count; i++)
			{
				settings.Rows.Add(new Settings.Row { Variable = variables[i].Text.Trim(), Expression = expressions[i].Text.Trim(), Comment = comments[i].Text.Trim() });
			}

			var newFile = JsonSerializer.Serialize(settings, new JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
			});

			bool createNewFile;
			if (File.Exists(settingsFile))
			{
				var oldFile = File.ReadAllText(settingsFile);
				createNewFile = oldFile != newFile;

				if (createNewFile)
				{
					File.Move(settingsFile, settingsFile + ".bak", true);
				}
			}
			else
			{
				createNewFile = true;
			}

			if (createNewFile)
			{
				File.WriteAllText(settingsFile, newFile);
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Settings settings;
			if (File.Exists(settingsFile))
			{
				try
				{
					settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile), new JsonSerializerOptions
					{
						Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
					});
					settings.Height = Math.Max(110, settings.Height);
					settings.Width = Math.Max(400, settings.Width);

					if (settings.Rows == null)
					{
						throw new Exception("Rows settings are expected");
					}
				}
				catch (Exception ex)
				{
					AddGridRow(expressions.Count, "", "", "");
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
					Rows = new List<Settings.Row>
					{
						new Settings.Row{ Variable="v0", Expression="123"},
						new Settings.Row{ Expression="v0*2"}
					}
				};
			}

			Top = settings.Top;
			Left = settings.Left;
			Height = settings.Height;
			Width = settings.Width;

			foreach (var item in settings.Rows)
			{
				AddGridRow(expressions.Count, item.Variable, item.Expression, item.Comment);
			}

			TextBox_TextChanged(null, null);
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var errors = "";
			var interpreter = new Interpreter();

			for (var i = 0; i < expressions.Count; i++)
			{
				var expression = expressions[i].Text.Trim();

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
					results[i].Text = res.ToString();

					if (!string.IsNullOrWhiteSpace(variables[i].Text))
					{
						interpreter.SetVariable(variables[i].Text.Trim(), res);
					}
				}
				else
				{
					results[i].Text = "";
				}
			}

			errorsLabel.Content = errors.Length == 0 ? "OK" : errors;

			if (expressions.Count==0 || !string.IsNullOrWhiteSpace(expressions[expressions.Count - 1].Text))
			{
				AddGridRow(expressions.Count, "", "", "");
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
			variables.Add(var);

			var eq = new Label { Content = "=" };
			Grid.SetRow(eq, rowIndex);
			Grid.SetColumn(eq, 1);
			expressionsGrid.Children.Add(eq);

			var exp = new TextBox { Text = newExpression };
			exp.TextChanged += TextBox_TextChanged;
			Grid.SetRow(exp, rowIndex);
			Grid.SetColumn(exp, 2);
			expressionsGrid.Children.Add(exp);
			expressions.Add(exp);

			var res = new TextBox
			{
				IsReadOnly = true,
				HorizontalAlignment = HorizontalAlignment.Right,
				BorderThickness = new Thickness(0)
			};
			Grid.SetRow(res, rowIndex);
			Grid.SetColumn(res, 3);
			expressionsGrid.Children.Add(res);
			results.Add(res);

			var com = new TextBox { Text = newComment }; ;
			Grid.SetRow(com, rowIndex);
			Grid.SetColumn(com, 4);
			expressionsGrid.Children.Add(com);
			comments.Add(com);
		}
	}
}
