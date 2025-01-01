
using System;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;

namespace TestClrAssembly
{
#if false
CREATE AGGREGATE [dbo].[JoinStringWithCommas]
(
	@value [NVARCHAR](4000)
)
RETURNS [NVARCHAR](MAX)
EXTERNAL NAME [TestClrAssembly].[TestClrAssembly.JoinStringWithCommas]
GO
#endif

    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        MaxByteSize = -1,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        IsNullIfEmpty = true)]
    public class JoinStringWithCommas : IBinarySerialize
    {
        private StringBuilder _buffer = new StringBuilder();

        public void Init()
        {
        }

        public void Accumulate(SqlString sqlValue)
        {
            if (!sqlValue.IsNull)
            {
                if (_buffer.Length > 0)
                {
                    _buffer.Append(",");
                }

                _buffer.Append(sqlValue.ToString());
            }
        }

        public void Merge(JoinStringWithCommas other)
        {
            if (other._buffer.Length > 0)
            {
                if (_buffer.Length > 0)
                {
                    _buffer.Append(",");
                }

                _buffer.Append(other._buffer);
            }
        }

        public SqlString Terminate()
        {
            if (_buffer.Length == 0)
            {
                return SqlString.Null;
            }

            return new SqlString(_buffer.ToString());
        }

        public void Read(BinaryReader r)
        {
            _buffer = new StringBuilder(r.ReadString());
        }

        public void Write(BinaryWriter w)
        {
            w.Write(_buffer.ToString());
        }
    }
}