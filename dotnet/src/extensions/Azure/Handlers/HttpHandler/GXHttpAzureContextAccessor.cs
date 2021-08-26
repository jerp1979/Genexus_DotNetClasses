using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker.Http;

namespace GeneXus.Deploy.AzureFunctions.HttpHandler
{
	public class GXHttpAzureContextAccessor : HttpContext
	{
		DefaultHttpContext defaultHttpContext = new DefaultHttpContext();
		public HttpResponse httpResponseData;
		public GXHttpAzureContextAccessor(HttpRequestData requestData, HttpResponseData responseData)
		{
			foreach (var header in requestData.Headers)
			{
				string[] values = new Microsoft.Extensions.Primitives.StringValues(header.Value.Select(val => val).ToArray());
				defaultHttpContext.Request.Headers[header.Key] = new Microsoft.Extensions.Primitives.StringValues(values);
			}
			defaultHttpContext.Request.Method = requestData.Method;
			defaultHttpContext.Request.Body = requestData.Body;
			defaultHttpContext.Request.Path = PathString.FromUriComponent(requestData.Url);
			defaultHttpContext.Request.QueryString = QueryString.FromUriComponent(requestData.Url);

			httpResponseData = new GxHttpAzureResponse(defaultHttpContext, responseData);
		}
		public override IFeatureCollection Features => defaultHttpContext.Features;

		public override HttpRequest Request => defaultHttpContext.Request;

		public override HttpResponse Response => httpResponseData;

		public override ConnectionInfo Connection => defaultHttpContext.Connection;

		public override WebSocketManager WebSockets => defaultHttpContext.WebSockets;

		public override ClaimsPrincipal User { get => defaultHttpContext.User; set => defaultHttpContext.User = value; }
		public override IDictionary<object, object> Items { get => defaultHttpContext.Items; set => defaultHttpContext.Items = value; }
		public override IServiceProvider RequestServices { get => defaultHttpContext.RequestServices; set => defaultHttpContext.RequestServices = value; }
		public override CancellationToken RequestAborted { get => defaultHttpContext.RequestAborted; set => defaultHttpContext.RequestAborted = value; }
		public override string TraceIdentifier { get => defaultHttpContext.TraceIdentifier; set => defaultHttpContext.TraceIdentifier = value; }
		public override ISession Session { get => new MockHttpSession(); set => defaultHttpContext.Session = value; }

		public override void Abort()
		{
			//throw new NotImplementedException();
		}
	}
	public class GxHttpAzureResponse : HttpResponse
	{
		HttpResponseData httpResponseData;
		HttpContext httpContext;

		private FeatureReferences<FeatureInterfaces> _features;

		private readonly static Func<IFeatureCollection, IHttpResponseFeature> _nullResponseFeature = f => null;
		private readonly static Func<IFeatureCollection, IHttpResponseBodyFeature> _nullResponseBodyFeature = f => null;
		private readonly static Func<IFeatureCollection, IResponseCookiesFeature> _newResponseCookiesFeature = f => new ResponseCookiesFeature(f);

		struct FeatureInterfaces
		{
			public IHttpResponseFeature Response;
			public IHttpResponseBodyFeature ResponseBody;
			public IResponseCookiesFeature Cookies;
		}
		public void Initialize()
		{
			_features.Initalize(httpContext.Features);
		}
		public void Initialize(int revision)
		{
			_features.Initalize(httpContext.Features, revision);
		}
		
		private IHttpResponseBodyFeature HttpResponseBodyFeature =>
		   _features.Fetch(ref _features.Cache.ResponseBody, _nullResponseBodyFeature);

		private IResponseCookiesFeature ResponseCookiesFeature =>
			_features.Fetch(ref _features.Cache.Cookies, _newResponseCookiesFeature);
		private IHttpResponseFeature HttpResponseFeature =>
		   _features.Fetch(ref _features.Cache.Response, _nullResponseFeature);

		public GxHttpAzureResponse(HttpContext context, HttpResponseData responseData)
		{
			httpResponseData = responseData;
			httpContext = context;
			_features.Initalize(context.Features);
		}
		public override HttpContext HttpContext => httpContext;

		public override int StatusCode { get => (int)httpResponseData.StatusCode; set => httpResponseData.StatusCode = (System.Net.HttpStatusCode)value; }

		public override IHeaderDictionary Headers
		{
			get 
			{
				IHeaderDictionary headers = new HeaderDictionary();
				foreach (var header in httpResponseData.Headers)
				{
					string[] values = new Microsoft.Extensions.Primitives.StringValues(header.Value.Select(val => val).ToArray());
					headers.Add(header.Key, values);
				}
					return headers;
			}
		}
		public override Stream Body { get => httpResponseData.Body; set => httpResponseData.Body = value; }	
		public override long? ContentLength {get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override string ContentType
		{
			get
			{
				var headers = from head in httpResponseData.Headers
							 where head.Key == "Content-Type"
							 select head;				
				foreach (var header in headers)
				{
					string[] values = new Microsoft.Extensions.Primitives.StringValues(header.Value.Select(val => val).ToArray());
					return (values.First());
				}
				return ("application/json");
			}

			set
			{
				if (!string.IsNullOrEmpty(ContentType))
					httpResponseData.Headers.Remove("Content-Type");
				httpResponseData.Headers.Add("Content-Type", value);
			}
		}
		public override IResponseCookies Cookies => throw new NotImplementedException();

		public override bool HasStarted
		{
			get { return HttpResponseFeature.HasStarted; }
		}

		public override void OnCompleted(Func<object, Task> callback, object state)
		{
			//throw new NotImplementedException();
		}
		public override void OnStarting(Func<object, Task> callback, object state)
		{
			//throw new NotImplementedException();
		}

		public override void Redirect(string location, bool permanent)
		{
			//throw new NotImplementedException();
		}
		public override PipeWriter BodyWriter
		{
			get
			{
				return (PipeWriter.Create(Body));		
			}
		}
	}
}