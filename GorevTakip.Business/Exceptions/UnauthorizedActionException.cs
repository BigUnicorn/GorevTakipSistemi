using System;

namespace GorevTakip.Business.Exceptions
{
    public class UnauthorizedActionException : Exception
    {
        public UnauthorizedActionException(string message) : base(message)
        {
        }
    }
}
