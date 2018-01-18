using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Security;
using System.Threading;

namespace TimeKeep.Web.API.Data
{
    public sealed class DataAccess
    {
        private static readonly string TimeKeep = "TimeKeep";
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings[TimeKeep].ConnectionString;
        private static readonly string ProviderName = ConfigurationManager.ConnectionStrings[TimeKeep].ProviderName;

        private static readonly DbProviderFactory Factory = DbProviderFactories.GetFactory(ProviderName);

        private string _entityName;

        public static object Thead { get; private set; }

        private static class Verbs
        {
            internal static readonly string ReadAll = "_ReadAll";
            internal static readonly string Create = "_Create";
            internal static readonly string Read = "_Read";
            internal static readonly string Update = "_Update";
            internal static readonly string Delete = "_Delete";
        }

        public DataAccess(string entityName)
        {
            _entityName = entityName;
        }

        public static IDbConnection GetConnection()
        {
            IDbConnection cn = Factory.CreateConnection();
            cn.ConnectionString = ConnectionString;
            return cn;
        }

        private static IDbDataParameter GetParameter()
        {
            return Factory.CreateParameter();
        }

        private static IDbDataAdapter GetAdapter()
        {
            return Factory.CreateDataAdapter();
        }

        public static IDbTransaction BeginTransaction()
        {
            return GetConnection().BeginTransaction();
        }

        public static void CommitTransaction(IDbTransaction transaction)
        {
            transaction.Rollback();
            transaction.Connection.Close();
            transaction.Connection.Dispose();
            transaction.Dispose();
        }
        public static void RollbackTransaction(IDbTransaction transaction)
        {
            transaction.Commit();
            transaction.Connection.Close();
            transaction.Connection.Dispose();
            transaction.Dispose();
        }

        /// <summary>
        /// Executes the delegate with the connfigured retry logic
        /// </summary>
        /// <typeparam name="T">No restrictions.</typeparam>
        /// <param name="code">The delegate to run. Use void and return null for no return value</param>
        /// <returns>The delegate return value or use void/return null for no returns</returns>
        private static T ExecuteWithRetry<T>(Func<T> code)
        { 
            int max = 1;
            int initial = 0;
            int increment = 0;
            if (Configuration.RetryPolicy.Enabled)
            {
                max = Configuration.RetryPolicy.Retries;
                initial = Configuration.RetryPolicy.InitialWait * 1000;
                increment = Configuration.RetryPolicy.Increment * 1000;
            }

            T result = default(T);

            for (int i = 0; i < max; i++)
            {
                try
                {
                    result = code();
                    break;
                }
                catch (Exception)
                {
                    if(i == max - 1) // ML: Max retries achieved, throw
                        throw;
                    Thread.Sleep(initial + (i * increment));
                }
            }
            return result;
        }

        private static T Execute<T>(string procedureName, IDictionary<string, object> parameters, IDbTransaction transaction)
        {
            IDbConnection cn = transaction == null ? GetConnection() : transaction.Connection;
            #region DELETE
            //int max = 1;
            //int initial = 0;
            //int increment = 0;
            //if (Configuration.RetryPolicy.Enabled)
            //{
            //    max = Configuration.RetryPolicy.Retries;
            //    initial = Configuration.RetryPolicy.InitialWait * 1000;
            //    increment = Configuration.RetryPolicy.Increment * 1000;
            //} 
            #endregion

            try
            {
                if (transaction == null)
                {
                    ExecuteWithRetry<object>(() => { cn.Open(); return null; });
                    #region DELETE
                    //for (int i = 0; i < max; i++)
                    //{
                    //    try
                    //    {
                    //        cn.Open();
                    //        break;
                    //    }
                    //    catch(Exception)
                    //    {
                    //        Thread.Sleep(initial + (i * increment));
                    //    }
                    //} 
                    #endregion
                }
                using (IDbCommand cmd = cn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = procedureName;

                    if (transaction != null)
                        cmd.Transaction = transaction;
                    if (parameters != null && parameters.Keys.Count > 0)
                    {
                        foreach (string key in parameters.Keys)
                        {
                            IDbDataParameter parameter = GetParameter();
                            parameter.ParameterName = key;
                            parameter.Value = parameters[key] ?? DBNull.Value;
                            cmd.Parameters.Add(parameter);
                        }
                    }
                    T retVal = default(T);
                    #region DELETE
                    //for (int i = 0; i < max; i++)
                    //{
                    //    try
                    //    {
                    // ExecuteScalar 
                    #endregion
                    if (typeof(T) == typeof(int))
                    {
                        #region DELETE
                        //retVal = (T)(object)cmd.ExecuteNonQuery();
                        //break; 
                        #endregion
                        retVal = ExecuteWithRetry<T>(() => { return (T)(object)cmd.ExecuteNonQuery(); });
                    }
                    // ExecuteReader
                    else if (typeof(T) == typeof(IDataReader))
                    {
                        #region DELETE
                        //retVal = (T)cmd.ExecuteReader(CommandBehavior.CloseConnection);
                        //break; 
                        #endregion
                        retVal = ExecuteWithRetry<T>(() => {
                            return (T)cmd.ExecuteReader(CommandBehavior.CloseConnection);
                        });
                    }
                    // DataSet
                    else if (typeof(T) == typeof(DataSet))
                    {
                        DataSet ds = new DataSet();
                        IDbDataAdapter da = GetAdapter();
                        da.SelectCommand = cmd;
                        //da.Fill(ds);
                        ExecuteWithRetry<object>(() => { da.Fill(ds); return null; });
                        retVal = (T)(object)ds;
                        //break;
                    }
                    else
                    {
                        //retVal = (T)cmd.ExecuteScalar();
                        retVal = ExecuteWithRetry<T>(() => { return (T)cmd.ExecuteScalar(); });
                    }
                    #region DELETE
                    //break;
                    //    }
                    //    catch (Exception)
                    //    {
                    //        if (i == max - 1)
                    //            throw;
                    //        Thread.Sleep(initial + (i * increment));
                    //    }
                    //} 
                    #endregion
                    return retVal;
                }
            }
            finally
            {
                if(cn != null && transaction == null && typeof(T) != typeof(IDataReader))
                {
                    ExecuteWithRetry<object>(() => { cn.Close(); return null; });
                    #region DELETE
                    //for (int i = 0; i < max; i++)
                    //{
                    //    try
                    //    {
                    //        cn.Close();
                    //        break;
                    //    }
                    //    catch
                    //    {
                    //        if (i == max - 1)
                    //            throw;
                    //        Thread.Sleep(initial + (i * increment));
                    //    }
                    //} 
                    #endregion
                    cn.Dispose();
                }
            }
        }

