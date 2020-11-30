using System;

namespace AnyBase
{
    /// <summary>
    ///  Categorise the exception.
    ///  </summary>
    ///  <remarks></remarks>
    public class CrudError
    {
        private readonly string errorType;
        private readonly string errorMessage;

        public CrudError(string errorType, string errorMessage, Exception mySqlError)
        {
            this.errorType = errorType;
            this.errorMessage = errorMessage;
            DbError = mySqlError;
        }

        public CrudError(Exception dbException)
        {
            DbError = dbException;

            if (dbException.Message.StartsWith("constraint failed"))
            {
                errorType = "Constraint broken when inserting";
                errorMessage = "Unique constraint failed when trying to insert the record.";
            }
            else if (dbException.Message == "Table * doesn't exist")
            {
                // TODO Fix regex above "Table * doesn't exist"
                errorType = "Table doesn't exist.";
            }
            else if (dbException.Message.Contains("too many SQL variables"))
            {
                 // ***** Other mySQL categorised exceptions here *****
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dbException"></param>
        /// <remarks></remarks>
        public CrudError(Exception dbException, string columnName)
        {
            DbError = dbException;

            errorType = "Cannot add column to table.";
            errorMessage = "Column already exists.";
        }

        public string ConcatenatedError
        {
            get
            {
                var result = $"{errorType} {errorMessage} {DbError.Message}";
                return result;
            }
        }

        public string ErrorType => errorType.EmptyIfNull();

        public string ErrorMessage => errorMessage.EmptyIfNull();

        public Exception DbError { get; }
    }
}