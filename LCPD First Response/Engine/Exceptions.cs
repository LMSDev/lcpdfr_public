using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCPD_First_Response.Engine
{
    class IllegalClientRequest : ApplicationException
    {
        public IllegalClientRequest() : base("There was an error handling a client request")
        {}
    }

    class IllegalServerResponseException : ApplicationException
    {
        public string Response { get; private set; }

        public IllegalServerResponseException(string response) : base("The server returned an illegal value")
        {
            this.Response = response;
        }

        public override string ToString()
        {
            return base.ToString() +" Server response: " + this.Response; 
        }
    }

    class NoServerConnectionException : Exception
    {
        public NoServerConnectionException() : base("Failed to connect to the server")
        {}
    }
}
