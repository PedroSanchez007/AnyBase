using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AnyBase
{
    public abstract class QueryResult
    {
        public QueryResult()
        {
        }

        public QueryResult(List<CrudError> errors)
        {
            Errors = errors.ToList();
        }

        public List<CrudError> Errors { get; }
    }

    public class GenericReadResult<T> : QueryResult
    {
        public GenericReadResult(IEnumerable<T> records, List<CrudError> errors) : 
            base(errors)
        {
            Records = records;
        }

        public IEnumerable<T> Records { get; }
    }

    public class NonGenericReadResult : QueryResult
    {
        public NonGenericReadResult(DataTable queryData, List<CrudError> errors) : 
            base(errors)
        {
            Data = queryData;
        }

        public DataTable Data { get; }
    }

    public class CudResult : QueryResult
    {
        public CudResult()
        {
        }

        public CudResult(int affectedRowsCount, List<CrudError> errors) : 
            base(errors)
        {
            AffectedRowsCount = affectedRowsCount;
        }
        //
        // public CudResult(CudResult existingCudResult, CudResult newCudResult) :
        //     base(existingCudResult.Errors.AddRange(newCudResult.Errors));
        // {
        //     AffectedRowsCount = existingCudResult.AffectedRowsCount + newCudResult.AffectedRowsCount;
        // }

        public int AffectedRowsCount { get; }
    }

    public class ScalarResult : QueryResult
    {
        public ScalarResult(object scalarValue, CrudError scalarError) : 
            base(  new List<CrudError>{ scalarError})
        {
            ScalarValue = scalarValue;
        }

        public object ScalarValue { get; }
    }
}