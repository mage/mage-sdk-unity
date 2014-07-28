using System;

namespace MAGE {

    public class MageError: ApplicationException {
        public MageError() : base() {}
        public MageError(string message) : base(message) {}
    }

    public class MageRPCError: MageError {
        public MageRPCError(string code, string message) : base(message) {
            errorCode = code;
        }
        public string code() {
            return errorCode;
        }
        private string errorCode;
    }

    public class MageErrorMessage: MageError {
        public MageErrorMessage(string code, string message) : base(message) {
            errorCode = code;
        }
        public string code() {
            return errorCode;
        }
        private string errorCode;
    }

}
