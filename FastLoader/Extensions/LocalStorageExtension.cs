using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Extensions
{
	public static class LocalStorageExtension
	{
		public static List<String> GetAllFiles(this IsolatedStorageFile store)
		{
			return GetFiles(store, "", "*.*");
		}

		public static List<String> GetFiles(this IsolatedStorageFile store, string mask)
		{
			return GetFiles(store, "", mask);
		}
		/// <summary>
		/// Return current size of user isolated storage
		/// </summary>        
		/// <returns></returns>
		public static long GetCurretnSize(this IsolatedStorageFile store)
		{
			List<String> files = store.GetAllFiles();
			long res = 0;
			foreach (string name in files)
			{
				//FileInfo f = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name));
				IsolatedStorageFileStream fs = store.OpenFile(name, FileMode.Open, FileAccess.Read);
				res += fs.Length;
				fs.Close();
			}
			return res;
		}

		/// <summary>
		/// Get files by mask from certain folder
		/// </summary>
		/// <param name="store"></param>
		/// <param name="path"> if string is "" then get files begin root folder, else path must end with \\</param>
		/// <param name="mask"> avaliable ? and *</param>
		/// <returns></returns>
		private static List<String> GetFiles(IsolatedStorageFile store, string path, string mask)
		{
			List<String> res = new List<String>();
			string[] dir = store.GetDirectoryNames(path + "*.*");

			string pathToFolder = path;
			res.AddRange(store.GetFileNames(path + mask).Select(elem => pathToFolder + elem));
			if (dir.Length == 0)
				return res;

			foreach (string s in dir)
				res.AddRange(GetFiles(store, pathToFolder + s + "\\", mask));

			return res;
		}
	}
}
