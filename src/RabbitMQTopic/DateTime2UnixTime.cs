using System;

namespace RabbitMQTopic
{
    /// <summary>
    /// DateTime和UnixTime间互转
    /// </summary>
    internal class DateTime2UnixTime
    {        
        private static readonly DateTime _startTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
        /// <summary>
        /// 将Unix时间戳转换为DateTime类型时间
        /// </summary>
        /// <param name="unixTime">double 型数字</param>
        /// <returns>DateTime</returns>
        public static DateTime FromUnixTime(long unixTime)
        {
            var time = _startTime.AddMilliseconds(unixTime);
            return time;
        }

        /// <summary>
        /// 将c# DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>long</returns>
        public static long ToUnixTime(DateTime time)
        {
            long t = (time.Ticks - _startTime.Ticks) / 10000; //除10000调整为13位
            return t;
        }
    }
}
