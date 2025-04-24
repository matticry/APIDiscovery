using System.Security.Cryptography.Xml;
using System.Xml;

namespace APIDiscovery.Services.Commands;

public class SignedXmlWithId : SignedXml
{
    public SignedXmlWithId(XmlDocument doc) : base(doc) { }
    
    
    public override XmlElement GetIdElement(XmlDocument document, string idValue)
    {
        XmlElement idElem = base.GetIdElement(document, idValue);
        if (idElem != null)
            return idElem;

        // Buscar por atributo "ID" (mayúsculas)
        var elem = document.SelectSingleNode($"//*[@ID='{idValue}']") as XmlElement;
        return elem;
    }
}