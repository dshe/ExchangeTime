using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Schema;

#nullable enable

namespace ExchangeTime.Code
{
    internal static class Data
    {
        internal static List<Location> GetLocations()
        {
            var doc = GetXmlDocument();
            var locationNodes = doc.SelectNodes("ExchangeTime/Location");
            if (locationNodes == null || locationNodes.Count == 0)
                throw new Exception("ExchangeTime.xml: No 'ExchangeTime/Location' nodes found.");
            return locationNodes.Cast<XmlNode>().Select(node => new Location(node)).ToList();
        }

        private static XmlDocument GetXmlDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("ExchangeTime.xml");
            var uri = new Uri("/ExchangeTime.xsd", UriKind.Relative);
            // ExchangeTime.xsd is a project file added to assembly as a RESOURCE!
            var info = Application.GetResourceStream(uri); // will throw if cannot load resource
            Trace.Assert(info != null);
            XmlReader reader = new XmlTextReader(info.Stream);
            doc.Schemas.Add(null, reader);
            doc.Validate(ValidationHandler); // this does not exit on failure
            return doc;
        }

        private static void ValidationHandler(object sender, ValidationEventArgs vargs)
        {
            var msg = "XML Validation Error in line " + vargs.Exception.LineNumber + "\n\n" + vargs.Exception.SourceUri + "\n\n" + vargs.Message;
            MessageBox.Show(msg, "ExchangeTime: Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown(); // does not exit immediately
        }
    }
}
