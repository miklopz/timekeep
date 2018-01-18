using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using TimeKeep.Web.API.Data;

namespace TimeKeep.Web.API.Models
{
    public sealed class Category : IComparable, IComparable<Category>
    {
        public Guid ID { get; set; }
        public string Description { get; set; }
        public bool IsScorecard { get; set; }
        public bool IsOut { get; set; }

        public Category()
        {

        }

        private static Category ReadOneFromDataReader(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (reader.IsClosed)
                throw new ArgumentException("reader is closed");

            return new Category
            {
                ID = (Guid)reader["RowID"],
                Description = (string)reader["Description"],
                IsScorecard = (bool)reader["IsScorecard"],
                IsOut = (bool)reader["IsOut"]
            };
        }

        private static IEnumerable<Category> ReadListFromDataReader(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (reader.IsClosed)
                throw new ArgumentException("reader is closed");

            IList<Category> list = new List<Category>();

            while (reader.Read())
                list.Add(ReadOneFromDataReader(reader));
            return list;
        }

        private static IEnumerable<Category> ReadAll()
        {
            DataAccess dal = new DataAccess("Categories");
            using (IDataReader reader = dal.ReadAll())
            {
                return ReadListFromDataReader(reader);
            }
        }

        private static object _opLock = new object();
        private static IEnumerable<Category> _categories;
        public static IEnumerable<Category> Categories
        {
            get
            {
                if (_categories == null)
                {
                    lock (_opLock)
                    {
                        if(_categories == null)
                        {
                            _categories = ReadAll();
                        }
                    }
                }
                return _categories;
            }
        }

        public static Category ReadByID(Guid id)
        {
            return Categories.Where(c => c.ID.Equals(id)).FirstOrDefault<Category>();
        }

        public override bool Equals(object obj)
        {
            Category val = obj as Category;
            if (val == null) return false;
            return ID.Equals(val.ID);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override string ToString()
        {
            return Description;
        }

        // Order priority ascending
        // 1. Scorecard <
        // 2. Non-scorecard && !out <
        // 3. Non-scorecard && out <
        // 4. Description based
        // Tests
        public int CompareTo(Category other)
        {
            if (other == null)
                return -1;
            if (this.IsScorecard == other.IsScorecard)
            {
                if (this.IsOut == other.IsOut)
                    return this.Description.CompareTo(other.Description);
                else if (this.IsOut)
                    return 1;
                return -1;
            }
            else if (this.IsScorecard)
                return -1;

            return 1;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return -1;
            Category other = obj as Category;
            if (other == null)
                throw new ArgumentException("obj must be of type Category", "obj");
            return CompareTo(other);
        }
    }
}