using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Micser.Common;
using Newtonsoft.Json;

namespace Micser.App.Infrastructure.Api
{
    public abstract class ApiClient
    {
        private static readonly HttpClient _httpClient;
        private readonly string _resource;

        static ApiClient()
        {
            _httpClient = CreateClient();
        }

        protected ApiClient(string resource)
        {
            _resource = resource.TrimEnd('/') + "/";
        }

        protected async Task<ServiceResult<T>> DeleteAsync<T>(string action, object id, object parameters = null)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(_resource + action + "/" + id + GetQueryString(parameters));
                return await HandleResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return GetInternalErrorResult<T>(ex);
            }
        }

        protected async Task<ServiceResult<T>> GetAsync<T>(string action, object parameters = null)
        {
            try
            {
                var url = GetResourceUrl(action, parameters);
                var response = await _httpClient.GetAsync(url);
                return await HandleResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return GetInternalErrorResult<T>(ex);
            }
        }

        protected string GetResourceUrl(string action, object parameters)
        {
            return _resource + action + GetQueryString(parameters);
        }

        protected async Task<ServiceResult<Stream>> GetStreamAsync(string action, object parameters = null)
        {
            try
            {
                var url = GetResourceUrl(action, parameters);
                var response = await _httpClient.GetAsync(url);

                Stream result = null;
                string error = null;

                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStreamAsync();
                }
                else
                {
                    error = response.ReasonPhrase;
                }

                return new ServiceResult<Stream>(response, result, new ErrorList(error));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return GetInternalErrorResult<Stream>(ex);
            }
        }

        protected async Task<ServiceResult<T>> PostAsync<T>(string action, object data, object parameters = null)
        {
            try
            {
                var url = GetResourceUrl(action, parameters);
                var content = JsonConvert.SerializeObject(data);

                var response = await _httpClient.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"));
                return await HandleResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return GetInternalErrorResult<T>(ex);
            }
        }

        protected async Task<ServiceResult<T>> PutAsync<T>(string action, object id, object data, object parameters = null)
        {
            try
            {
                var content = JsonConvert.SerializeObject(data);
                var response = await _httpClient.PutAsync(_resource + action + "/" + id + GetQueryString(parameters),
                                                          new StringContent(content, Encoding.UTF8, "application/json"));
                return await HandleResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return GetInternalErrorResult<T>(ex);
            }
        }

        private static HttpClient CreateClient()
        {
            var baseAddress = new Uri($"http://localhost:{Globals.ApiPort}/api/");

            try
            {
                // http://byterot.blogspot.com/2016/07/singleton-httpclient-dns.html
                var servicePoint = ServicePointManager.FindServicePoint(baseAddress);
                servicePoint.ConnectionLeaseTimeout = 60 * 1000;
            }
            catch
            {
                // ConnectionLeaseTimeout is not implemented on mono/android
            }

            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler, true)
            {
                Timeout = TimeSpan.FromSeconds(10),
                BaseAddress = baseAddress
            };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        private static ServiceResult<T> GetInternalErrorResult<T>(Exception ex)
        {
            return new ServiceResult<T>(Globals.InternalErrorStatusCode, default(T), new ErrorList(ex));
        }

        private static async Task<ServiceResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
        {
            var data = default(T);
            ErrorList error = null;

            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                data = (T) JsonConvert.DeserializeObject(responseString, typeof(T));
            }
            else if (!string.IsNullOrEmpty(responseString))
            {
                if (responseString.StartsWith("<"))
                {
                    Debug.WriteLine(responseString);
                }
                else
                {
                    error = (ErrorList) JsonConvert.DeserializeObject(responseString, typeof(ErrorList));
                }
            }

            return new ServiceResult<T>(response, data, error);
        }

        #region Helpers

        protected static string GetQueryString(object parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            var items = new List<string>();
            var fields = parameters
                        .GetType()
                        .GetRuntimeFields()
                        .Where(f => f.IsPublic);

            foreach (var fieldInfo in fields)
            {
                var key = fieldInfo.Name;
                var value = fieldInfo.GetValue(parameters);
                var item = ParseValue(key, value);
                items.Add(item);
            }

            var properties = parameters
                            .GetType()
                            .GetRuntimeProperties()
                            .Where(p => p.CanRead);

            foreach (var propertyInfo in properties)
            {
                var key = propertyInfo.Name;
                var value = propertyInfo.GetValue(parameters);
                var item = ParseValue(key, value);
                items.Add(item);
            }

            if (items.Count > 0)
            {
                return "?" + string.Join("&", items);
            }

            return "";
        }

        private static string ParseValue(string key, object value)
        {
            if (value is IEnumerable enumerable)
            {
                var items = enumerable.Cast<object>().Select(x => $"{key}={x}");
                return string.Join("&", items);
            }

            return $"{key}={value}";
        }

        #endregion Helpers
    }
}