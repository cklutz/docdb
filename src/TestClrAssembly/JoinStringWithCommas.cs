
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
        private StringBuilder m_buffer;

        public void Init()
        {
            m_buffer = new StringBuilder();
        }

        public void Accumulate(SqlString sqlValue)
        {
            if (!sqlValue.IsNull)
            {
                if (m_buffer.Length > 0)
                {
                    m_buffer.Append(",");
                }

                m_buffer.Append(sqlValue.ToString());
            }
        }

        public void Merge(JoinStringWithCommas other)
        {
            if (other.m_buffer.Length > 0)
            {
                if (m_buffer.Length > 0)
                {
                    m_buffer.Append(",");
                }

                m_buffer.Append(other.m_buffer);
            }
        }

        public SqlString Terminate()
        {
            if (m_buffer.Length == 0)
            {
                return SqlString.Null;
            }

            return new SqlString(m_buffer.ToString());
        }

        public void Read(BinaryReader r)
        {
            m_buffer = new StringBuilder(r.ReadString());
        }

        public void Write(BinaryWriter w)
        {
            w.Write(m_buffer.ToString());
        }
    }
}