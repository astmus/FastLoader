using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.DB
{
	[global::System.Data.Linq.Mapping.TableAttribute(Name = "HistoryItem")]
	public class HistoryItem
	{
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

		[Column(Storage = "FormatedSize", CanBeNull = false)]
		public string FormatedSize { get; set; }

		[Column(Storage = "Size", CanBeNull = false)]
		public long Size { get; set; }

		[Column(Storage = "HistoryItemId", AutoSync = AutoSync.OnInsert, IsPrimaryKey = true, IsDbGenerated = true)]
		public int HistoryItemId { get; set; }

		public HistoryItem()
		{

		}

		public override string ToString()
		{
			return Title + " " + OpenTime.ToString("yyyy.MM.dd HH:mm:ss");
		}
	}
}
