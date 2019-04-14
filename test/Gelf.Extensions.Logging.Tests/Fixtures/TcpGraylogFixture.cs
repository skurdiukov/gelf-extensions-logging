using Xunit.Abstractions;

namespace Gelf.Extensions.Logging.Tests.Fixtures
{
    public class TcpGraylogFixture : GraylogFixture
    {
        public TcpGraylogFixture(IMessageSink messageSink) : base(messageSink)
        {
        }

        public override int InputPort => 12201;

        public override string InputType => "org.graylog2.inputs.gelf.tcp.GELFTCPInput";

        public override string InputTitle => "Gelf.Extensions.Logging.Tests.Tcp";
    }
}
