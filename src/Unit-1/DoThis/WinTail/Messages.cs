using System;
using System.Collections.Generic;
using System.Text;

namespace WinTail
{
    public class Messages
    {
        public class ContinueProcessing { }

        public class InputSuccess
        {
            public InputSuccess(string reason)
            {
                this.Reason = reason;
            }

            public string Reason { get; }
        }

        public class InputError
        {
            public InputError(string reason)
            {
                this.Reason = reason;
            }

            public string Reason { get; }
        }

        public class NullInputError : InputError
        {
            public NullInputError(string reason) : base(reason)
            {
            }
        }

        public class ValidationError : InputError
        {
            public ValidationError(string reason) : base(reason)
            {
            }
        }
    }
}
