using DynamicExpresso;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfCoreCalculator
{
	public partial class MainWindow : Window
	{
		readonly List<TextBox> textboxes;
		readonly List<Label> labels;
		readonly List<TextBox> comments;

		public MainWindow()
		{
			InitializeComponent();

			textboxes = new List<TextBox> { text0, text1 };
			labels = new List<Label> { res0, res1 };
			comments = new List<TextBox> { comment0, comment1 };
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var interpreter = new Interpreter();

			for (var i = 0; i < textboxes.Count; i++)
			{
				var expression = textboxes[i].Text.Trim();

				double? result = null;
				if (!string.IsNullOrWhiteSpace(expression))
				{
					try
					{
						result = interpreter.Eval<double>(expression);
					}
					catch
					{
						result = null;
					}
				}

				if (result.HasValue)
				{
					labels[i].Content = result.Value;
					interpreter.SetVariable("v" + i, result.Value);
				}
				else
				{
					labels[i].Content = "NULL";
				}
			}
		}
	}
}
