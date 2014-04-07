﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace FastLoader.Classes
{
	public class HttpWebRequestIndicate
	{
		public HttpWebRequestIndicate(HttpWebRequest request)
		{
			HttpRequest = request;
		}

		public HttpWebRequest HttpRequest
		{
			get;
			private set;
		}

		public bool IsPerformed
		{
			get;
			set;
		}

		public void Abort()
		{
			HttpRequest.Abort();
			IsPerformed = false;
		}

		public IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
		{
			IsPerformed = true;
			return HttpRequest.BeginGetResponse(callback, state);
		}

		public WebResponse EndGetResponse(IAsyncResult asyncResult)
		{
			return HttpRequest.EndGetResponse(asyncResult);
		}

	}
}