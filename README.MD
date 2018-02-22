Example
-------


```
<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  <root>
    <level value="WARN" />
    <appender-ref ref="asyncForwarder" />
  </root>


  <appender name="asyncForwarder" type="Log4Net.Async.ParallelForwardingAppender,Log4Net.Async">
    <appender-ref ref="UdpAppender" />
    <bufferSize value="2147483644" />
  </appender>

<appender name="UdpAppender" type="log4net.Appender.UdpAppender">
    <remoteAddress value="10.10.10.10" />
    <remotePort value="3995" />
    <layout type="log4net.Layout.SerializedLayout, log4net.Ext.JsonStandard" ></layout>
  </appender>
</log4net>
```
