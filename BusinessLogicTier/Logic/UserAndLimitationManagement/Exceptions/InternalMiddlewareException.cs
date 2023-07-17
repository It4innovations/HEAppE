using System;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions
{
    public class InternalMiddlewareException : Exception
    {
        /// <summary>Constructs the InternalMiddlewareException. Don't init anything.</summary>
        public InternalMiddlewareException()
        {
            // don't init anything.
        }

        ///<summary>Constructs the InternalMiddlewareException.</summary>
        public InternalMiddlewareException
        (
            string msg
        )
        {
            this.msg = msg;
        }

        public String GetMessage()
        {
            return String.Format("msg={0}", msg);
        }

        public string msg;
        ///<summary>Gets Value</summary>
        /// <returns>return the value.</returns>
        public string GetMsg()
        {
            return msg;
        }
        ///<summary>Sets Value</summary>
        ///<param name="value">the value</param>
        public void SetMsg(string value)
        {
            this.msg = value;
        }
    }
}
