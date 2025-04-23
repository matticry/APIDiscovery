using System.Globalization;
using System.Xml.Linq;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class XmlFacturaService : IXmlFacturaService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _xmlOutputDirectory;

        public XmlFacturaService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _xmlOutputDirectory = config.GetValue<string>("XmlOutputDirectory") ?? "FacturasXml";
    
            if (!Directory.Exists(_xmlOutputDirectory))
            {
                Directory.CreateDirectory(_xmlOutputDirectory);
            }
        }


        public async Task<string> GenerarXmlFacturaAsync(int invoiceId)
        {
            throw new NotImplementedException();
        }
    }