using System.Collections.Generic;

namespace VariableCalculator
{
	public class Settings
	{
		public double Top { get; set; }
		public double Left { get; set; }
		public double Height { get; set; }
		public double Width { get; set; }
		public List<Row> Rows { get; set; }

		public class Row
		{
			public string Variable { get; set; }
			public string Expression { get; set; }
			public string Comment { get; set; }
		}
	}
}
