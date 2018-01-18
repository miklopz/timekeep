using System;
using System.Collections.Generic;

namespace TimeKeep.Web.API.Models
{
    public sealed class ResponseObject<T>
    {
        private IEnumerable<Category> categories;

        public T Result { get; set; }

        public ResponseObject()
        {

        }

        public ResponseObject(T result)
        {
            Result = result;
        }

        public ResponseObject(IEnumerable<Category> categories)
        {
            this.categories = categories;
        }
    }
}