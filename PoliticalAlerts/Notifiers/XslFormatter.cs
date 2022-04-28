﻿using PoliticalAlerts.Models;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Linq;
using System.Text;
using System;

namespace PoliticalAlerts.Notifiers
{
    public class XslFormatter
    {
        public Tuple<Stream, string> Format(IEnumerable<Match> matches, Observer observer, string template)
        {
            IEnumerable<IGrouping<Search, Match>> indexedBySearch = matches.GroupBy(m => m.Search);

            XslCompiledTransform transform = new XslCompiledTransform();
            transform.Load(XmlReader.Create(new StringReader(template)));

            XmlDocument source = new XmlDocument();
            var root = source.DocumentElement.AppendChild(source.CreateElement("notification"));

            foreach (IGrouping<Search, Match> groupedSearch in indexedBySearch)
            {
                var xmlSearch = source.CreateElement("search");
                root.AppendChild(xmlSearch);

                xmlSearch.SetAttribute("id", groupedSearch.Key.Id.ToString());
                xmlSearch.AppendChild(source.CreateElement("name", groupedSearch.Key.Name));
                xmlSearch.AppendChild(source.CreateElement("phrase", groupedSearch.Key.Phrase));

                foreach (Match match in groupedSearch)
                {
                    var xmlMatch = xmlSearch.AppendChild(source.CreateElement("meeting")) as XmlElement;

                    xmlMatch.AppendChild(source.CreateElement("excerpt")).InnerText = match.Excerpt;
                    xmlMatch.AppendChild(source.CreateElement("timeFound")).InnerText = match.TimeFound.ToString("g");

                    var xmlAgendaItem = xmlMatch.AppendChild(source.CreateElement("agendaitem")) as XmlElement;

                    xmlAgendaItem.SetAttribute("id", match.AgendaItem.Id.ToString());
                    xmlAgendaItem.SetAttribute("meetingId", match.AgendaItem.ExternalId.ToString());
                    xmlAgendaItem.AppendChild(source.CreateElement("title")).InnerText = match.AgendaItem.Title;
                    xmlAgendaItem.AppendChild(source.CreateElement("url")).InnerText = match.AgendaItem.Url.ToString();
                    xmlAgendaItem.AppendChild(source.CreateElement("date")).InnerText = match.AgendaItem.Meeting.Date.ToString();
                    xmlAgendaItem.AppendChild(source.CreateElement("boardName")).InnerText = match.AgendaItem.Meeting.BoardName;

                    var xmlSource = xmlAgendaItem.AppendChild(source.CreateElement("source")) as XmlElement;
                    xmlSource.SetAttribute("id", match.AgendaItem.Meeting.Source.Id.ToString());
                    xmlSource.AppendChild(source.CreateElement("name")).InnerText = match.AgendaItem.Meeting.Source.Name;
                }
            }

            MemoryStream output = new MemoryStream();
            XmlWriter resultWriter = XmlWriter.Create(output);

            transform.Transform(source, resultWriter);

            output.Seek(0, SeekOrigin.Begin);

            XmlDocument resultDoc = new XmlDocument();

            resultDoc.Load(XmlReader.Create(output));

            string contentType = "text/xml";

            // Inspect result to determine content-type.
            switch (resultDoc.DocumentElement.NamespaceURI)
            {
                //case "http://www.w3.org/1999/XSL/Format":
                //    // run through FO.NET to produce PDF.
                //    contentType = "application/pdf";
                //    break;
                case "http://www.w3.org/1999/xhtml":
                    contentType = "text/html";
                    break;
                default:
                    break;
            }

            output.Seek(0, SeekOrigin.Begin);

            return new Tuple<Stream, string>(output, contentType);
        }
    }
}