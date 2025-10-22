// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Xml;
using System.Xml.Linq;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Formatters;

/// <summary>
/// Formatter for exporting outbox messages to XML format
/// Useful for enterprise systems and legacy integrations
/// </summary>
public class XmlFormatter : IDataFormatter
{
    public string FormatName => "xml";
    public string ContentType => "application/xml";

    /// <summary>
    /// Formats messages as an XML document
    /// </summary>
    public string Format(List<OutboxMessage> messages)
    {
        var root = new XElement("OutboxMessages",
            new XAttribute("count", messages.Count),
            new XAttribute("exportedAt", DateTime.UtcNow.ToString("o")),
            messages.Select(m => FormatMessage(m))
        );

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = System.Text.Encoding.UTF8
        };

        using var writer = new System.IO.StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, settings);
        root.WriteTo(xmlWriter);
        xmlWriter.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// Formats a single message as an XML element
    /// </summary>
    private XElement FormatMessage(OutboxMessage message)
    {
        return new XElement("Message",
            new XElement("Id", message.Id),
            new XElement("IdempotencyKey", message.IdempotencyKey),
            new XElement("AggregateId", message.AggregateId),
            new XElement("AggregateType", message.AggregateType),
            new XElement("EventType", message.EventTypeName),
            new XElement("Topic", message.Topic),
            new XElement("State", message.State.ToString()),
            new XElement("PublishAttempts", message.PublishAttempts),
            new XElement("MaxPublishAttempts", message.MaxPublishAttempts),
            new XElement("CreatedAt", message.CreatedAt.ToString("o")),
            message.PublishedAt.HasValue ? new XElement("PublishedAt", message.PublishedAt.Value.ToString("o")) : null,
            !string.IsNullOrEmpty(message.ErrorMessage) ? new XElement("ErrorMessage", message.ErrorMessage) : null,
            !string.IsNullOrEmpty(message.PartitionKey) ? new XElement("PartitionKey", message.PartitionKey) : null,
            !string.IsNullOrEmpty(message.CorrelationId) ? new XElement("CorrelationId", message.CorrelationId) : null,
            !string.IsNullOrEmpty(message.EventData) ? new XElement("EventData", message.EventData) : null
        );
    }
}
