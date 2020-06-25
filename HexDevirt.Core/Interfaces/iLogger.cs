using System;

namespace HexDevirt.Core
{
    public interface iLogger
    {
        public void Success(object message);
        public void Warning(object message);
        public void Error(object message);
        public void Info(object message);
        public void ShowInfo(Version version);
    }
}