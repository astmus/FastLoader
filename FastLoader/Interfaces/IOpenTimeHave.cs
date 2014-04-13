using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Interfaces
{
	public interface IWebItem
	{
		DateTime OpenTime { get; set; }
		String Link { get; set; }
	}
}
