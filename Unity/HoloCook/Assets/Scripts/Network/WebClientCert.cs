//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace HoloCook.Network
{
    /// <summary>
    /// An expanded web client that allows certificate auth and 
    /// the retrieval of status' for successful requests
    /// </summary>
    public class WebClientCert : WebClient
    {
        private X509Certificate2 _cert;

        public WebClientCert(X509Certificate2 cert) : base()
        {
            _cert = cert;
        }

        public WebClientCert() : base()
        {
            _cert = null;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 1000 * 1000; // 1000 second
            request.ReadWriteTimeout = 1000 * 1000;
            if (_cert != null)
            {
                request.ClientCertificates.Add(_cert);
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;
            response = base.GetWebResponse(request);
            HttpWebResponse baseResponse = response as HttpWebResponse;
            StatusCode = baseResponse.StatusCode;
            StatusDescription = baseResponse.StatusDescription;
            return response;
        }

        /// <summary>
        /// The most recent response statusCode
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The most recent response statusDescription
        /// </summary>
        public string StatusDescription { get; set; }
    }
}
