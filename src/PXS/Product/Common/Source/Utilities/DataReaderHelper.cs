// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// Data Reader Helper is intended to be a generic helper class to make an IDataReader result easier to consume
    /// </summary>
    public class DataReaderHelper : IDisposable
    {
        public IDataReader Reader { get; set; }
        public int RecordsRead { get; private set; }

        public string[] ColumnNames { get; private set; }
        public object[] RowValues { get; private set; }

        public DataReaderHelper(IDataReader reader = null)
        {
            this.RecordsRead = 0;
            this.Reader = reader;
            this.Initialize();
        }

        public void Dispose()
        {
            if (this.Reader != null)
            {
                this.Reader.Close();
                this.Reader.Dispose();
            }
        }

        public string ColumnName(int col)
        {
            return this.ColumnNames[col];
        }

        private void Initialize()
        {
            if (this.Reader == null)
            {
                return;
            }

            this.RowValues = new object[this.Reader.FieldCount];
 
            var schema = this.Reader.GetSchemaTable();
            if (schema == null)
            {
                return;
            }
            int columnNameColumn = schema.Columns["ColumnName"].Ordinal;
            this.ColumnNames = new string[schema.Rows.Count];
            for (int i = 0; i < schema.Rows.Count; i++)
            {
                this.ColumnNames[i] = (string)schema.Rows[i][columnNameColumn];
            }
        }

        public bool Read()
        {
            if (this.Reader == null)
            {
                throw new ArgumentNullException("DataReaderHelper.Reader");
            }
            var success = this.Reader.Read();
            if (success)
            {
                this.Reader.GetValues(this.RowValues);
                this.RecordsRead++;
            }
            return success;
        }

        public int FieldCount
        {
            get { return this.Reader.FieldCount; }
        }

        public string GetCell(int column)
        {
            return (string)this.Reader.GetValue(column);
        }

        public string GetBareUserId(int idColumn)
        {
            var id = GetCell(idColumn);
            if (id.StartsWith("p:"))
            {
                return id.Substring(2);
            }
            throw new ArgumentException("userId from query is invalid " + id);
        }

    }
}
