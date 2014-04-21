using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastLoader.Interfaces;

namespace FastLoader.DB
{
	[global::System.Data.Linq.Mapping.TableAttribute(Name = "HistoryDates")]
	public class HistoryItem : IWebItem
	{
		[Column(Storage = "HistoryItemId", AutoSync = AutoSync.OnInsert, IsPrimaryKey = true, IsDbGenerated = true)]
		public int HistoryItemId { get; set; }

		[Column(Storage = "Link", CanBeNull = false)]
		public String Link { get; set; }

		[Column(Storage = "Title", CanBeNull = false)]
		public String Title { get; set; }

		[Column(Storage = "OpenTime", CanBeNull = false)]
		public DateTime OpenTime { get; set; }

		String _fav;
		public String Favicon
		{
			get
			{
				if (_fav == null)
				{
					Uri u = new Uri(Link, UriKind.RelativeOrAbsolute);
					_fav = "http://www.google.com/s2/favicons?domain=" + u.Authority;
				}
				return _fav;
			}
			set
			{
				_fav = value;
			}
		}

		public override string ToString()
		{
			return this.GetHashCode().ToString();
		}
	}
}