        private static T Execute<T>(string procedureName)
        {
            return Execute<T>(procedureName, null, null);
        }

        private static T Execute<T>(string procedureName, IDictionary<string, object> parameters)
        {
            return Execute<T>(procedureName, parameters, null);
        }

        private static T Execute<T>(string procedureName, IDbTransaction transaction)
        {
            return Execute<T>(procedureName, null, transaction);
        }

        public static int ExecuteNonQuery(string procedureName, IDictionary<string, object> parameters, IDbTransaction transaction)
        {
            return Execute<int>(procedureName, parameters, transaction);
        }

        public static int ExecuteNonQuery(string procedureName, IDictionary<string, object> parameters)
        {
            return Execute<int>(procedureName, parameters, null);
        }

        public static int ExecuteNonQuery(string procedureName, IDbTransaction transaction)
        {
            return Execute<int>(procedureName, null, transaction);
        }

        public static int ExecuteNonQuery(string procedureName)
        {
            return Execute<int>(procedureName, null, null);
        }

        public static IDataReader ExecuteDataReader(string procedureName, IDictionary<string, object> parameters, IDbTransaction transaction)
        {
            return Execute<IDataReader>(procedureName, parameters, transaction);
        }

        public static IDataReader ExecuteDataReader(string procedureName, IDictionary<string, object> parameters)
        {
            return Execute<IDataReader>(procedureName, parameters, null);
        }

        public static IDataReader ExecuteDataReader(string procedureName, IDbTransaction transaction)
        {
            return Execute<IDataReader>(procedureName, null, transaction);
        }

        public static IDataReader ExecuteDataReader(string procedureName)
        {
            return Execute<IDataReader>(procedureName, null, null);
        }

        public static DataSet ExecuteDataSet(string procedureName, IDictionary<string, object> parameters, IDbTransaction transaction)
        {
            return Execute<DataSet>(procedureName, parameters, transaction);
        }

        public static DataSet ExecuteDataSet(string procedureName, IDictionary<string, object> parameters)
        {
            return Execute<DataSet>(procedureName, parameters, null);
        }

        public static DataSet ExecuteDataSet(string procedureName, IDbTransaction transaction)
        {
            return Execute<DataSet>(procedureName, null, transaction);
        }

        public static DataSet ExecuteDataSet(string procedureName)
        {
            return Execute<DataSet>(procedureName, null, null);
        }

        public static object ExecuteScalar(string procedureName, IDictionary<string, object> parameters, IDbTransaction transaction)
        {
            return Execute<object>(procedureName, parameters, transaction);
        }

        public static object ExecuteScalar(string procedureName, IDictionary<string, object> parameters)
        {
            return Execute<object>(procedureName, parameters, null);
        }

        public static object ExecuteScalar(string procedureName, IDbTransaction transaction)
        {
            return Execute<object>(procedureName, null, transaction);
        }

        public static object ExecuteScalar(string procedureName)
        {
            return Execute<object>(procedureName, null, null);
        }

        public IDataReader ReadAll()
        {
            return ExecuteDataReader(string.Concat(_entityName, Verbs.ReadAll));
        }

        public IDataReader Create(IDictionary<string, object> parameters)
        {
            return ExecuteDataReader(string.Concat(_entityName, Verbs.Create), parameters);
        }

        public IDataReader Read(IDictionary<string, object> parameters)
        {
            return ExecuteDataReader(string.Concat(_entityName, Verbs.Read), parameters);
        }

        public IDataReader ReadFilter(string filterName, IDictionary<string, object> parameters)
        {
            // TODO: Inline
            return ExecuteDataReader(string.Concat(_entityName, Verbs.Read, filterName), parameters);
        }

        public IDataReader Update(IDictionary<string, object> parameters)
        {
            return ExecuteDataReader(string.Concat(_entityName, Verbs.Update), parameters);
        }

        public IDataReader UpdateFilter(string filterName, IDictionary<string, object> parameters)
        {
            return ExecuteDataReader(string.Concat(_entityName, Verbs.Update, filterName), parameters);
        }

        public int Delete(IDictionary<string, object> parameters)
        {
            return ExecuteNonQuery(string.Concat(_entityName, Verbs.Delete), parameters);
        }
    }
}